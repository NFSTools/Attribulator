using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Abstractions;

namespace Attribulator.ModScript.API.Utils
{
    /// <summary>
    ///     Exposes utility functions to parse property paths.
    /// </summary>
    /// <example>CollectionKey</example>
    /// <example>ShiftPattern[1] XValues[0]</example>
    /// <example>ShiftPattern[1] XValues[0] Value</example>
    /// <example>TopArray[2] NestedArray[3] AnotherArray[4] StructValue StringVal</example>
    public static class PropertyUtils
    {
        /// <summary>
        ///     Parses the given property path and returns a stream of <see cref="ParsedProperty" /> objects
        /// </summary>
        /// <param name="path">The property path to parse.</param>
        /// <returns>An enumerable stream of <see cref="ParsedProperty" /> objects representing the property path.</returns>
        public static IEnumerable<ParsedProperty> ParsePath(string path)
        {
            return ParsePath(path.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        ///     Parses the given property path and returns a stream of <see cref="ParsedProperty" /> objects
        /// </summary>
        /// <param name="path">The property path to parse.</param>
        /// <returns>An enumerable stream of <see cref="ParsedProperty" /> objects representing the property path.</returns>
        public static IEnumerable<ParsedProperty> ParsePath(IEnumerable<string> path)
        {
            foreach (var pathSegment in path)
                if (!ushort.TryParse(pathSegment, out var index))
                {
                    // Do full parse
                    var split = pathSegment.Split(new[] {'[', ']'}, StringSplitOptions.RemoveEmptyEntries);

                    switch (split.Length)
                    {
                        case 1:
                            yield return new ParsedProperty {Name = split[0], HasIndex = false};
                            break;
                        case 2:
                            yield return new ParsedProperty
                                {Name = split[0], Index = ushort.Parse(split[1]), HasIndex = true};
                            break;
                        default:
                            throw new InvalidDataException($"Cannot parse segment: {pathSegment}");
                    }
                }
                else
                {
                    yield return new ParsedProperty {Index = index, Name = null, HasIndex = true};
                }
        }

        /// <summary>
        ///     Retrieves the relevant property for the given property path in the given object.
        /// </summary>
        /// <param name="baseObject">The object to examine</param>
        /// <param name="propertyPath">The property path</param>
        /// <returns>The value</returns>
        public static RetrievedProperty GetProperty([NotNull] VLTBaseType baseObject, [NotNull] string propertyPath)
        {
            return GetProperty(baseObject, ParsePath(propertyPath));
        }

        /// <summary>
        ///     Retrieves the relevant property for the given property path in the given object.
        /// </summary>
        /// <param name="baseObject">The object to examine</param>
        /// <param name="propertyPath">The parsed property path</param>
        /// <returns>The value</returns>
        public static RetrievedProperty GetProperty([NotNull] VLTBaseType baseObject,
            [NotNull] IEnumerable<ParsedProperty> propertyPath)
        {
            object itemToExamine = baseObject;
            var pathList = propertyPath.ToList();
            PropertyInfo propertyInfo = null;

            for (var index = 0; index < pathList.Count; index++)
            {
                if (itemToExamine == null) throw new CommandExecutionException("Cannot manipulate null object");

                var parsedProperty = pathList[index];
                if (parsedProperty.Name != null)
                {
                    var propName = parsedProperty.Name;
                    if (itemToExamine is BaseRefSpec)
                        propName = propName switch
                        {
                            "Collection" => "CollectionKey",
                            "Class" => "ClassKey",
                            _ => propName
                        };

                    propertyInfo = itemToExamine.GetType()
                        .GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

                    if (propertyInfo == null)
                        throw new CommandExecutionException(
                            $"Property not found on type {itemToExamine.GetType()}: {propName}");

                    if (!(propertyInfo.SetMethod?.IsPublic ?? false))
                        throw new CommandExecutionException(
                            $"{itemToExamine.GetType()}[{propName}] is read-only");

                    if (index == pathList.Count - 1) break;
                    itemToExamine = propertyInfo.GetValue(itemToExamine);
                }

                if (parsedProperty.HasIndex)
                {
                    if (!(itemToExamine is Array array))
                        throw new CommandExecutionException("Not working with an array!");

                    if (parsedProperty.Index >= array.Length)
                        throw new CommandExecutionException(
                            $"Array index out of bounds (requested {parsedProperty.Index} but there are {array.Length} elements)");

                    itemToExamine = array.GetValue(parsedProperty.Index);

                    if (itemToExamine == null) throw new CommandExecutionException("Cannot manipulate null object");

                    var elementType = itemToExamine.GetType();

                    if (elementType.IsPrimitive || elementType == typeof(string))
                        return new ArrayProperty(array, parsedProperty.Index,
                            propertyInfo!.PropertyType.GetElementType());
                }
            }

            return new ReflectedProperty(propertyInfo, itemToExamine);
        }

        public struct ParsedProperty
        {
            public string Name;
            public ushort Index;
            public bool HasIndex;
        }

        public abstract class RetrievedProperty
        {
            public abstract object GetValue();
            public abstract void SetValue(object value);
        }

        public class ReflectedProperty : RetrievedProperty
        {
            private readonly PropertyInfo _propertyInfo;
            private readonly object _targetObject;

            public ReflectedProperty(PropertyInfo propertyInfo, object targetObject)
            {
                _propertyInfo = propertyInfo;
                _targetObject = targetObject;
            }

            public override object GetValue()
            {
                return _propertyInfo.GetValue(_targetObject);
            }

            public override void SetValue(object value)
            {
                _propertyInfo.SetValue(_targetObject, value);
            }

            public Type GetPropertyType()
            {
                return _propertyInfo.PropertyType;
            }
        }

        public class ArrayProperty : RetrievedProperty
        {
            private readonly Array _array;
            private readonly Type _elementType;
            private readonly int _index;

            public ArrayProperty(Array array, int index, Type elementType)
            {
                _array = array;
                _index = index;
                _elementType = elementType;
            }

            public override object GetValue()
            {
                return _array.GetValue(_index);
            }

            public override void SetValue(object value)
            {
                _array.SetValue(value, _index);
            }

            public Type GetElementType()
            {
                return _elementType;
            }
        }
    }
}
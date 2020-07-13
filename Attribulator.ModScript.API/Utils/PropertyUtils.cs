using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Abstractions;
using VaultLib.Core.Types.EA.Reflection;

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
        public static RetrievedProperty GetProperty(VLTBaseType baseObject, string propertyPath)
        {
            return GetProperty(baseObject, ParsePath(propertyPath));
        }

        /// <summary>
        ///     Retrieves the relevant property for the given property path in the given object.
        /// </summary>
        /// <param name="baseObject">The object to examine</param>
        /// <param name="propertyPath">The parsed property path</param>
        /// <returns>The value</returns>
        public static RetrievedProperty GetProperty(VLTBaseType baseObject, IEnumerable<ParsedProperty> propertyPath)
        {
            object itemToExamine = baseObject ?? throw new ArgumentNullException(nameof(baseObject));
            PropertyInfo propertyInfo = null;
            var pathList = propertyPath.ToList();
            string lastPropName = null;
            for (var i = 0; i < pathList.Count; i++)
            {
                if (itemToExamine == null) throw new CommandExecutionException("Cannot index null object");

                var parsedProperty = pathList[i];
                var propName = parsedProperty.Name;

                if (propName == null)
                {
                    if (propertyInfo == null)
                        throw new CommandExecutionException("PropertyInfo is null!");

                    if (!propertyInfo.PropertyType.IsArray)
                        throw new CommandExecutionException(
                            $"{itemToExamine.GetType()}[{lastPropName}] is not an array.");

                    var array = (Array) itemToExamine;

                    if (parsedProperty.Index >= array.Length)
                        throw new CommandExecutionException(
                            $"{itemToExamine.GetType()}[{lastPropName}]: index out of bounds (requested {parsedProperty.Index} but there are {array.Length} elements)");

                    itemToExamine = array.GetValue(parsedProperty.Index);
                }
                else
                {
                    switch (itemToExamine)
                    {
                        case BaseRefSpec _:
                            propName = propName switch
                            {
                                "Collection" => "CollectionKey",
                                "Class" => "ClassKey",
                                _ => propName
                            };
                            break;
                        case PrimitiveTypeBase _ when propName == "Value":
                            continue;
                    }

                    propertyInfo = itemToExamine.GetType()
                        .GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);

                    if (propertyInfo == null)
                        throw new CommandExecutionException(
                            $"{itemToExamine.GetType()}[{propName}] does not exist");

                    if (!(propertyInfo.SetMethod?.IsPublic ?? false))
                        throw new CommandExecutionException(
                            $"{itemToExamine.GetType()}[{propName}] is read-only");

                    if (i == pathList.Count - 1) break;
                    var newItemToExamine = propertyInfo.GetValue(itemToExamine);

                    if (parsedProperty.HasIndex)
                    {
                        if (!propertyInfo.PropertyType.IsArray)
                            throw new CommandExecutionException(
                                $"{itemToExamine.GetType()}[{propName}] is not an array.");

                        var array = (Array) newItemToExamine;

                        if (array == null)
                            throw new CommandExecutionException($"{itemToExamine.GetType()}[{propName}] is null.");

                        if (parsedProperty.Index >= array.Length)
                            throw new CommandExecutionException(
                                $"{itemToExamine.GetType()}[{propName}]: index out of bounds (requested {parsedProperty.Index} but there are {array.Length} elements)");

                        newItemToExamine = array.GetValue(parsedProperty.Index);
                    }

                    itemToExamine = newItemToExamine;
                    lastPropName = propName;
                }
            }

            return new RetrievedProperty
            {
                PropertyInfo = propertyInfo,
                TargetObject = itemToExamine
            };
        }

        public struct ParsedProperty
        {
            public string Name;
            public ushort Index;
            public bool HasIndex;
        }

        public struct RetrievedProperty
        {
            public PropertyInfo PropertyInfo;
            public object TargetObject;

            public object GetValue()
            {
                return PropertyInfo.GetValue(TargetObject);
            }

            public void SetValue(object value)
            {
                PropertyInfo.SetValue(TargetObject, value);
            }
        }
    }
}
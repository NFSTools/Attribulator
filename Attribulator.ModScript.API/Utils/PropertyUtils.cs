using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using VaultLib.Core.Types;

namespace Attribulator.ModScript.API.Utils
{
    /// <summary>
    ///     Exposes utility functions for property paths.
    /// </summary>
    /// <example>CollectionKey</example>
    /// <example>ShiftPattern[1] XValues[0]</example>
    /// <example>ShiftPattern[1] XValues[0] Value</example>
    /// <example>TopArray[2] NestedArray[3] AnotherArray[4] StructValue StringVal</example>
    public static class PropertyUtils
    {
        /// <summary>
        ///     Parses a property path and produces a stream of <see cref="ParsedProperty"/> structures.
        /// </summary>
        /// <param name="path">The path to parse.</param>
        /// <returns>A stream of <see cref="ParsedProperty"/> structures.</returns>
        public static IEnumerable<ParsedProperty> ParsePath(IEnumerable<string> path)
        {
            return from pathSegment in path
                let split = pathSegment.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
                select split.Length switch
                {
                    1 => new ParsedProperty { Name = split[0] },
                    2 => new ParsedProperty { Name = split[0], Index = ushort.Parse(split[1]) },
                    _ => throw new InvalidDataException($"Cannot parse segment: {pathSegment}")
                };
        }

        /// <summary>
        /// Generates a property access proxy for an object given a property path.
        /// </summary>
        /// <param name="baseObject">The initial object to descend into.</param>
        /// <param name="propertyPath">The property path to follow.</param>
        /// <returns>A property access proxy that can be used to get/set the property value.</returns>
        /// <exception cref="NullReferenceException">if an unexpected NULL reference is encountered</exception>
        /// <exception cref="MissingFieldException">if a property cannot be found on the data being examined</exception>
        /// <exception cref="FieldAccessException">if a property exists but is not both readable and writable</exception>
        /// <exception cref="IndexOutOfRangeException">if an attempted array access is determined to be out of bounds</exception>
        /// <exception cref="MemberAccessException">if an array is accessed without an index</exception>
        public static RetrievedProperty GetProperty([NotNull] VLTBaseType baseObject,
            [NotNull] IEnumerable<ParsedProperty> propertyPath)
        {
            object examining = baseObject;
            RetrievedProperty retrievedProperty = null;

            foreach (var parsedProperty in propertyPath)
            {
                if (examining == null)
                    throw new NullReferenceException(
                        "Ran into an unexpected NULL value - cannot access further properties");

                var pi = examining.GetType()
                    .GetProperty(parsedProperty.Name, BindingFlags.Public | BindingFlags.Instance);

                if (pi == null)
                    throw new MissingFieldException(
                        $"Could not find property [{parsedProperty.Name}] in type {examining.GetType()}");

                if (!pi.CanRead || !pi.CanWrite)
                    throw new FieldAccessException(
                        $"Property [{parsedProperty.Name}] of type {examining.GetType()} is not accessible");

                var pv = pi.GetValue(examining);
                var pt = pi.PropertyType;

                if (!pt.IsArray)
                {
                    retrievedProperty = new ReflectedProperty(pi, examining);
                    examining = pv;
                }
                else if (pv is Array arr)
                {
                    if (parsedProperty.Index is { } idx)
                    {
                        if (idx >= arr.Length)
                            throw new IndexOutOfRangeException(
                                $"Attempted access to item at index {idx}, but that's out of range: 0 <= {idx} < {arr.Length} not satisfied");

                        retrievedProperty = new ArrayProperty(arr, idx, pt.GetElementType());
                        examining = arr.GetValue(idx);
                    }
                    else
                    {
                        throw new MemberAccessException("Array access must include index");
                    }
                }
                else
                {
                    throw new NullReferenceException("Can't index into NULL array");
                }
            }

            return retrievedProperty;
        }

        public struct ParsedProperty
        {
            public string Name;
            public ushort? Index;
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
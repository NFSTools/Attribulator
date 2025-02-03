using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using Attribulator.ModScript.API.Utils;
using VaultLib.Core.Types;
using VaultLib.Core.Utils;

namespace Attribulator.Plugins.ModScript.Commands
{
    // update_field class node field [property] value
    public class UpdateFieldModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<UpdateFieldModScriptCommand>
    {
        public required string ClassName { get; init; }
        public required string CollectionName { get; init; }
        public required string FieldName { get; init; }
        public int ArrayIndex { get; init; }
        public required List<string> PropertyPath { get; init; }
        public required string Value { get; init; }

        public static UpdateFieldModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count < 5) throw new CommandParseException("Expected at least 5 tokens");

            var className = parts[1];
            var collectionName = parts[2];
            var fieldName = parts[3];
            var propertyPath = new List<string>();

            var split = fieldName.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

            int arrayIndex = 0;

            switch (split.Length)
            {
                case 2:
                    if (split[1] == "^")
                        arrayIndex = -1;
                    else
                        arrayIndex = int.Parse(split[1]);
                    fieldName = split[0];
                    break;
                case 1:
                    fieldName = split[0];
                    break;
                default:
                    throw new CommandParseException("Badly malformed update_field command...");
            }

            string value;
            if (parts.Count > 5)
            {
                propertyPath = parts.Skip(4).Take(parts.Count - 5).ToList();
                value = parts[^1];
            }
            else
            {
                value = parts[4];
            }

            return new UpdateFieldModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                FieldName = fieldName,
                ArrayIndex = arrayIndex,
                Value = value,
                PropertyPath = propertyPath
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName)!;
            var field = databaseHelper.GetField(collection.Class, FieldName);
            var rawValue = collection.GetRawValue(field.Key);
            // var itemToEdit = rawValue;

            object itemToEdit;

            var arrayIndex = ArrayIndex;

            if (rawValue is VltArrayType<TKey> array)
            {
                if (arrayIndex == -1)
                    arrayIndex = array.Items.Count - 1;
                if (arrayIndex >= 0 && arrayIndex < array.Items.Count)
                    itemToEdit = array.Items[arrayIndex];
                else
                    throw new CommandExecutionException(
                        $"update_field command is out of bounds. Checked: 0 <= {arrayIndex} < {array.Items.Count}");
            }
            else
            {
                itemToEdit = rawValue;
            }

            if (PropertyPath.Count == 0)
            {
                if (TypeUtils.IsPrimitiveValue(itemToEdit))
                {
                    itemToEdit = ValueConversionUtils.ConvertPrimitiveToNewPrimitive(itemToEdit.GetType(), Value);
                }
                else
                {
                    switch (itemToEdit)
                    {
                        case IStringValue stringValue:
                            stringValue.SetString(Value);
                            break;
                        case BaseRefSpec<TKey> refSpec:
                            refSpec.SetCollectionKey(databaseHelper.StringToKey(Value, true));
                            break;
                        default:
                            throw new CommandExecutionException(
                                $"Object stored in {ClassName}[{FieldName}] is not a simple type and cannot be used in a value-update command");
                    }
                }
            }
            else
            {
                // TODO for VaultLib: change Matrix to be multiple floats instead of 1 array
                if (itemToEdit is Matrix4x4 matrix && PropertyPath.Count == 1)
                {
                    var matrixPath =
                        PropertyPath[0].Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    var indices = matrixPath.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToArray();
                    if (indices.Length != 2) throw new CommandExecutionException("invalid matrix access");

                    var value = float.Parse(Value, CultureInfo.InvariantCulture);
                    switch ((indices[0], indices[1]))
                    {
                        case (1, 1):
                            matrix.M11 = value;
                            break;
                        case (1, 2):
                            matrix.M12 = value;
                            break;
                        case (1, 3):
                            matrix.M13 = value;
                            break;
                        case (1, 4):
                            matrix.M14 = value;
                            break;
                        case (2, 1):
                            matrix.M21 = value;
                            break;
                        case (2, 2):
                            matrix.M22 = value;
                            break;
                        case (2, 3):
                            matrix.M23 = value;
                            break;
                        case (2, 4):
                            matrix.M24 = value;
                            break;
                        case (3, 1):
                            matrix.M31 = value;
                            break;
                        case (3, 2):
                            matrix.M32 = value;
                            break;
                        case (3, 3):
                            matrix.M33 = value;
                            break;
                        case (3, 4):
                            matrix.M34 = value;
                            break;
                        case (4, 1):
                            matrix.M41 = value;
                            break;
                        case (4, 2):
                            matrix.M42 = value;
                            break;
                        case (4, 3):
                            matrix.M43 = value;
                            break;
                        case (4, 4):
                            matrix.M44 = value;
                            break;
                    }

                    itemToEdit = matrix;
                }
                else if (itemToEdit is BaseRefSpec<TKey> baseRefSpec && PropertyPath.Count == 1
                                                                     && (PropertyPath[0] == "Class" ||
                                                                         PropertyPath[0] == "Collection"))
                {
                    switch (PropertyPath[0])
                    {
                        case "Class":
                            baseRefSpec.SetClassKey(databaseHelper.StringToKey(Value, true));
                            break;
                        case "Collection":
                            baseRefSpec.SetCollectionKey(databaseHelper.StringToKey(Value, true));
                            break;
                    }
                }
                else
                {
                    var parsedProperties = PropertyUtils.ParsePath(PropertyPath).ToList();
                    var retrievedProperty = PropertyUtils.GetProperty(itemToEdit, parsedProperties);

                    var value = ValueConversionUtils.ConvertPrimitiveToNewPrimitive(retrievedProperty.GetPropertyType(),
                        Value);
                    if (value == null) throw new Exception();

                    retrievedProperty.SetValue(value);
                }
            }

            if (rawValue is VltArrayType<TKey> array2)
            {
                array2.Items[arrayIndex] = itemToEdit;
                collection.SetRawValue(field.Key, array2);
            }
            else
            {
                collection.SetRawValue(field.Key, itemToEdit);
            }

            databaseHelper.MarkVaultAsModified(collection.Vault);
        }
    }
}
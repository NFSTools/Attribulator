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
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public int ArrayIndex { get; set; }
        public List<string> PropertyPath { get; set; }
        public string Value { get; set; }

        public static UpdateFieldModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count < 5) throw new CommandParseException("Expected at least 5 tokens");

            var className = parts[1];
            var collectionName = CleanHashString(parts[2]);
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

            fieldName = CleanHashString(fieldName);

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
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var field = databaseHelper.GetField(collection.Class, FieldName);
            var data = collection.GetRawValue(field.Key);
            var itemToEdit = data;

            if (data is VltArrayType<TKey> array)
            {
                if (ArrayIndex == -1)
                    ArrayIndex = array.Items.Count - 1;
                if (ArrayIndex >= 0 && ArrayIndex < array.Items.Count)
                    itemToEdit = array.Items[ArrayIndex];
                else
                    throw new CommandExecutionException(
                        $"update_field command is out of bounds. Checked: 0 <= {ArrayIndex} < {array.Items.Count}");
            }

            if (PropertyPath.Count == 0)
            {
                if (TypeUtils.IsPrimitiveValue(itemToEdit))
                {
                    itemToEdit = ValueConversionUtils.ConvertPrimitiveToNewPrimitive(itemToEdit.GetType(), Value);
                }
                else if (itemToEdit is IStringValue stringValue)
                {
                    stringValue.SetString(Value);
                }
                else if (itemToEdit is BaseRefSpec<TKey> refSpec)
                {
                    refSpec.SetCollectionKey(KeyUtils.StringToKey<TKey>(Value, true));
                }
                else
                {
                    throw new CommandExecutionException(
                        $"Object stored in {ClassName}[{FieldName}] is not a simple type and cannot be used in a value-update command");
                }
                // switch (itemToEdit)
                // {
                //     case PrimitiveTypeBase primitiveTypeBase:
                //         ValueConversionUtils.DoPrimitiveConversion(primitiveTypeBase, Value);
                //         break;
                //     case IStringValue stringValue:
                //         stringValue.SetString(Value);
                //         break;
                //     case BaseRefSpec refSpec:
                //         // NOTE: This is a compatibility feature for certain types, such as GCollectionKey, which are technically a RefSpec.
                //         refSpec.CollectionKey = Value;
                //         break;
                //     default:
                //         throw new CommandExecutionException(
                //             $"cannot handle update for {collection.Class.Name}[{field.Name}]");
                // }
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

            collection.SetRawValue(field.Key, itemToEdit);

            databaseHelper.MarkVaultAsModified(collection.Vault);
        }
    }
}
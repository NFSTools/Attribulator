using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Abstractions;
using VaultLib.Core.Types.Attrib.Types;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using YAMLDatabase.API.Utils;
using YAMLDatabase.ModScript.API;

namespace YAMLDatabase.Plugins.ModScript.Commands
{
    // update_field class node field [property] value
    public class UpdateFieldModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public int ArrayIndex { get; set; }
        public List<string> PropertyPath { get; set; }
        public string Value { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count < 5) throw new CommandParseException("Expected at least 5 tokens");

            ClassName = parts[1];
            CollectionName = CleanHashString(parts[2]);
            FieldName = parts[3];
            PropertyPath = new List<string>();

            if (FieldName.Contains('['))
            {
                var split = FieldName.Split(new[] {'[', ']'}, StringSplitOptions.RemoveEmptyEntries);
                if (split[1] == "^")
                    ArrayIndex = -1;
                else
                    ArrayIndex = int.Parse(split[1]);
                FieldName = split[0];
            }

            FieldName = CleanHashString(FieldName);

            if (parts.Count > 5)
            {
                PropertyPath = parts.Skip(4).Take(parts.Count - 5).ToList();
                Value = parts[^1];
            }
            else
            {
                Value = parts[4];
            }
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var field = GetField(collection.Class, FieldName);
            var data = collection.GetRawValue(field.Name);
            var itemToEdit = data;

            if (data is VLTArrayType array)
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
                switch (itemToEdit)
                {
                    case PrimitiveTypeBase primitiveTypeBase:
                        ValueConversionUtils.DoPrimitiveConversion(primitiveTypeBase, Value);
                        break;
                    case IStringValue stringValue:
                        stringValue.SetString(Value);
                        break;
                    case BaseRefSpec refSpec:
                        // NOTE: This is a compatibility feature for certain types, such as GCollectionKey, which are technically a RefSpec.
                        refSpec.CollectionKey = Value;
                        break;
                    default:
                        throw new CommandExecutionException(
                            $"cannot handle update for {collection.Class.Name}[{field.Name}]");
                }
            }
            else
            {
                // TODO for VaultLib: change Matrix to be multiple floats instead of 1 array
                if (itemToEdit is Matrix matrix && PropertyPath.Count == 1)
                {
                    var matrixPath =
                        PropertyPath[0].Split(new[] {'[', ']'}, StringSplitOptions.RemoveEmptyEntries)[1];
                    var indices = matrixPath.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToArray();
                    if (indices.Length != 2) throw new CommandExecutionException("invalid matrix access");

                    matrix.Data ??= new float[16];
                    matrix.Data[4 * (indices[0] - 1) + (indices[1] - 1)] =
                        float.Parse(Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    object valueToEdit = itemToEdit;
                    PropertyInfo propertyInfo = null;
                    for (var i = 0; i < PropertyPath.Count; i++)
                    {
                        if (valueToEdit is BaseRefSpec)
                            PropertyPath[i] = PropertyPath[i] switch
                            {
                                "Collection" => "CollectionKey",
                                "Class" => "ClassKey",
                                _ => PropertyPath[i]
                            };

                        propertyInfo = valueToEdit.GetType()
                            .GetProperty(PropertyPath[i], BindingFlags.Instance | BindingFlags.Public);

                        if (propertyInfo == null)
                            throw new CommandExecutionException(
                                $"{itemToEdit.GetType()}[{PropertyPath[i]}] does not exist");

                        if (!(propertyInfo.SetMethod?.IsPublic ?? false))
                            throw new CommandExecutionException(
                                $"{itemToEdit.GetType()}[{PropertyPath[i]}] is read-only");

                        if (i == PropertyPath.Count - 1) break;

                        var newValue = propertyInfo.GetValue(valueToEdit);

                        if (newValue == null)
                        {
                            if (propertyInfo.PropertyType.IsSubclassOf(typeof(VLTBaseType)))
                                newValue = Activator.CreateInstance(propertyInfo.PropertyType, collection.Class,
                                    field, collection);
                            else
                                newValue = Activator.CreateInstance(propertyInfo.PropertyType);

                            propertyInfo.SetValue(valueToEdit, newValue);
                        }

                        valueToEdit = newValue;
                    }

                    var value = ValueConversionUtils.DoPrimitiveConversion(propertyInfo.GetValue(valueToEdit), Value);
                    if (value == null) throw new Exception();
                    propertyInfo.SetValue(valueToEdit, value);
                }
            }
        }
    }
}
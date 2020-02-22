using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib.Types;
using VaultLib.Core.Types.EA.Reflection;
using YAMLDatabase.ModScript.Utils;

namespace YAMLDatabase.ModScript.Commands
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
            if (parts.Count < 5)
            {
                throw new ModScriptParserException("Expected at least 5 tokens");
            }

            ClassName = parts[1];
            CollectionName = CleanHashString(parts[2]);
            FieldName = parts[3];
            PropertyPath = new List<string>();

            if (FieldName.Contains('['))
            {
                var split = FieldName.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
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

        public override void Execute(Database database)
        {
            VltCollection collection = GetCollection(database, ClassName, CollectionName);
            VltClassField field = GetField(collection.Class, FieldName);
            VLTBaseType data = collection.GetRawValue(field.Name);
            VLTBaseType itemToEdit = data;

            if (data is VLTArrayType array)
            {
                if (ArrayIndex < array.Items.Count)
                    itemToEdit = array.Items[ArrayIndex];
                else if (ArrayIndex == array.Items.Count)
                {
                    array.Items.Add(TypeRegistry.ConstructInstance(array.ItemType, collection.Class, field, collection));
                    itemToEdit = array.Items[ArrayIndex];
                }
                else
                {
                    throw new ModScriptCommandExecutionException($"update_field command is out of bounds. If you resized the array, make sure your updates are sorted by index.");
                }
            }

            if (PropertyPath.Count == 0)
            {
                // update_field class collection field value
                if (itemToEdit is PrimitiveTypeBase primitiveTypeBase)
                {
                    ValueConversionUtils.DoPrimitiveConversion(primitiveTypeBase, Value);
                }
                else
                {
                    throw new ModScriptCommandExecutionException($"cannot handle update for {collection.Class.Name}[{field.Name}]");
                }
            }
            else
            {
                // TODO for VaultLib: change Matrix to be multiple floats instead of 1 array
                if (itemToEdit is Matrix matrix && PropertyPath.Count == 1)
                {
                    string matrixPath =
                        PropertyPath[0].Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    int[] indices = matrixPath.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToArray();
                    if (indices.Length != 2)
                    {
                        throw new ModScriptCommandExecutionException("invalid matrix access");
                    }

                    matrix.Data ??= new float[16];
                    matrix.Data[4 * (indices[0] - 1) + (indices[1] - 1)] =
                        float.Parse(Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    object valueToEdit = itemToEdit;
                    PropertyInfo propertyInfo = null;
                    for (int i = 0; i < PropertyPath.Count; i++)
                    {
                        propertyInfo = valueToEdit.GetType()
                            .GetProperty(PropertyPath[i], BindingFlags.Instance | BindingFlags.Public);
                        if (propertyInfo == null)
                        {
                            throw new InvalidDataException($"{itemToEdit.GetType()}[{PropertyPath[i]}] does not exist");
                        }

                        if (!(propertyInfo.SetMethod?.IsPublic ?? false))
                        {
                            throw new InvalidDataException($"{itemToEdit.GetType()}[{PropertyPath[i]}] is read-only");
                        }

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
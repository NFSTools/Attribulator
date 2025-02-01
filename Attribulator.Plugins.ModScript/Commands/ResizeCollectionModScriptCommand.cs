using System;
using System.Collections.Generic;
using System.Linq;
using Attribulator.ModScript.API;
using Attribulator.ModScript.API.Utils;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    // resize_collection class node field [property path] size
    public class ResizeCollectionModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<ResizeCollectionModScriptCommand>
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public int ArrayIndex { get; set; }
        public List<string> PropertyPath { get; set; }
        public ushort Size { get; set; }

        public static ResizeCollectionModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count < 6) throw new CommandParseException("Expected at least 6 tokens");

            var className = parts[1];
            var collectionName = (parts[2]);
            var fieldName = parts[3];

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

            var propertyPath = parts.Skip(4).Take(parts.Count - 5).ToList();
            var size = ushort.Parse(parts[^1]);

            return new ResizeCollectionModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                FieldName = fieldName,
                ArrayIndex = arrayIndex,
                PropertyPath = propertyPath,
                Size = size
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
                        $"resize_collection command is out of bounds. Checked: 0 <= {ArrayIndex} < {array.Items.Count}");
            }

            var parsedProperties = PropertyUtils.ParsePath(PropertyPath).ToList();
            var retrievedProperty =
                (PropertyUtils.ReflectedProperty)PropertyUtils.GetProperty(itemToEdit, parsedProperties);
            var retrievedValue = retrievedProperty.GetValue();

            if (!(retrievedValue is Array retrievedArray))
                throw new CommandExecutionException("Value is not an array.");

            var elementType = retrievedProperty.GetPropertyType().GetElementType();

            if (elementType == null) throw new CommandExecutionException("GetElementType() returned null");

            var newArray = Array.CreateInstance(elementType, Size);

            for (var i = 0; i < retrievedArray.Length && i < Size; i++)
                newArray.SetValue(retrievedArray.GetValue(i), i);

            retrievedProperty.SetValue(newArray);
            databaseHelper.MarkVaultAsModified(collection.Vault);
        }
    }
}
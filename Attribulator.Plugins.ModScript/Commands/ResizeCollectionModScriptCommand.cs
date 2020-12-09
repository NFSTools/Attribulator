using System;
using System.Collections.Generic;
using System.Linq;
using Attribulator.ModScript.API;
using Attribulator.ModScript.API.Utils;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    // resize_collection class node field [property path] size
    public class ResizeCollectionModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public int ArrayIndex { get; set; }
        public List<string> PropertyPath { get; set; }
        public ushort Size { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count < 6) throw new CommandParseException("Expected at least 6 tokens");

            ClassName = parts[1];
            CollectionName = CleanHashString(parts[2]);
            FieldName = parts[3];
            PropertyPath = new List<string>();

            var split = FieldName.Split(new[] {'[', ']'}, StringSplitOptions.RemoveEmptyEntries);

            switch (split.Length)
            {
                case 2:
                    if (split[1] == "^")
                        ArrayIndex = -1;
                    else
                        ArrayIndex = int.Parse(split[1]);
                    FieldName = split[0];
                    break;
                case 1:
                    FieldName = split[0];
                    break;
                default:
                    throw new CommandParseException("Badly malformed update_field command...");
            }

            FieldName = CleanHashString(FieldName);
            PropertyPath = parts.Skip(4).Take(parts.Count - 5).ToList();
            Size = ushort.Parse(parts[^1]);
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
                        $"resize_collection command is out of bounds. Checked: 0 <= {ArrayIndex} < {array.Items.Count}");
            }

            var parsedProperties = PropertyUtils.ParsePath(PropertyPath).ToList();
            var retrievedProperty =
                (PropertyUtils.ReflectedProperty) PropertyUtils.GetProperty(itemToEdit, parsedProperties);
            var retrievedValue = retrievedProperty.GetValue();

            if (!(retrievedValue is Array retrievedArray))
                throw new CommandExecutionException("Value is not an array.");

            var elementType = retrievedProperty.GetPropertyType().GetElementType();

            if (elementType == null) throw new CommandExecutionException("GetElementType() returned null");

            var newArray = Array.CreateInstance(elementType, Size);

            for (var i = 0; i < retrievedArray.Length && i < Size; i++)
                newArray.SetValue(retrievedArray.GetValue(i), i);

            retrievedProperty.SetValue(newArray);
        }
    }
}
using System.Collections.Generic;
using Attribulator.ModScript.API;
using VaultLib.Core;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class ResizeFieldModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public ushort NewCapacity { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 5) throw new CommandParseException($"Expected 5 tokens but got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            FieldName = CleanHashString(parts[3]);

            if (!ushort.TryParse(parts[4], out var newCapacity))
                throw new CommandParseException($"Failed to parse '{parts[4]}' as a number");

            NewCapacity = newCapacity;
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var field = GetField(collection.Class, FieldName);

            if (!field.IsArray)
                throw new CommandExecutionException($"Field {ClassName}[{FieldName}] is not an array!");

            if (field.MaxCount < NewCapacity)
                throw new CommandExecutionException(
                    $"Cannot resize field {ClassName}[{FieldName}] beyond maximum count (requested {NewCapacity} but limit is {field.MaxCount})");

            var array = collection.GetRawValue<VLTArrayType>(FieldName);

            if (NewCapacity < array.Items.Count)
                while (NewCapacity < array.Items.Count)
                    array.Items.RemoveAt(array.Items.Count - 1);
            else if (NewCapacity > array.Items.Count)
                while (NewCapacity > array.Items.Count)
                    array.Items.Add(TypeRegistry.ConstructInstance(array.ItemType, collection.Class, field,
                        collection));

            if (!field.IsInLayout) array.Capacity = NewCapacity;
        }
    }
}
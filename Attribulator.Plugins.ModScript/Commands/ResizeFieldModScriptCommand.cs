using System.Collections.Generic;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class ResizeFieldModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<ResizeFieldModScriptCommand>
    {
        public required string ClassName { get; init; }
        public required string CollectionName { get; init; }
        public required string FieldName { get; init; }
        public ushort NewCapacity { get; init; }

        public static ResizeFieldModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 5) throw new CommandParseException($"Expected 5 tokens but got {parts.Count}");

            var className = parts[1];
            var collectionName = parts[2];
            var fieldName = parts[3];

            if (!ushort.TryParse(parts[4], out var newCapacity))
                throw new CommandParseException($"Failed to parse '{parts[4]}' as a number");

            return new ResizeFieldModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                FieldName = fieldName,
                NewCapacity = newCapacity,
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName)!;
            var field = databaseHelper.GetField(collection.Class, FieldName);

            if (!field.IsArray)
                throw new CommandExecutionException($"Field {ClassName}[{FieldName}] is not an array!");

            if (field.MaxCount < NewCapacity)
                throw new CommandExecutionException(
                    $"Cannot resize field {ClassName}[{FieldName}] beyond maximum count (requested {NewCapacity} but limit is {field.MaxCount})");

            var array = collection.GetRawValue<VltArrayType<TKey>>(field.Key);

            if (NewCapacity < array.Items.Count)
            {
                while (NewCapacity < array.Items.Count)
                    array.Items.RemoveAt(array.Items.Count - 1);
            }
            else if (NewCapacity > array.Items.Count)
            {
                while (NewCapacity > array.Items.Count)
                    array.Items.Add(FieldUtils.ConstructFieldType(databaseHelper.Database.TypeRegistry, field));
            }

            if (!field.IsInLayout)
            {
                if (array.Capacity != NewCapacity)
                {
                    array.Capacity = NewCapacity;
                    databaseHelper.MarkVaultAsModified(collection.Vault);
                }
            }
        }
    }
}
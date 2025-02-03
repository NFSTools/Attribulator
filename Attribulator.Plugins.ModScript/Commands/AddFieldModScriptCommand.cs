using System.Collections.Generic;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    // add_field class node field
    public class AddFieldModScriptCommand : BaseModScriptCommand, IParseableModScriptCommand<AddFieldModScriptCommand>
    {
        public required string ClassName { get; init; }
        public required string CollectionName { get; init; }
        public required string FieldName { get; init; }
        public ushort ArrayCapacity { get; init; }

        public static AddFieldModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 4 && parts.Count != 5)
                throw new CommandParseException($"Expected 4 or 5 tokens, got {parts.Count}");

            var className = parts[1];
            var collectionName = parts[2];
            var fieldName = parts[3];

            ushort arrayCapacity = 0;

            if (parts.Count == 5) arrayCapacity = ushort.Parse(parts[4]);

            return new AddFieldModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                FieldName = fieldName,
                ArrayCapacity = arrayCapacity
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName)!;
            var field = collection.Class[FieldName];

            if (field.IsInLayout)
                throw new CommandExecutionException($"add_field failed because field '{FieldName}' is a base field");

            if (collection.HasEntry(field.Key))
                return;

            var vltBaseType = FieldUtils.CreateFieldValue(databaseHelper.Database.TypeRegistry, field);

            if (vltBaseType is VltArrayType<TKey> array)
            {
                if (ArrayCapacity > field.MaxCount)
                    throw new CommandExecutionException(
                        $"Cannot add field {ClassName}[{FieldName}] with capacity beyond maximum (requested {ArrayCapacity} but limit is {field.MaxCount})");

                array.Capacity = ArrayCapacity;
                array.Items = new List<object>();

                for (var i = 0; i < ArrayCapacity; i++)
                    array.Items.Add(FieldUtils.ConstructFieldType(databaseHelper.Database.TypeRegistry, field));
            }

            collection.SetRawValue(field.Key, vltBaseType);
            databaseHelper.MarkVaultAsModified(collection.Vault);
        }
    }
}
using System.Collections.Generic;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using VaultLib.Core;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    // add_field class node field
    public class AddFieldModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public ushort ArrayCapacity { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4 && parts.Count != 5)
                throw new CommandParseException($"Expected 4 or 5 tokens, got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            FieldName = CleanHashString(parts[3]);

            if (parts.Count == 5) ArrayCapacity = ushort.Parse(parts[4]);
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var field = collection.Class[FieldName];

            if (field.IsInLayout)
                throw new CommandExecutionException($"add_field failed because field '{field.Name}' is a base field");

            if (collection.HasEntry(field.Name))
                return;

            var vltBaseType = FieldUtils.CreateFieldValue(databaseHelper.Database.TypeRegistry, field);

            if (vltBaseType is VltArrayType array)
            {
                if (ArrayCapacity > field.MaxCount)
                    throw new CommandExecutionException(
                        $"Cannot add field {ClassName}[{FieldName}] with capacity beyond maximum (requested {ArrayCapacity} but limit is {field.MaxCount})");

                array.Capacity = ArrayCapacity;
                array.Items = new List<object>();

                for (var i = 0; i < ArrayCapacity; i++)
                    array.Items.Add(FieldUtils.ConstructFieldType(databaseHelper.Database.TypeRegistry, field));
            }

            collection.SetRawValue(field.Name, vltBaseType);
            databaseHelper.MarkVaultAsModified(collection.Vault);
        }
    }
}
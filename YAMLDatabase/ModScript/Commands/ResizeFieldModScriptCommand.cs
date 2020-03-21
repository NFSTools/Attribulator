using System.Collections.Generic;
using System.Diagnostics;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Types;

namespace YAMLDatabase.ModScript.Commands
{
    public class ResizeFieldModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public ushort NewCapacity { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 5)
            {
                throw new ModScriptParserException($"Expected 5 tokens but got {parts.Count}");
            }

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            FieldName = CleanHashString(parts[3]);

            if (!ushort.TryParse(parts[4], out var newCapacity))
            {
                throw new ModScriptParserException($"Failed to parse '{parts[4]}' as a number");
            }

            NewCapacity = newCapacity;
        }

        public override void Execute(ModScriptDatabaseHelper database)
        {
            VltCollection collection = GetCollection(database, ClassName, CollectionName);
            VltClassField field = GetField(collection.Class, FieldName);

            if (!field.IsArray)
            {
                throw new ModScriptCommandExecutionException($"Field {ClassName}[{FieldName}] is not an array!");
            }

            if (!collection.HasEntry(FieldName))
            {
                throw new ModScriptCommandExecutionException($"Collection {collection.ShortPath} does not have an entry for {FieldName}.");
            }

            VLTArrayType array = collection.GetRawValue<VLTArrayType>(FieldName);

            while (NewCapacity < array.Items.Count)
            {
                array.Items.RemoveAt(array.Items.Count - 1);
            }

            if (!field.IsInLayout)
            {
                array.Capacity = NewCapacity;
            }
        }
    }
}
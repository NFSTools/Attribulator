using System.Collections.Generic;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Abstractions;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using YAMLDatabase.ModScript.Utils;

namespace YAMLDatabase.ModScript.Commands
{
    // append_array class node field [value]
    public class AppendArrayModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public string Value { get; set; }

        private bool _hasValue;

        public override void Parse(List<string> parts)
        {
            if (parts.Count < 4)
            {
                throw new ModScriptParserException("Expected at least 4 tokens");
            }

            ClassName = parts[1];
            CollectionName = CleanHashString(parts[2]);
            FieldName = CleanHashString(parts[3]);

            if (parts.Count > 4)
            {
                Value = parts[4];
                _hasValue = true;
            }
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

            if (array.Items.Count == array.Capacity && field.IsInLayout)
            {
                throw new ModScriptCommandExecutionException("Cannot append to a full array when it is a layout field");
            }

            if (array.Items.Count + 1 > field.MaxCount)
            {
                throw new ModScriptCommandExecutionException("Appending to this array would cause it to exceed the maximum number of allowed elements.");
            }

            var itemToEdit = TypeRegistry.ConstructInstance(array.ItemType, collection.Class, field, collection);

            if (_hasValue)
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
                        throw new ModScriptCommandExecutionException($"Object stored in {collection.Class.Name}[{field.Name}] is not a simple type and cannot be used in a value-append command");
                }
            }

            array.Items.Add(itemToEdit);

            if (!field.IsInLayout)
            {
                array.Capacity++;
            }
        }
    }
}
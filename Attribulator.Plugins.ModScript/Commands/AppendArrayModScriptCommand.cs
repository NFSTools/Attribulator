using System.Collections.Generic;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using VaultLib.Core.Types;
using VaultLib.Core.Utils;

namespace Attribulator.Plugins.ModScript.Commands
{
    // append_array class node field [value]
    public class AppendArrayModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<AppendArrayModScriptCommand>
    {
        public required string ClassName { get; init; }
        public required string CollectionName { get; init; }
        public required string FieldName { get; init; }
        public required string? Value { get; init; }

        public static AppendArrayModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count < 4) throw new CommandParseException("Expected at least 4 tokens");

            var className = parts[1];
            var collectionName = parts[2];
            var fieldName = parts[3];
            string? value = null;

            if (parts.Count > 4)
            {
                value = parts[4];
            }

            return new AppendArrayModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                FieldName = fieldName,
                Value = value
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName)!;
            var field = databaseHelper.GetField(collection.Class, FieldName);

            if (!field.IsArray)
                throw new CommandExecutionException($"Field {ClassName}[{FieldName}] is not an array!");

            var fieldKey = databaseHelper.StringToKey(FieldName);
            if (!collection.HasEntry(fieldKey))
                throw new CommandExecutionException(
                    $"Collection {ClassName}[{CollectionName}] does not have an entry for {FieldName}.");

            var array = collection.GetRawValue<VltArrayType<TKey>>(fieldKey);

            if (array.Items.Count == array.Capacity && field.IsInLayout)
                throw new CommandExecutionException("Cannot append to a full array when it is a layout field");

            if (array.Items.Count + 1 > field.MaxCount)
                throw new CommandExecutionException(
                    "Appending to this array would cause it to exceed the maximum number of allowed elements.");

            var itemToEdit = FieldUtils.ConstructFieldType(databaseHelper.Database.TypeRegistry, field);

            if (Value != null)
            {
                if (TypeUtils.IsPrimitiveValue(itemToEdit))
                {
                    itemToEdit = ValueConversionUtils.ConvertPrimitiveToNewPrimitive(itemToEdit.GetType(), Value);
                }
                else
                {
                    switch (itemToEdit)
                    {
                        case IStringValue stringValue:
                            stringValue.SetString(Value);
                            break;
                        case BaseRefSpec<TKey> refSpec:
                            refSpec.SetCollectionKey(databaseHelper.StringToKey(Value, true));
                            break;
                        default:
                            throw new CommandExecutionException(
                                $"Object stored in {collection.Class.Key}[{field.Key}] is not a simple type and cannot be used in a value-append command");
                    }
                }
            }

            array.Items.Add(itemToEdit);

            if (!field.IsInLayout) array.Capacity++;
            databaseHelper.MarkVaultAsModified(collection.Vault);
        }
    }
}
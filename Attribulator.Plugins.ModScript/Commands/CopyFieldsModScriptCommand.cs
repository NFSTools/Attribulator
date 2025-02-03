using System;
using System.Collections.Generic;
using System.Linq;
using Attribulator.ModScript.API;
using Attribulator.ModScript.API.Utils;
using VaultLib.Core.Data;

namespace Attribulator.Plugins.ModScript.Commands
{
    // copy_fields class sourceNode targetNode options
    public class CopyFieldsModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<CopyFieldsModScriptCommand>
    {
        [Flags]
        public enum CopyOptions
        {
            Base = 1, // copy+overwrite all base fields
            Optional = 2, // copy nonexistent optional fields
            OverwriteOptional = 4 // copy+overwrite all optional fields
        }

        public required string ClassName { get; init; }
        public required string SourceCollectionName { get; init; }
        public required string DestinationCollectionName { get; init; }
        public CopyOptions Options { get; init; }

        public static CopyFieldsModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 5) throw new CommandParseException($"Expected 5 tokens, got {parts.Count}");

            var className = parts[1];
            var sourceCollectionName = parts[2];
            var destinationCollectionName = parts[3];
            var copyOptionEntries = parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

            CopyOptions options = 0;

            if (copyOptionEntries.Contains("base"))
                options |= CopyOptions.Base;
            if (copyOptionEntries.Contains("optional"))
                options |= CopyOptions.Optional;
            if (copyOptionEntries.Contains("overwrite"))
                options |= CopyOptions.OverwriteOptional;

            return new CopyFieldsModScriptCommand
            {
                ClassName = className,
                SourceCollectionName = sourceCollectionName,
                DestinationCollectionName = destinationCollectionName,
                Options = options,
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var srcCollection = GetCollection(databaseHelper, ClassName, SourceCollectionName)!;
            var dstCollection = GetCollection(databaseHelper, ClassName, DestinationCollectionName)!;
            var values = new Dictionary<VltClassField<TKey>, object>();

            if ((Options & CopyOptions.Base) != 0)
                foreach (var baseField in srcCollection.Class.BaseFields)
                    values.Add(baseField,
                        ValueCloningUtils.CloneValue(databaseHelper.Database, srcCollection.GetRawValue(baseField.Key),
                            srcCollection.Class,
                            baseField, dstCollection));

            if ((Options & CopyOptions.Optional) != 0)
                foreach (var (key, value) in srcCollection.GetData())
                {
                    var field = srcCollection.Class[key];

                    if (!field.IsInLayout)
                        values.Add(field,
                            ValueCloningUtils.CloneValue(databaseHelper.Database, value, srcCollection.Class, field,
                                dstCollection));
                }

            // base will always overwrite
            // optional by itself will copy anything that doesn't exist
            // optional + overwrite will copy nonexistent fields and overwrite the other ones(optional only)
            if ((Options & CopyOptions.Base) != 0)
                foreach (var (field, value) in values)
                    if (field.IsInLayout)
                        dstCollection.SetRawValue(field.Key, value);

            if ((Options & CopyOptions.Optional) != 0)
                foreach (var (field, value) in values)
                    if (!field.IsInLayout && (!dstCollection.HasEntry(field.Key) ||
                                              (Options & CopyOptions.OverwriteOptional) != 0))
                        dstCollection.SetRawValue(field.Key, value);
            databaseHelper.MarkVaultAsModified(dstCollection.Vault);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Attribulator.ModScript.API;
using Attribulator.Plugins.ModScript.Utils;
using VaultLib.Core.Data;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    // copy_fields class sourceNode targetNode options
    public class CopyFieldsModScriptCommand : BaseModScriptCommand
    {
        [Flags]
        public enum CopyOptions
        {
            Base = 1, // copy+overwrite all base fields
            Optional = 2, // copy nonexistent optional fields
            OverwriteOptional = 4 // copy+overwrite all optional fields
        }

        public string ClassName { get; set; }
        public string SourceCollectionName { get; set; }
        public string DestinationCollectionName { get; set; }
        public CopyOptions Options { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 5) throw new CommandParseException($"Expected 5 tokens, got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            SourceCollectionName = CleanHashString(parts[2]);
            DestinationCollectionName = CleanHashString(parts[3]);
            var copyOptionEntries = parts[4].Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (copyOptionEntries.Contains("base"))
                Options |= CopyOptions.Base;
            if (copyOptionEntries.Contains("optional"))
                Options |= CopyOptions.Optional;
            if (copyOptionEntries.Contains("overwrite"))
                Options |= CopyOptions.OverwriteOptional;
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var srcCollection = GetCollection(databaseHelper, ClassName, SourceCollectionName);
            var dstCollection = GetCollection(databaseHelper, ClassName, DestinationCollectionName);
            var values = new Dictionary<VltClassField, VLTBaseType>();

            if ((Options & CopyOptions.Base) != 0)
                foreach (var baseField in srcCollection.Class.BaseFields)
                    values.Add(baseField,
                        ValueCloningUtils.CloneValue(databaseHelper.Database, srcCollection.GetRawValue(baseField.Name),
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
                foreach (var (key, value) in values)
                    if (key.IsInLayout)
                        dstCollection.SetRawValue(key.Name, value);

            if ((Options & CopyOptions.Optional) != 0)
                foreach (var (field, value) in values)
                    if (!field.IsInLayout && (!dstCollection.HasEntry(field.Name) ||
                                              (Options & CopyOptions.OverwriteOptional) != 0))
                        dstCollection.SetRawValue(field.Name, value);
        }
    }
}
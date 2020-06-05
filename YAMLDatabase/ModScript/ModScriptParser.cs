using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAMLDatabase.ModScript.Commands;

namespace YAMLDatabase.ModScript
{
    /// <summary>
    /// Parses ModScript (.nfsms) files and provides a list of command objects
    /// </summary>
    public class ModScriptParser
    {
        private readonly string _filename;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModScriptParser"/> class.
        /// </summary>
        /// <param name="filename">The name of the script file</param>
        public ModScriptParser(string filename)
        {
            _filename = filename;
        }

        /// <summary>
        /// Parses the script file and returns command objects
        /// </summary>
        /// <returns>A series of <see cref="BaseModScriptCommand"/> objects</returns>
        public IEnumerable<BaseModScriptCommand> Parse()
        {
            foreach (var line in File.ReadLines(_filename).Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith('#')))
            {
                //Debug.WriteLine(line);

                var parts = line.Split('"')
                    .Select((element, index) => index % 2 == 0  // If even index
                        ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                        : new[] { element })  // Keep the entire item
                    .SelectMany(element => element).ToList();

                for (var index = 0; index < parts.Count; index++)
                {
                    var part = parts[index];
                    if (part.StartsWith("0x"))
                    {
                        parts[index] = $"0x{part.Substring(2).ToUpper()}";
                    }
                }

                BaseModScriptCommand command = parts[0] switch
                {
                    "append_array" => new AppendArrayModScriptCommand(),
                    "version" => new VersionModScriptCommand(),
                    "game" => new GameModScriptCommand(),
                    "resize_field" => new ResizeFieldModScriptCommand(),
                    "update_field" => new UpdateFieldModScriptCommand(),
                    "copy_node" => new CopyNodeModScriptCommand(),
                    "add_node" => new AddNodeModScriptCommand(),
                    "change_vault" => new ChangeVaultModScriptCommand(),
                    "copy_fields" => new CopyFieldsModScriptCommand(),
                    "delete_node" => new DeleteNodeModScriptCommand(),
                    "add_field" => new AddFieldModScriptCommand(),
                    "delete_field" => new DeleteFieldModScriptCommand(),
                    "rename_node" => new RenameNodeModScriptCommand(),
                    _ => new GenericModScriptCommand(line)
                };

                command.Line = line;
                command.Parse(parts);

                yield return command;
            }
        }
    }
}
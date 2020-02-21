using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

                BaseModScriptCommand command;

                switch (parts[0])
                {
                    case "version":
                        command = new VersionModScriptCommand();
                        break;
                    case "game":
                        command = new GameModScriptCommand();
                        break;
                    case "resize_field":
                        command = new ResizeFieldModScriptCommand();
                        break;
                    case "update_field":
                        command = new UpdateFieldModScriptCommand();
                        break;
                    case "copy_node":
                        command = new CopyNodeModScriptCommand();
                        break;
                    case "add_node":
                        command = new AddNodeModScriptCommand();
                        break;
                    case "change_vault":
                        command = new ChangeVaultModScriptCommand();
                        break;
                    case "copy_fields":
                        command = new CopyFieldsModScriptCommand();
                        break;
                    case "delete_node":
                        command = new DeleteNodeModScriptCommand();
                        break;
                    case "add_field":
                        command = new AddFieldModScriptCommand();
                        break;
                    case "delete_field":
                        command = new DeleteFieldModScriptCommand();
                        break;
                    case "rename_node":
                        command = new RenameNodeModScriptCommand();
                        break;
                    default:
                        //Debug.WriteLine("Unknown verb: {0}", new object[] { parts[0] });
                        command = new GenericModScriptCommand(line);
                        break;
                }

                command.Parse(parts);

                yield return command;
            }
        }
    }
}
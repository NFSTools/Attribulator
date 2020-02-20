using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
                var parts = line.Split('"')
                    .Select((element, index) => index % 2 == 0  // If even index
                        ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                        : new[] { element })  // Keep the entire item
                    .SelectMany(element => element).ToList();
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
                    default:
                        Debug.WriteLine("Unknown verb: {0}", new object[] { parts[0] });
                        command = new GenericModScriptCommand(line);
                        break;
                }

                command.Parse(parts);

                yield return command;
            }
        }
    }
}
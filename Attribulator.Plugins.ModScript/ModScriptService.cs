using System;
using System.Collections.Generic;
using System.Linq;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript
{
    public class ModScriptService : IModScriptService
    {
        private readonly Dictionary<string, Func<long, string, List<string>, IModScriptCommand>> _commandMappings =
            new();

        public IEnumerable<IModScriptCommand> ParseCommands(IEnumerable<string> commands)
        {
            var lineNumber = 0L;
            foreach (var line in commands.Select(s => s.Trim()))
            {
                lineNumber++;
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;

                var parts = line.Split('"')
                    .Select((element, index) => index % 2 == 0 // If even index
                        ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) // Split the item
                        : new[] { element }) // Keep the entire item
                    .SelectMany(element => element).ToList();

                // Find command
                if (_commandMappings.TryGetValue(parts[0], out var creator))
                {
                    IModScriptCommand command;
                    try
                    {
                        command = creator(lineNumber, line, parts);
                    }
                    catch (Exception exception)
                    {
                        throw new CommandParseException($"Failed to parse command at line {lineNumber}: {line}",
                            exception);
                    }

                    yield return command;
                }
                else
                {
                    throw new CommandParseException($"Unknown command: {parts[0]} (line {lineNumber} [{line}])");
                }
            }
        }

        public void RegisterCommand<TCommand>(string name) where TCommand : IParseableModScriptCommand<TCommand>
        {
            _commandMappings.Add(name, (lineNumber, line, parts) =>
            {
                var cmd = TCommand.Parse(parts);
                cmd.LineNumber = lineNumber;
                cmd.Line = line;
                return cmd;
            });
        }

        public IEnumerable<string> GetAvailableCommandNames()
        {
            return _commandMappings.Keys;
        }
    }
}
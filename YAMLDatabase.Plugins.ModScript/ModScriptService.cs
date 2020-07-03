using System;
using System.Collections.Generic;
using System.Linq;
using YAMLDatabase.ModScript.API;

namespace YAMLDatabase.Plugins.ModScript
{
    public class ModScriptService : IModScriptService
    {
        private readonly Dictionary<string, Func<string, IModScriptCommand>> _commandMappings =
            new Dictionary<string, Func<string, IModScriptCommand>>();

        public IEnumerable<IModScriptCommand> ParseCommands(IEnumerable<string> commands)
        {
            foreach (var command in commands.Select(s => s.Trim()))
            {
                if (string.IsNullOrEmpty(command)) continue;
                if (command.StartsWith("#", StringComparison.Ordinal)) continue;

                var parts = command.Split('"')
                    .Select((element, index) => index % 2 == 0 // If even index
                        ? element.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries) // Split the item
                        : new[] {element}) // Keep the entire item
                    .SelectMany(element => element).ToList();

                for (var index = 0; index < parts.Count; index++)
                {
                    var part = parts[index];
                    if (part.StartsWith("0x", StringComparison.Ordinal))
                        parts[index] = $"0x{part.Substring(2).ToUpper()}";
                }

                // Find command
                if (_commandMappings.TryGetValue(parts[0], out var creator))
                {
                    var newCommand = creator(command);
                    newCommand.Parse(parts);

                    yield return newCommand;
                }
                else
                {
                    throw new CommandParseException($"Unknown command: {parts[0]}");
                }
            }
        }

        public void RegisterCommand<TCommand>(string name) where TCommand : IModScriptCommand, new()
        {
            _commandMappings.Add(name, line => new TCommand {Line = line});
        }

        public IEnumerable<string> GetAvailableCommandNames()
        {
            return _commandMappings.Keys;
        }
    }
}
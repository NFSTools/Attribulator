using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands;

public class ExecScriptModScriptCommand : BaseModScriptCommand, IParseableModScriptCommand<ExecScriptModScriptCommand>
{
    public required string FileName { get; init; }

    public static ExecScriptModScriptCommand Parse(List<string> parts)
    {
        if (parts.Count != 2) throw new CommandParseException($"Expected 2 tokens, got {parts.Count}");

        return new ExecScriptModScriptCommand
        {
            FileName = parts[1]
        };
    }

    protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
    {
        throw new System.NotImplementedException("This command should not be executed directly");
    }
}
using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands;

public class ExecScriptModScriptCommand : BaseModScriptCommand
{
    public string FileName { get; set; }

    public override void Parse(List<string> parts)
    {
        if (parts.Count != 2) throw new CommandParseException($"Expected 2 tokens, got {parts.Count}");
        FileName = parts[1];
    }

    public override void Execute(DatabaseHelper databaseHelper)
    {
        throw new System.NotImplementedException("This command should not be executed directly");
    }
}
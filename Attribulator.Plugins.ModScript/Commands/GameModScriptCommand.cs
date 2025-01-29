using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class GameModScriptCommand : BaseModScriptCommand, IParseableModScriptCommand<GameModScriptCommand>
    {
        public string Game { get; private set; }

        public static GameModScriptCommand Parse(List<string> parts)
        {
            return new GameModScriptCommand
            {
                Game = parts[1]
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            //
        }
    }
}
using System.Collections.Generic;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.ModScript.API
{
    /// <summary>
    ///     Exposes a basic interface for a ModScript command.
    /// </summary>
    public interface IModScriptCommand
    {
        /// <summary>
        ///     Gets or sets the command string.
        /// </summary>
        public string Line { get; set; }

        /// <summary>
        ///     Gets or sets the command line number.
        /// </summary>
        public long LineNumber { get; set; }

        // /// <summary>
        // ///     Parses the given command tokens.
        // /// </summary>
        // /// <param name="parts">The tokens to be parsed.</param>
        // void Parse(List<string> parts);

        void Execute<TKey>(DatabaseHelper<TKey> databaseHelper) where TKey : struct, IKey<TKey>;
    }

    public interface IParseableModScriptCommand<TSelf> : IModScriptCommand
        where TSelf : IParseableModScriptCommand<TSelf>
    {
        static abstract TSelf Parse(List<string> parts);
    }
}
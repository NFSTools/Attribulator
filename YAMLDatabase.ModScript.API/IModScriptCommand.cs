using System.Collections.Generic;

namespace YAMLDatabase.ModScript.API
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
        ///     Parses the given command tokens.
        /// </summary>
        /// <param name="parts">The tokens to be parsed.</param>
        void Parse(List<string> parts);

        /// <summary>
        ///     Executes the command.
        /// </summary>
        /// <param name="databaseHelper">An instance of the <see cref="DatabaseHelper" /> class.</param>
        void Execute(DatabaseHelper databaseHelper);
    }
}
using System.Collections.Generic;

namespace YAMLDatabase.ModScript.API
{
    /// <summary>
    ///     Exposes an interface for working with ModScript files.
    /// </summary>
    public interface IModScriptService
    {
        /// <summary>
        ///     Parses the given command strings and produces <see cref="IModScriptCommand" /> objects.
        /// </summary>
        /// <param name="commands">An instance of <see cref="IEnumerable{T}" /> that produces command strings.</param>
        /// <returns>A stream of <see cref="IModScriptCommand" /> objects.</returns>
        IEnumerable<IModScriptCommand> ParseCommands(IEnumerable<string> commands);

        /// <summary>
        ///     Registers a new command type under the given name.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        void RegisterCommand<TCommand>(string name) where TCommand : IModScriptCommand, new();
    }
}
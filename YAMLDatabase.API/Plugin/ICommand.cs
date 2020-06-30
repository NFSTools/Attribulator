namespace YAMLDatabase.API.Plugin
{
    /// <summary>
    /// Exposes an interface for a command.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <returns>The return code of the command (0 for success)</returns>
        int Execute();
    }
}
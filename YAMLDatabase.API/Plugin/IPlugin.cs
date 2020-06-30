namespace YAMLDatabase.API.Plugin
{
    /// <summary>
    ///     Exposes an interface for a plugin.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        ///     Gets the name of the plugin.
        /// </summary>
        /// <returns>The name of the plugin.</returns>
        string GetName();
    }
}
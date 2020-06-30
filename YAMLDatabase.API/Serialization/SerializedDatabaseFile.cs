using System.Collections.Generic;

namespace YAMLDatabase.API.Serialization
{
    /// <summary>
    ///     Represents the serialized version of <see cref="YAMLDatabase.API.Data.LoadedFile" />.
    /// </summary>
    public class SerializedDatabaseFile
    {
        /// <summary>
        ///     Gets or sets the name of the file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the group ID of the file.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        ///     Gets or sets the list of vault names.
        /// </summary>
        public List<string> Vaults { get; set; }
    }
}
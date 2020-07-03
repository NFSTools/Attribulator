using System.Collections.Generic;

namespace YAMLDatabase.API.Serialization
{
    /// <summary>
    ///     Simple database info container
    /// </summary>
    public class SerializedDatabaseInfo
    {
        /// <summary>
        ///     Gets or sets the list of serialized classes.
        /// </summary>
        public List<SerializedDatabaseClass> Classes { get; set; }

        /// <summary>
        ///     Gets or sets the list of serialized type records.
        /// </summary>
        public List<SerializedTypeInfo> Types { get; set; }

        /// <summary>
        ///     Gets or sets the list of serialized file records.
        /// </summary>
        public List<SerializedDatabaseFile> Files { get; set; }

        /// <summary>
        ///     Gets or sets the name of the primary vault.
        /// </summary>
        public string PrimaryVaultName { get; set; }
    }
}
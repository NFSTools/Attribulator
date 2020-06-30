using System.Collections.Generic;

namespace YAMLDatabase.API.Serialization
{
    /// <summary>
    ///     Represents the serialized version of <see cref="VaultLib.Core.Data.VltCollection" />.
    /// </summary>
    public class SerializedCollection
    {
        /// <summary>
        ///     Gets or sets the name of the parent collection.
        /// </summary>
        public string ParentName { get; set; }

        /// <summary>
        ///     Gets or sets the name of the collection.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the collection data map.
        /// </summary>
        public Dictionary<string, object> Data { get; set; }
    }
}
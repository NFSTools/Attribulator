using System.Collections.Generic;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.API.Serialization
{
    /// <summary>
    ///     Represents the serialized version of <see cref="VaultLib.Core.Data.VltCollection" />.
    /// </summary>
    public class SerializedCollection<TKey> where TKey : struct, IKey<TKey>
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
        public VltDataTable<TKey> Data { get; set; }
    }
}
using System.Collections.Generic;

namespace Attribulator.API.Serialization
{
    /// <summary>
    ///     Represents the serialized version of <see cref="VaultLib.Core.Data.VltClass" />.
    /// </summary>
    public class SerializedDatabaseClass
    {
        /// <summary>
        ///     Gets or sets the name of the class.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the list of fields.
        /// </summary>
        public List<SerializedDatabaseClassField> Fields { get; set; }
    }
}
namespace YAMLDatabase.API.Serialization
{
    /// <summary>
    ///     Represents the serialized version of <see cref="VaultLib.Core.DB.DatabaseTypeInfo" />.
    /// </summary>
    public class SerializedTypeInfo
    {
        /// <summary>
        ///     Gets or sets the name of the type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the size of the type.
        /// </summary>
        public uint Size { get; set; }
    }
}
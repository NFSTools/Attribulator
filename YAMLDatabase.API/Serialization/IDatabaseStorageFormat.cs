using System.Collections.Generic;
using VaultLib.Core.DB;
using YAMLDatabase.API.Data;

namespace YAMLDatabase.API.Serialization
{
    /// <summary>
    ///     Exposes an interface for reading and writing serialized databases.
    /// </summary>
    public interface IDatabaseStorageFormat
    {
        /// <summary>
        ///     Deserializes data in the given directory and loads it into the given database.
        /// </summary>
        /// <param name="sourceDirectory">The path to the directory to read data from.</param>
        /// <param name="destinationDatabase">The <see cref="Database" /> instance to load data into.</param>
        /// <returns>
        ///     A new instance of the <see cref="SerializedDatabaseInfo" /> class with information about the serialized
        ///     database.
        /// </returns>
        IEnumerable<LoadedFile> Deserialize(string sourceDirectory, Database destinationDatabase);

        /// <summary>
        ///     Serializes data in the given database to files in the given directory.
        /// </summary>
        /// <param name="sourceDatabase">The <see cref="Database" /> instance to load data from.</param>
        /// <param name="destinationDirectory">The path to the directory to write data to.</param>
        /// <param name="loadedFiles">The loaded files</param>
        void Serialize(Database sourceDatabase, string destinationDirectory, IEnumerable<LoadedFile> loadedFiles);

        /// <summary>
        ///     Gets the identifier of the storage format.
        /// </summary>
        /// <returns>The format identifier.</returns>
        string GetFormatId();

        /// <summary>
        ///     Gets the name of the storage format.
        /// </summary>
        /// <returns>The format name.</returns>
        string GetFormatName();

        /// <summary>
        ///     Returns a value indicating whether the storage format can read from the given directory.
        /// </summary>
        /// <param name="sourceDirectory">The directory to test.</param>
        /// <returns><c>true</c> if the storage format can read from the directory; otherwise, <c>false</c>.</returns>
        bool CanDeserializeFrom(string sourceDirectory);

        /// <summary>
        ///     Computes a hash of the data stored for the given file.
        /// </summary>
        /// <param name="sourceDirectory">The base directory to load data from.</param>
        /// <param name="file">The file information object.</param>
        /// <returns>A hash string.</returns>
        string ComputeHash(string sourceDirectory, LoadedFile file);
    }
}
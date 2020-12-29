﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Attribulator.API.Data;
using VaultLib.Core;
using VaultLib.Core.DB;

namespace Attribulator.API.Serialization
{
    /// <summary>
    ///     Exposes an interface for reading and writing serialized databases.
    /// </summary>
    public interface IDatabaseStorageFormat
    {
        /// <summary>
        ///     Deserializes and returns the INFORMATION about the database stored in the given directory.
        /// </summary>
        /// <param name="sourceDirectory">The path to the directory to read data from.</param>
        /// <returns>A new instance of the <see cref="SerializedDatabaseInfo" /> object containing information about the database.</returns>
        SerializedDatabaseInfo LoadInfo(string sourceDirectory);

        /// <summary>
        ///     Deserializes data in the given directory and loads it into the given database.
        /// </summary>
        /// <param name="sourceDirectory">The path to the directory to read data from.</param>
        /// <param name="destinationDatabase">The <see cref="Database" /> instance to load data into.</param>
        /// <param name="fileNames">The names of the database files to load.</param>
        /// <returns>
        ///     An enumerable object of <see cref="LoadedFile" /> instances.
        /// </returns>
        Task<IEnumerable<LoadedFile>> DeserializeAsync(string sourceDirectory, Database destinationDatabase,
            IEnumerable<string> fileNames = null);

        /// <summary>
        ///     Serializes data in the given database to files in the given directory.
        /// </summary>
        /// <param name="sourceDatabase">The <see cref="Database" /> instance to load data from.</param>
        /// <param name="destinationDirectory">The path to the directory to write data to.</param>
        /// <param name="loadedFiles">The loaded files</param>
        /// <param name="filterFunc"></param>
        void Serialize(Database sourceDatabase, string destinationDirectory, IEnumerable<LoadedFile> loadedFiles,
            Func<Vault, bool> filterFunc = null);

        /// <summary>
        ///     Generates backups of the given files.
        /// </summary>
        /// <param name="srcDirectory"></param>
        /// <param name="destinationDirectory">The path to the directory to write backups to.</param>
        /// <param name="file"></param>
        /// <param name="vaults"></param>
        void Backup(string srcDirectory, string destinationDirectory, LoadedFile file, IEnumerable<Vault> vaults);

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
        ValueTask<string> ComputeHashAsync(string sourceDirectory, SerializedDatabaseFile file);
    }
}
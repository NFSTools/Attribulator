using System.Collections.Generic;
using Attribulator.API.Data;
using VaultLib.Core.DB;

namespace Attribulator.API
{
    /// <summary>
    /// Exposes an interface for a VLT data profile.
    /// </summary>
    public interface IProfile
    {
        /// <summary>
        /// Loads VLT files from the given directory into the given <see cref="Database"/> object.
        /// </summary>
        /// <param name="database">The instance of <see cref="Database"/> to load data into.</param>
        /// <param name="directory">The directory to load VLT files from.</param>
        /// <returns>An enumerator of <see cref="LoadedFile"/> objects.</returns>
        IEnumerable<LoadedFile> LoadFiles(Database database, string directory);

        /// <summary>
        /// Saves the given files to the given directory.
        /// </summary>
        /// <param name="database">The <see cref="Database"/> instance.</param>
        /// <param name="directory">The directory to save files to.</param>
        /// <param name="files">The list of <see cref="LoadedFile"/> objects to save.</param>
        void SaveFiles(Database database, string directory, IEnumerable<LoadedFile> files);

        /// <summary>
        /// Gets the name of the profile.
        /// </summary>
        /// <returns>The name of the profile.</returns>
        /// <example><code>"Need for Speed: Most Wanted"</code></example>
        string GetName();

        /// <summary>
        /// Gets the game ID of the profile.
        /// </summary>
        /// <returns>The game ID of the profile</returns>
        /// <example><code>MOST_WANTED</code></example>
        string GetGameId();

        /// <summary>
        /// Gets the database type of the profile.
        /// </summary>
        /// <returns>The database type.</returns>
        DatabaseType GetDatabaseType();
    }
}
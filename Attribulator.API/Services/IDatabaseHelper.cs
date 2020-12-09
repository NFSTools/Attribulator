using System.Collections.Generic;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;

namespace Attribulator.API.Services
{
    /// <summary>
    ///     Exposes an interface for easily retrieving data from a <see cref="Database" />.
    /// </summary>
    public interface IDatabaseHelper
    {
        /// <summary>
        ///     Gets the collections in the database.
        /// </summary>
        /// <returns>The collections in the database.</returns>
        IEnumerable<VltCollection> GetCollections();

        /// <summary>
        ///     Gets the collections under the given class name.
        /// </summary>
        /// <param name="className">The class name to filter by.</param>
        /// <returns>The collections under the given class name.</returns>
        IEnumerable<VltCollection> GetCollections(string className);

        /// <summary>
        ///     Gets the collections under the given class.
        /// </summary>
        /// <param name="class">The class to filter by.</param>
        /// <returns>The collections under the given class.</returns>
        IEnumerable<VltCollection> GetCollections(VltClass @class);

        /// <summary>
        ///     Gets the collections under the given vault.
        /// </summary>
        /// <param name="vault">The vault to filter by.</param>
        /// <returns>The collections under the given vault.</returns>
        IEnumerable<VltCollection> GetCollections(Vault vault);
    }
}
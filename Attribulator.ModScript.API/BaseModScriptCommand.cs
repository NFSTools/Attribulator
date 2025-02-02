using System.Collections.Generic;
using System.Globalization;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Hashing;

namespace Attribulator.ModScript.API
{
    /// <summary>
    ///     Base class for ModScript commands.
    /// </summary>
    public abstract class BaseModScriptCommand : IModScriptCommand
    {
        public string Line { get; set; }
        public long LineNumber { get; set; }

        void IModScriptCommand.Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            Execute(databaseHelper);
        }

        protected abstract void Execute<TKey>(DatabaseHelper<TKey> databaseHelper) where TKey : struct, IKey<TKey>;

        /// <summary>
        ///     Finds the collection with the given name in the given class.
        /// </summary>
        /// <param name="database">An instance of the <see cref="DatabaseHelper" /> class.</param>
        /// <param name="className">The class name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="throwOnMissing">Whether to throw an exception if the collection is not found.</param>
        /// <returns>An instance of the <see cref="VltCollection" /> class.</returns>
        /// <exception cref="CommandExecutionException">if the collection cannot be found</exception>
        protected static VltCollection<TKey>? GetCollection<TKey>(DatabaseHelper<TKey> database, string className,
            string collectionName,
            bool throwOnMissing = true) where TKey : struct, IKey<TKey>
        {
            var collection = database.FindCollectionByName(className, collectionName);

            if (collection != null)
            {
                return collection;
            }

            if (throwOnMissing)
                throw new CommandExecutionException($"Cannot find collection: {className}/{collectionName}");
            return null;
        }

        protected static void RemoveCollectionFromCache<TKey>(VltCollection<TKey> vltCollection)
            where TKey : struct, IKey<TKey>
        {
            // CollectionCache.Remove((vltCollection.Class.Name, vltCollection.Name));
        }
    }
}
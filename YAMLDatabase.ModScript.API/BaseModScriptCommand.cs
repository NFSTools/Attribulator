using System;
using System.Collections.Generic;
using System.Globalization;
using VaultLib.Core.Data;
using VaultLib.Core.Hashing;

namespace YAMLDatabase.ModScript.API
{
    /// <summary>
    ///     Base class for ModScript commands.
    /// </summary>
    public abstract class BaseModScriptCommand : IModScriptCommand
    {
        public string Line { get; set; }
        public long LineNumber { get; set; }

        /// <inheritdoc />
        public abstract void Parse(List<string> parts);

        /// <inheritdoc />
        public abstract void Execute(DatabaseHelper databaseHelper);

        /// <summary>
        ///     Finds the collection with the given name in the given class.
        /// </summary>
        /// <param name="database">An instance of the <see cref="DatabaseHelper" /> class.</param>
        /// <param name="className">The class name.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="throwOnMissing">Whether to throw an exception if the collection is not found.</param>
        /// <returns>An instance of the <see cref="VltCollection" /> class.</returns>
        /// <exception cref="CommandExecutionException">if the collection cannot be found</exception>
        protected VltCollection GetCollection(DatabaseHelper database, string className, string collectionName,
            bool throwOnMissing = true)
        {
            var collection = database.FindCollectionByName(className, collectionName);

            if (collection == null && throwOnMissing)
                throw new CommandExecutionException($"Cannot find collection: {className}/{collectionName}");

            return collection;
        }

        /// <summary>
        ///     Finds the field with the given name in the given class.
        /// </summary>
        /// <param name="vltClass">The <see cref="VltClass" /> object to search in.</param>
        /// <param name="fieldName">The field name.</param>
        /// <returns>An instance of the <see cref="VltClassField" /> class.</returns>
        /// <exception cref="CommandExecutionException">if the field cannot be found</exception>
        protected VltClassField GetField(VltClass vltClass, string fieldName)
        {
            if (vltClass == null) throw new CommandExecutionException("GetField() was given a null VltClass!");

            if (vltClass.TryGetField(fieldName, out var field)) return field;

            throw new CommandExecutionException($"Cannot find field: {vltClass.Name}[{fieldName}]");
        }

        /// <summary>
        ///     Converts the given hash-string to its source string if possible.
        /// </summary>
        /// <param name="hashString">The string to convert.</param>
        /// <returns>The original string.</returns>
        protected string CleanHashString(string hashString)
        {
            if (hashString.StartsWith("0x", StringComparison.Ordinal))
                hashString =
                    HashManager.ResolveVLT(uint.Parse(hashString.Substring(2), NumberStyles.AllowHexSpecifier));

            return hashString;
        }
    }
}
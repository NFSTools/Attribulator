using System;
using System.Collections.Generic;
using YAMLDatabase.API.Serialization;

namespace YAMLDatabase.API.Services
{
    /// <summary>
    ///     Exposes an interface for registering and retrieving storage formats.
    /// </summary>
    public interface IStorageFormatService
    {
        /// <summary>
        ///     Registers a new storage format type.
        /// </summary>
        /// <typeparam name="TStorageFormat">The storage format type.</typeparam>
        void RegisterStorageFormat<TStorageFormat>() where TStorageFormat : IDatabaseStorageFormat;

        /// <summary>
        ///     Registers a new storage format type.
        /// </summary>
        void RegisterStorageFormat(Type storageFormatType);

        /// <summary>
        ///     Gets the registered storage formats.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}" /> that produces the storage formats.</returns>
        IEnumerable<IDatabaseStorageFormat> GetStorageFormats();

        /// <summary>
        ///     Gets the storage format mapped to the given format ID.
        /// </summary>
        /// <param name="formatId">The format ID.</param>
        /// <returns>The <see cref="IDatabaseStorageFormat" /> object.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the format cannot be found.</exception>
        IDatabaseStorageFormat GetStorageFormat(string formatId);
    }
}
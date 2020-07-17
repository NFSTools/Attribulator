using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Attribulator.API.Data;
using VaultLib.Core.DB;

namespace Attribulator.API.Serialization
{
    /// <summary>
    ///     Base class for storage formats to inherit from.
    /// </summary>
    public abstract class BaseStorageFormat : IDatabaseStorageFormat
    {
        /// <inheritdoc />
        public abstract SerializedDatabaseInfo LoadInfo(string sourceDirectory);

        /// <inheritdoc />
        public abstract Task<IEnumerable<LoadedFile>> DeserializeAsync(string sourceDirectory,
            Database destinationDatabase, IEnumerable<string> fileNames = null);

        /// <inheritdoc />
        public abstract void Serialize(Database sourceDatabase, string destinationDirectory,
            IEnumerable<LoadedFile> loadedFiles);

        /// <inheritdoc />
        public abstract string GetFormatId();

        /// <inheritdoc />
        public abstract string GetFormatName();

        /// <inheritdoc />
        public abstract bool CanDeserializeFrom(string sourceDirectory);

        /// <inheritdoc />
        public virtual async ValueTask<string> ComputeHashAsync(string sourceDirectory,
            SerializedDatabaseFile loadedFile)
        {
            var path = Path.Combine(sourceDirectory, loadedFile.Group, loadedFile.Name);

            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .OrderBy(p => p).ToList();

            if (files.Count <= 0) return string.Empty;
            var md5 = MD5.Create();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                // hash path
                var relativePath = file.Substring(path.Length + 1);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                var contentBytes = await File.ReadAllBytesAsync(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }
    }
}
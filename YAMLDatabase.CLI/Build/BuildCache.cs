using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace YAMLDatabase.CLI.Build
{
    /// <summary>
    ///     Represents a file hash cache. This is used to shorten BIN compilation times.
    /// </summary>
    [JsonObject]
    public class BuildCache
    {
        [JsonProperty("entries")]
        public ConcurrentDictionary<string, BuildCacheEntry> Entries { get; set; } =
            new ConcurrentDictionary<string, BuildCacheEntry>();

        [JsonProperty("last_updated")] public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

        /// <summary>
        ///     Retrieves the cache entry with the given name.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The cache entry with the given name.</returns>
        public BuildCacheEntry FindEntry(string name)
        {
            return Entries.TryGetValue(name, out var value) ? value : null;
        }
    }

    /// <summary>
    ///     Represents an entry in a <see cref="BuildCache" /> object.
    /// </summary>
    [JsonObject]
    public class BuildCacheEntry
    {
        /// <summary>
        ///     Gets or sets the entry hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        ///     Gets or sets the list of dependencies.
        /// </summary>
        public HashSet<string> Dependencies { get; set; } = new HashSet<string>();

        /// <summary>
        ///     Gets or sets the modification timestamp.
        /// </summary>
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;
    }
}
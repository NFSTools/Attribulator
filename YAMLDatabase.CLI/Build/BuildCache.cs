using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace YAMLDatabase.CLI.Build
{
    /// <summary>
    ///     Represents a file hash cache. This is used to shorten BIN compilation times.
    /// </summary>
    [JsonObject]
    public class BuildCache
    {
        [JsonProperty("hashes")]
        public ConcurrentDictionary<string, string> HashMap { get; set; } = new ConcurrentDictionary<string, string>();

        [JsonProperty("last_updated")] public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

        /// <summary>
        ///     Returns the hash associated with the given key.
        ///     If no hash exists, returns <c>null</c>.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>The hash, if one is found; otherwise, <c>null</c>.</returns>
        public string GetHash(string key)
        {
            return HashMap.TryGetValue(key, out var value) ? value : null;
        }
    }
}
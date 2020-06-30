using System;
using VaultLib.Core.DB;
using YAMLDatabase.API.Serialization;

namespace YAMLDatabase.Plugins.YAMLSupport
{
    /// <summary>
    ///     Implements the YAML storage format.
    /// </summary>
    public class YamlStorageFormat : IDatabaseStorageFormat
    {
        public SerializedDatabaseInfo Deserialize(string sourceDirectory, Database destinationDatabase)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Database sourceDatabase, string destinationDirectory)
        {
            throw new NotImplementedException();
        }

        public string GetFormatId()
        {
            throw new NotImplementedException();
        }

        public string GetFormatName()
        {
            throw new NotImplementedException();
        }

        public bool CanDeserializeFrom(string sourceDirectory)
        {
            throw new NotImplementedException();
        }
    }
}
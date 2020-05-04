using System.Collections.Generic;
using VaultLib.Core;
using YamlDotNet.Serialization;

namespace YAMLDatabase.Core
{
    public class LoadedDatabaseFile
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public List<string> Vaults { get; set; }

        [YamlIgnore]
        public List<Vault> LoadedVaults { get; set; }
    }
}
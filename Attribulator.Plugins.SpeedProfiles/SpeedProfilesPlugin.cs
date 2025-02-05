using System.IO;
using System.Reflection;
using Attribulator.API.Plugin;
using Attribulator.API.Utils;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.SpeedProfiles
{
    public class SpeedProfilesPlugin : IPlugin
    {
        public string GetName()
        {
            return "Speed Profiles";
        }

        public void Init()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var resourcesDir = Path.Combine(assemblyDir, "Resources");
            HashManager.LoadDictionary(Path.Combine(resourcesDir, "hashes.txt"));
            KeyUtils.LoadBinDictionary(Path.Combine(resourcesDir, "hashes_bin.txt"));
        }
    }
}
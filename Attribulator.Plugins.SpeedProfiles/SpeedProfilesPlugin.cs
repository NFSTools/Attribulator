using System.IO;
using System.Reflection;
using Attribulator.API.Plugin;
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
            HashManager.LoadDictionary(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Resources", "hashes.txt"));
        }
    }
}
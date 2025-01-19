using System.IO;
using System.Reflection;
using Attribulator.API.Plugin;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.BPSupport
{
    public class BurnoutParadisePlugin : IPlugin
    {
        public string GetName()
        {
            return "Burnout Paradise Support";
        }

        public void Init()
        {
            HashManager.LoadDictionary(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Resources", "hashes.txt"));
        }
    }
}
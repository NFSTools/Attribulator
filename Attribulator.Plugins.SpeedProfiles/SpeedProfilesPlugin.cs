using System.IO;
using System.Reflection;
using Attribulator.API.Plugin;
using VaultLib.Core.Hashing;
using CarbonModule = VaultLib.Support.Carbon;
using MostWantedModule = VaultLib.Support.MostWanted;
using ProStreetModule = VaultLib.Support.ProStreet;
using UndercoverModule = VaultLib.Support.Undercover;
using WorldModule = VaultLib.Support.World;

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

            new CarbonModule.ModuleDef().Load();
            new MostWantedModule.ModuleDef().Load();
            new ProStreetModule.ModuleDef().Load();
            new UndercoverModule.ModuleDef().Load();
            new WorldModule.ModuleDef().Load();
        }
    }
}
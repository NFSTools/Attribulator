using System.IO;
using System.Reflection;
using Attribulator.API.Plugin;
using Attribulator.Plugins.BPSupport.Types;
using VaultLib.Core;
using VaultLib.Core.Exports;
using VaultLib.Core.Exports.Implementations;
using VaultLib.Core.Hashing;
using VaultLib.ModernBase.Exports;
using VaultLib.ModernBase.Structures;

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
            ExportFactory.SetClassLoadCreator<ClassLoad64>("BURNOUT_PARADISE");
            ExportFactory.SetCollectionLoadCreator<CollectionLoad64>("BURNOUT_PARADISE");
            ExportFactory.SetDatabaseLoadCreator<DatabaseLoad>("BURNOUT_PARADISE");
            ExportFactory.SetExportEntryCreator<ExportEntry64>("BURNOUT_PARADISE");
            ExportFactory.SetPointerCreator<PtrRef64>("BURNOUT_PARADISE");

            TypeRegistry.Register<RwVector2>("Attrib::Types::RwVector2", "BURNOUT_PARADISE");
            TypeRegistry.Register<RwVector3>("Attrib::Types::RwVector3", "BURNOUT_PARADISE");
        }
    }
}
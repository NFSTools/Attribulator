using System.Collections.Generic;
using VaultLib.Core.DB;

namespace YAMLDatabase.Profiles
{
    public abstract class BaseProfile
    {
        public abstract IList<LoadedDatabaseFile> LoadFiles(Database database, string directory);

        public abstract void SaveFiles(Database database, string directory, IList<LoadedDatabaseFile> files);

        public abstract string GetName();
        public abstract string GetGame();
        public abstract DatabaseType GetDatabaseType();
        public abstract IEnumerable<string> GetFilesToLoad(string directory);
    }
}
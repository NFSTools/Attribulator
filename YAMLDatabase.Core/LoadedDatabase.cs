using System.Collections.Generic;

namespace YAMLDatabase.Core
{
    public class LoadedDatabase
    {
        public List<LoadedDatabaseClass> Classes { get; set; }
        public List<LoadedTypeInfo> Types { get; set; }
        public List<LoadedDatabaseFile> Files { get; set; }
    }
}
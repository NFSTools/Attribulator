using System.Collections.Generic;

namespace YAMLDatabase.Core
{
    public class LoadedDatabaseClass
    {
        public string Name { get; set; }
        public List<LoadedDatabaseClassField> Fields { get; set; }
    }
}
using System.Collections.Generic;

namespace YAMLDatabase
{
    public class LoadedDatabaseClass
    {
        public string Name { get; set; }
        public List<LoadedDatabaseClassField> Fields { get; set; }
    }
}
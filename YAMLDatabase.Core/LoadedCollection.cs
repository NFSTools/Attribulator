using System.Collections.Generic;

namespace YAMLDatabase.Core
{
    public class LoadedCollection
    {
        public string ParentName { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Data { get; set; }
        //public List<LoadedCollection> Children { get; set; }
    }
}
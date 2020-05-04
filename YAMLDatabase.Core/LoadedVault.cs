using System.Collections.Generic;

namespace YAMLDatabase
{
    public class LoadedVault
    {
        public string Name { get; set; }
        public List<LoadedCollection> Collections { get; set; }
    }
}
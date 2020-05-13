using System.Collections;

namespace YAMLDatabase.Core
{
    public class SerializedArrayWrapper
    {
        public ushort Capacity { get; set; }
        public IList Data { get; set; }
    }
}
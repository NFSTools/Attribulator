using System.Collections;

namespace YAMLDatabase
{
    public class SerializedArrayWrapper
    {
        public ushort Capacity { get; set; }
        public IList Data { get; set; }
    }
}
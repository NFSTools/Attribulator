using System.Collections.Generic;
using System.Linq;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class CustomSerializedArray<T> : ISerializedArray
{
    public ushort Capacity { get; set; }
    public List<T> Data { get; set; }
    
    public CustomSerializedArray() {}

    public CustomSerializedArray(ushort capacity, List<T> data)
    {
        Capacity = capacity;
        Data = data;
    }

    public IEnumerable<object> GetRawItems()
    {
        return Data.AsEnumerable().Cast<object>();
    }

    public ushort GetCapacity()
    {
        return Capacity;
    }
}
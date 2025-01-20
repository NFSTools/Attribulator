using System.Collections.Generic;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal interface ISerializedArray
{
    IEnumerable<object> GetRawItems();

    ushort GetCapacity();
}
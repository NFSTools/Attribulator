using System.Collections.Generic;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class CustomSerializedCollectionData
{
    private readonly Dictionary<string, object> _data = new();

    public object GetEntry(string key)
    {
        return _data[key];
    }

    public void SetEntry(string key, object value)
    {
        _data[key] = value;
    }

    public Dictionary<string, object> GetEntries()
    {
        return _data;
    }
}
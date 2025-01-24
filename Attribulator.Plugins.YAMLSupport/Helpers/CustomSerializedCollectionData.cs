using System.Collections.Generic;
using VaultLib.Core.Data;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class CustomSerializedCollectionData
{
    private readonly VltDataTable _table;

    public CustomSerializedCollectionData() : this(new VltDataTable())
    {
    }

    public CustomSerializedCollectionData(VltDataTable table)
    {
        _table = table;
    }

    public object GetEntry(string key)
    {
        return _table.GetValue(key);
    }

    public void SetEntry(string key, object value)
    {
        _table.SetValue(key, value);
    }

    public VltDataTable GetTable()
    {
        return _table;
    }
}
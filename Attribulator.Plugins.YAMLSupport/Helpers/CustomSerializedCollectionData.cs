using System.Collections.Generic;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class CustomSerializedCollectionData<TKey> where TKey : struct, IKey<TKey>
{
    private readonly VltDataTable<TKey> _table;

    public CustomSerializedCollectionData() : this(new VltDataTable<TKey>())
    {
    }

    public CustomSerializedCollectionData(VltDataTable<TKey> table)
    {
        _table = table;
    }

    public object GetEntry(TKey key)
    {
        return _table.GetValue(key);
    }

    public void SetEntry(TKey key, object value)
    {
        _table.SetValue(key, value);
    }

    public VltDataTable<TKey> GetTable()
    {
        return _table;
    }
}
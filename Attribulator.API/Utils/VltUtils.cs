using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.API.Utils;

public static class VltUtils
{
    public record FieldIdentifier<TKey>(TKey ClassKey, TKey FieldKey)
        where TKey : struct, IKey<TKey>;

    public record CollectionIdentifier<TKey>(TKey ClassKey, TKey CollectionKey)
        where TKey : struct, IKey<TKey>;

    public static FieldIdentifier<TKey> CreateFieldIdentifier<TKey>(VltClass<TKey> vltClass,
        VltClassField<TKey> vltField)
        where TKey : struct, IKey<TKey>
    {
        return new FieldIdentifier<TKey>(
            vltClass.Key, vltField.Key);
    }
    public static FieldIdentifier<TKey> CreateFieldIdentifier<TKey>(VltClass<TKey> vltClass,
        TKey vltFieldKey)
        where TKey : struct, IKey<TKey>
    {
        return new FieldIdentifier<TKey>(
            vltClass.Key, vltFieldKey);
    }

    public static CollectionIdentifier<TKey> CreateCollectionIdentifier<TKey>(VltCollection<TKey> vltCollection)
        where TKey : struct, IKey<TKey>
    {
        return new CollectionIdentifier<TKey>(
            vltCollection.Class.Key, vltCollection.Key);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Attribulator.API.Utils;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class VltClassSchemaTypeInspector<TKey> : ITypeInspector where TKey : struct, IKey<TKey>
{
    private readonly ITypeInspector _innerInspector;
    private readonly Database<TKey> _database;
    private readonly VltClass<TKey> _vltClass;
    private readonly Dictionary<TKey, IPropertyDescriptor> _fieldDescriptors;

    public VltClassSchemaTypeInspector(ITypeInspector innerInspector, Database<TKey> database, VltClass<TKey> vltClass)
    {
        _innerInspector = innerInspector;
        _database = database;
        _vltClass = vltClass;
        _fieldDescriptors = vltClass.Fields.Values.ToDictionary(f => f.Key, CreateFieldDescriptor);
    }

    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
    {
        if (type == typeof(CustomSerializedCollectionData<TKey>))
        {
            return ((CustomSerializedCollectionData<TKey>)container!).GetTable()
                .GetEntries()
                .Select(e => _fieldDescriptors[e.Key]);
        }

        return _innerInspector.GetProperties(type, container);
    }

    public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched,
        bool caseInsensitivePropertyMatching)
    {
        // we don't need to mess with anything but CustomSerializedCollectionData
        if (type != typeof(CustomSerializedCollectionData<TKey>))
        {
            return _innerInspector.GetProperty(type, container, name, ignoreUnmatched,
                caseInsensitivePropertyMatching);
        }
        
        var fieldKey = KeyUtils.StringToKey<TKey>(name, true);
        if (_vltClass.TryGetField(fieldKey, out var vltClassField))
        {
            return CreateFieldDescriptor(vltClassField);
        }
        
        if (ignoreUnmatched)
        {
            return null!;
        }

        throw new SerializationException($"Field '{name}' doesn't exist in class.");
    }

    private IPropertyDescriptor CreateFieldDescriptor(VltClassField<TKey> field)
    {
        var fieldType = _database.TypeRegistry.ResolveFieldType(field);

        if (fieldType == null)
        {
            throw new Exception("fieldType is null, this should never happen");
        }

        // before we make the final type, we need to deal with special cases

        // special case 1: IStringValue -> string
        // special case 2: blob -> string (file path)
        if (CloakingHelper.IsTypeAStringInDisguise<TKey>(fieldType))
        {
            fieldType = typeof(string);
        }

        // arcane trickery to transparently handle arrays
        // since CustomDataEntryPropertyDescriptor accepts an arbitrary type,
        // we can just conjure a generic type at runtime and get proper
        // array deserialization for free :)
        var propType = field.IsArray
            ? typeof(CustomSerializedArray<>).MakeGenericType(fieldType)
            : fieldType;

        return new CustomDataEntryPropertyDescriptor<TKey>(field, propType);
    }

    public string GetEnumName(Type enumType, string name)
    {
        return _innerInspector.GetEnumName(enumType, name);
    }

    public string GetEnumValue(object enumValue)
    {
        return _innerInspector.GetEnumValue(enumValue);
    }
}
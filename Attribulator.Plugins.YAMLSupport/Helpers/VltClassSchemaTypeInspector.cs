using System;
using System.Collections.Generic;
using System.Linq;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class VltClassSchemaTypeInspector : ITypeInspector
{
    private readonly ITypeInspector _innerInspector;
    private readonly Database _database;
    private readonly VltClass _vltClass;
    private readonly Dictionary<string, IPropertyDescriptor> _fieldDescriptors;

    public VltClassSchemaTypeInspector(ITypeInspector innerInspector, Database database, VltClass vltClass)
    {
        _innerInspector = innerInspector;
        _database = database;
        _vltClass = vltClass;
        _fieldDescriptors = vltClass.Fields.Values.ToDictionary(f => f.Name, CreateFieldDescriptor);
    }

    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
    {
        if (type == typeof(CustomSerializedCollectionData))
        {
            return ((CustomSerializedCollectionData)container!).GetTable()
                .GetEntries()
                .Select(e => _fieldDescriptors[e.Key]);
        }

        return _innerInspector.GetProperties(type, container);
    }

    public IPropertyDescriptor GetProperty(Type type, object container, string name, bool ignoreUnmatched,
        bool caseInsensitivePropertyMatching)
    {
        // we don't need to mess with anything but CustomSerializedCollectionData
        if (type != typeof(CustomSerializedCollectionData))
        {
            return _innerInspector.GetProperty(type, container, name, ignoreUnmatched,
                caseInsensitivePropertyMatching);
        }

        // todo: error handling?
        var field = _vltClass[name];

        return CreateFieldDescriptor(field);
    }

    private IPropertyDescriptor CreateFieldDescriptor(VltClassField field)
    {
        var fieldType = _database.TypeRegistry.ResolveType(field.TypeName);

        if (fieldType == null)
        {
            throw new Exception("fieldType is null, this should never happen");
        }

        // before we make the final type, we need to deal with special cases

        // special case 1: IStringValue -> string
        // special case 2: blob -> string (file path)
        if (CloakingHelper.IsTypeAStringInDisguise(fieldType))
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

        return new CustomDataEntryPropertyDescriptor(field, propType);
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
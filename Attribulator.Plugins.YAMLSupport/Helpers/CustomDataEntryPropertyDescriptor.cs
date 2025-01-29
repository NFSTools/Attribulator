using System;
using Attribulator.API.Utils;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class CustomDataEntryPropertyDescriptor<TKey> : IPropertyDescriptor where TKey : struct, IKey<TKey>
{
    private readonly VltClassField<TKey> _field;
    private readonly Type _fieldType;

    public CustomDataEntryPropertyDescriptor(VltClassField<TKey> field,
        Type fieldType)
    {
        _field = field;
        _fieldType = fieldType;
    }

    public T GetCustomAttribute<T>() where T : Attribute
    {
        return null;
    }

    public IObjectDescriptor Read(object target)
    {
        var value = ((CustomSerializedCollectionData<TKey>)target).GetEntry(_field.Key);
        return new ObjectDescriptor(value, _fieldType, _fieldType);
    }

    public void Write(object target, object value)
    {
        ((CustomSerializedCollectionData<TKey>)target).SetEntry(_field.Key, value);
    }

    public string Name => KeyUtils.KeyToString(_field.Key);
    public bool AllowNulls => !_field.IsInLayout;
    public bool CanWrite => true;
    public Type Type => _fieldType;
    public Type TypeOverride { get; set; }
    public int Order { get; set; }
    public ScalarStyle ScalarStyle { get; set; }
    public bool Required => _field.IsInLayout;
    public Type ConverterType => null;
}
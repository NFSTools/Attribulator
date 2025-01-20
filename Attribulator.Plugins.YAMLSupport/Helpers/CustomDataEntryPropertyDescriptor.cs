using System;
using VaultLib.Core.Data;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal class CustomDataEntryPropertyDescriptor : IPropertyDescriptor
{
    private readonly VltClassField _field;
    private readonly Type _fieldType;

    public CustomDataEntryPropertyDescriptor(VltClassField field,
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
        var value = ((CustomSerializedCollectionData)target).GetEntry(_field.Name);
        return new ObjectDescriptor(value, _fieldType, _fieldType);
    }

    public void Write(object target, object value)
    {
        ((CustomSerializedCollectionData)target).SetEntry(_field.Name, value);
    }

    public string Name => _field.Name;
    public bool AllowNulls => !_field.IsInLayout;
    public bool CanWrite => true;
    public Type Type => _fieldType;
    public Type TypeOverride { get; set; }
    public int Order { get; set; }
    public ScalarStyle ScalarStyle { get; set; }
    public bool Required => _field.IsInLayout;
    public Type ConverterType => null;
}
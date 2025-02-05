using System;
using Attribulator.API.Utils;
using VaultLib.Core.DataInterfaces;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

public class VltKeyTypeConverter<TKey> : IYamlTypeConverter where TKey : struct, IKey<TKey>
{
    public bool Accepts(Type type)
    {
        return type == typeof(TKey);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return KeyUtils.StringToKey<TKey>(scalar.Value, true);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var key = (TKey)value!;
        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, KeyUtils.KeyToString(key),
            ScalarStyle.DoubleQuoted, isPlainImplicit: false, isQuotedImplicit: true));
    }
}
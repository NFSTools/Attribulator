using System;
using System.Text.RegularExpressions;
using Attribulator.API.Utils;
using VaultLib.Core.DataInterfaces;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

public partial class VltKeyTypeConverter<TKey> : IYamlTypeConverter where TKey : struct, IKey<TKey>
{
    private readonly Regex _specialStringsRegex = SpecialStringsRegex();

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
        var keyToString = KeyUtils.KeyToString(key);

        if (_specialStringsRegex.IsMatch(keyToString))
        {
            emitter.Emit(new Scalar(
                AnchorName.Empty,
                TagName.Empty,
                keyToString,
                ScalarStyle.DoubleQuoted,
                true,
                true));
        }
        else
        {
            emitter.Emit(new Scalar(keyToString));
        }
    }

    // from YamlDotNet.Serialization.EventEmitters.TypeAssigningEventEmitter
    [GeneratedRegex(
        @"^(null|Null|NULL|\~|true|True|TRUE|false|False|FALSE|y|Y|yes|Yes|YES|n|N|no|No|NO|on|On|ON|off|Off|OFF|[-+]?0b[0-1_]+|[-+]?0o?[0-7_]+|[-+]?(0|[1-9][0-9_]*)|[-+]?0x[0-9a-fA-F_]+|[-+]?[1-9][0-9_]*(:[0-5]?[0-9])+|[-+]?([0-9][0-9_]*)?\.[0-9_]*([eE][-+][0-9]+)?|[-+]?[0-9][0-9_]*(:[0-5]?[0-9])+\.[0-9_]*|[-+]?\.(inf|Inf|INF)|\.(nan|NaN|NAN))$",
        RegexOptions.Compiled)]
    private static partial Regex SpecialStringsRegex();
}
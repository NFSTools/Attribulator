using VaultLib.Core.Data;

namespace YAMLDatabase.Core
{
    public class LoadedDatabaseClassField
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public int Alignment { get; set; }
        public DefinitionFlags Flags { get; set; }
        public ushort Offset { get; set; }
        public ushort Size { get; set; }
        public ushort MaxCount { get; set; }
        public object StaticValue { get; set; }
    }
}
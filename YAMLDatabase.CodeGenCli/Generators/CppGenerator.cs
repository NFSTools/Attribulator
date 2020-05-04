using System.Linq;
using System.Text;
using VaultLib.Core.Data;

namespace YAMLDatabase.CodeGenCli.Generators
{
    public class CppGenerator : ICodeGenerator
    {
        public string GenerateClassLayout(LoadedDatabaseClass loadedDatabaseClass)
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.AppendFormat("struct {0} {{", loadedDatabaseClass.Name);
            sb.AppendLine();

            int structOffset = 0;

            foreach (var field in loadedDatabaseClass.Fields
                .Where(f => (f.Flags & DefinitionFlags.InLayout) != 0)
                .OrderBy(f => f.Offset))
            {
                // Get proper type name
                string rtn = ResolveTypeName(field.TypeName);

                if ((field.Flags & DefinitionFlags.Array) != 0)
                {
                    // write Attrib::Array padding
                    sb.AppendFormat("\tchar _array_private_{0}[8];", field.Name);
                    structOffset += 8;
                    sb.AppendLine();
                }

                if (structOffset % field.Alignment != 0)
                {
                    var pad = field.Alignment - structOffset % field.Alignment;

                    sb.AppendFormat("\tchar _align_{0}[{1}];", field.Name, pad);
                    sb.AppendLine();
                    structOffset += pad;
                }

                string fn = field.Name;

                if (fn.StartsWith("0x"))
                    fn = "unk_" + fn;
                if ((field.Flags & DefinitionFlags.Array) != 0)
                {
                    sb.AppendFormat("\t{0} {1}[{2}];", rtn, fn, field.MaxCount);
                }
                else
                {
                    sb.AppendFormat("\t{0} {1};", rtn, fn);
                }

                sb.AppendLine();
            }

            sb.AppendLine("};");

            return sb.ToString();
        }

        public string GetExtension()
        {
            return ".h";
        }

        private string ResolveTypeName(string typeName)
        {
            return typeName switch
            {
                "EA::Reflection::Bool" => "bool",
                "EA::Reflection::Int8" => "char",
                "EA::Reflection::UInt8" => "unsigned char",
                "EA::Reflection::Int16" => "short",
                "EA::Reflection::UInt16" => "unsigned short",
                "EA::Reflection::Int32" => "int",
                "EA::Reflection::UInt32" => "unsigned int",
                "EA::Reflection::Float" => "float",
                "EA::Reflection::Double" => "double",
                "EA::Reflection::Text" => "const char*",
                _ => typeName
            };
        }
    }
}
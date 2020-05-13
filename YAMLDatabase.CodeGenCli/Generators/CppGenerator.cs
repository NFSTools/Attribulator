using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VaultLib.Core.Data;
using YAMLDatabase.Core;

namespace YAMLDatabase.CodeGenCli.Generators
{
    public class CppGenerator : ICodeGenerator
    {
        public string GenerateClassLayout(LoadedDatabaseClass loadedDatabaseClass)
        {
            Debug.WriteLine("Process class: {0}", new object[] { loadedDatabaseClass.Name });

            StringBuilder sb = new StringBuilder(2048);
            sb.AppendLine("#pragma once");
            sb.AppendLine("namespace attrib_sys::layouts");
            sb.AppendLine("{");

            var baseFields = loadedDatabaseClass.Fields
                .Where(f => (f.Flags & DefinitionFlags.InLayout) != 0)
                .OrderBy(f => f.Offset)
                .ToList();
            
            if (baseFields.Count != 0)
            {
                sb.AppendLine("#pragma pack(push, 1)");
                sb.AppendFormat("\tstruct {0} {{", loadedDatabaseClass.Name);
                sb.AppendLine();

                int structOffset = 0;
                foreach (var field in baseFields)
                {
                    Debug.WriteLine("Process field: name={0} typename={1} alignment={2} maxCount={3} size={4} offset={5}/{7} flags={6}",
                        field.Name,
                        field.TypeName,
                        field.Alignment,
                        field.MaxCount,
                        field.Size,
                        field.Offset,
                        field.Flags,
                        structOffset);

                    // Align
                    if (structOffset % field.Alignment != 0)
                    {
                        int pad = field.Alignment - structOffset % field.Alignment;

                        sb.AppendFormat("\t\tchar _pad_{0}[{1}];", field.Name, pad);
                        sb.AppendLine();

                        Debug.WriteLine("alignment padding: {0}", pad);

                        structOffset += pad;
                    }

                    if (structOffset != field.Offset)
                    {
                        if (structOffset > field.Offset)
                        {
                            throw new Exception("All you had to do was FOLLOW the damn train!");
                        }

                        int diff = field.Offset - structOffset;
                        Debug.WriteLine("See {0}.h for warning about field {1}", loadedDatabaseClass.Name, field.Name);
                        sb.AppendLine("\t\t#error Could not completely assemble structure");
                        sb.AppendFormat("\t\tchar _gap_{0}[{1}];", field.Name, diff);
                        sb.AppendLine();
                        structOffset += diff;
                    }

                    // Check if it's an array
                    if ((field.Flags & DefinitionFlags.Array) != 0)
                    {
                        sb.AppendFormat("\t\tchar _private_{0}[8];", field.Name);
                        sb.AppendLine();
                        structOffset += 8;
                    }

                    string cleanName = field.Name.StartsWith("0x") ? $"unk_{field.Name}" : field.Name;
                    string resolvedTypeName = ResolveTypeName(field.TypeName);

                    // Arrays get special treatment
                    if ((field.Flags & DefinitionFlags.Array) != 0)
                    {
                        sb.AppendFormat("\t\t__declspec(align({0})) {1} {2}[{3}];", field.Alignment, resolvedTypeName, cleanName, field.MaxCount);
                        sb.AppendLine();

                        for (int i = 0; i < field.MaxCount; i++)
                        {
                            if (structOffset % field.Alignment != 0)
                            {
                                int pad = field.Alignment - structOffset % field.Alignment;
                                structOffset += pad;
                            }
                            structOffset += field.Size;
                        }
                    }
                    else
                    {
                        sb.AppendFormat("\t\t{0} {1};", resolvedTypeName, cleanName);
                        sb.AppendLine();
                        structOffset += field.Size;
                    }
                }

                if (baseFields.Count > 0)
                {
                    int maxAlign = baseFields.Max(f => f.Alignment);
                    if (structOffset % maxAlign != 0)
                    {
                        var pad = maxAlign - structOffset % maxAlign;
                        structOffset += pad;
                        sb.AppendFormat("\t\tchar _end_pad[{0}];", pad);
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("\t};");
                sb.AppendLine("#pragma pack(pop)");

                sb.AppendFormat("\tstatic_assert(sizeof({0}) == {1}, \"Incorrect structure size\");\n",
                    loadedDatabaseClass.Name, structOffset);
            }
            else
            {
                sb.AppendFormat("\tstruct {0};", loadedDatabaseClass.Name);
                sb.AppendLine();
            }

            sb.AppendLine("}");
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
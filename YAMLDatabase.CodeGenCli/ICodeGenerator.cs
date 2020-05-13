using YAMLDatabase.Core;

namespace YAMLDatabase.CodeGenCli
{
    public interface ICodeGenerator
    {
        string GenerateClassLayout(LoadedDatabaseClass loadedDatabaseClass);

        string GetExtension();
    }
}
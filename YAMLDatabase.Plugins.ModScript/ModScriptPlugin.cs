using YAMLDatabase.API.Plugin;
using YAMLDatabase.ModScript.API;
using YAMLDatabase.Plugins.ModScript.Commands;

namespace YAMLDatabase.Plugins.ModScript
{
    /// <summary>
    ///     Base class for the ModScript plugin.
    /// </summary>
    public class ModScriptPlugin : IPlugin
    {
        public ModScriptPlugin(IModScriptService modScriptService)
        {
            modScriptService.RegisterCommand<AppendArrayModScriptCommand>("append_array");
            modScriptService.RegisterCommand<VersionModScriptCommand>("version");
            modScriptService.RegisterCommand<GameModScriptCommand>("game");
            modScriptService.RegisterCommand<ResizeFieldModScriptCommand>("resize_field");
            modScriptService.RegisterCommand<UpdateFieldModScriptCommand>("update_field");
            modScriptService.RegisterCommand<CopyNodeModScriptCommand>("copy_node");
            modScriptService.RegisterCommand<AddNodeModScriptCommand>("add_node");
            modScriptService.RegisterCommand<ChangeVaultModScriptCommand>("change_vault");
            modScriptService.RegisterCommand<CopyFieldsModScriptCommand>("copy_fields");
            modScriptService.RegisterCommand<DeleteNodeModScriptCommand>("delete_node");
            modScriptService.RegisterCommand<AddFieldModScriptCommand>("add_field");
            modScriptService.RegisterCommand<DeleteFieldModScriptCommand>("delete_field");
            modScriptService.RegisterCommand<RenameNodeModScriptCommand>("rename_node");
            modScriptService.RegisterCommand<MoveNodeModScriptCommand>("move_node");
        }

        public string GetName()
        {
            return "ModScript Support";
        }
    }
}
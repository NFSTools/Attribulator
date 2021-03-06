﻿using System;
using Attribulator.API.Plugin;
using Attribulator.ModScript.API;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.Plugins.ModScript
{
    /// <summary>
    ///     Plugin factory for the ModScript plugin.
    /// </summary>
    public class ModScriptPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<IModScriptService, ModScriptService>();
            services.AddTransient<ApplyScriptCommand>();
            services.AddTransient<ApplyScriptToBinCommand>();
            services.AddTransient<AvailableCommandsCommand>();
            services.AddSingleton<ModScriptPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<ModScriptPlugin>();
        }

        public string GetId()
        {
            return "Attribulator.Plugins.ModScript";
        }
    }
}
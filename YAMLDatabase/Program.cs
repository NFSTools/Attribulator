using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;
using YAMLDatabase.Services;

namespace YAMLDatabase
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            // Setup
            var services = new ServiceCollection();
            var loaders = GetPluginLoaders();

            // Register services
            services.AddSingleton<ICommandService, CommandServiceImpl>();
            services.AddSingleton<IProfileService, ProfileServiceImpl>();
            ConfigureServices(services, loaders);

            using var serviceProvider = services.BuildServiceProvider();

            // Load commands and profiles from DI container
            LoadCommands(services, serviceProvider);
            LoadProfiles(services, serviceProvider);

            // Off to the races!
            return RunApplication(serviceProvider, args);
        }

        private static int RunApplication(IServiceProvider serviceProvider, string[] args)
        {
            var commandService = serviceProvider.GetRequiredService<ICommandService>();
            var commandTypes = commandService.GetCommandTypes().ToArray();
            return Parser.Default.ParseArguments(args, commandTypes)
                .MapResult((ICommand cmd) => cmd.Execute(), errs => 1);
        }

        private static void LoadCommands(ServiceCollection services, IServiceProvider serviceProvider)
        {
            var commandTypes = (from service in services
                where typeof(ICommand).IsAssignableFrom(service.ImplementationType)
                select service.ImplementationType).ToList();
            var commandService = serviceProvider.GetRequiredService<ICommandService>();
            foreach (var commandType in commandTypes)
            {
                commandService.RegisterCommand(commandType);
            }
        }

        private static void LoadProfiles(ServiceCollection services, IServiceProvider serviceProvider)
        {
            var profileTypes = (from service in services
                where typeof(IProfile).IsAssignableFrom(service.ImplementationType)
                select service.ImplementationType).ToList();
            var profileService = serviceProvider.GetRequiredService<IProfileService>();
            foreach (var profileType in profileTypes)
            {
                profileService.RegisterProfile(profileType);
            }
        }

        private static IEnumerable<PluginLoader> GetPluginLoaders()
        {
            // create plugin loaders
            var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");

            return (from dir in Directory.GetDirectories(pluginsDir)
                let dirName = Path.GetFileName(dir)
                select Path.Combine(dir, dirName + ".dll")
                into pluginDll
                where File.Exists(pluginDll)
                select PluginLoader.CreateFromAssemblyFile(pluginDll, new[]
                {
                    // Basic stuff
                    typeof(IPluginFactory), typeof(IServiceCollection),

                    // Application stuff
                    typeof(ICommand), typeof(IProfile),
                    
                    // CommandLineParser
                    typeof(VerbAttribute)
                })).ToList();
        }

        private static void ConfigureServices(IServiceCollection services, IEnumerable<PluginLoader> loaders)
        {
            // Create an instance of plugin types
            foreach (var loader in loaders)
            {
                foreach (var pluginType in loader
                    .LoadDefaultAssembly()
                    .GetTypes()
                    .Where(t => typeof(IPluginFactory).IsAssignableFrom(t) && !t.IsAbstract))
                {
                    // This assumes the implementation of IPluginFactory has a parameterless constructor
                    var plugin = Activator.CreateInstance(pluginType) as IPluginFactory;
                    plugin?.Configure(services);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CoreLibraries.ModuleSystem;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VaultLib.Core.Hashing;
using YAMLDatabase.API;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Serialization;
using YAMLDatabase.API.Services;
using YAMLDatabase.CLI.Commands;
using YAMLDatabase.CLI.Services;

namespace YAMLDatabase.CLI
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Setup
            var services = new ServiceCollection();
            var loaders = GetPluginLoaders();

            // Register services
            services.AddSingleton<ICommandService, CommandServiceImpl>();
            services.AddSingleton<IProfileService, ProfileServiceImpl>();
            services.AddSingleton<IStorageFormatService, StorageFormatServiceImpl>();
            services.AddSingleton<IPluginService, PluginServiceImpl>();

            // Set up logging
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            var plugins = ConfigurePlugins(services, loaders);
            await using var serviceProvider = services.BuildServiceProvider();

            // Load everything from DI container
            LoadCommands(services, serviceProvider);
            LoadProfiles(services, serviceProvider);
            LoadStorageFormats(services, serviceProvider);
            LoadPlugins(plugins, serviceProvider);

            // Load hashes
            // TODO: This code should be part of the appropriate plugin.
            HashManager.LoadDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hashes.txt"));
            new ModuleLoader("VaultLib.Support.*.dll").Load();

            // Off to the races!
            return await RunApplication(serviceProvider, args);
        }

        private static async Task<int> RunApplication(IServiceProvider serviceProvider, IEnumerable<string> args)
        {
            var commandService = serviceProvider.GetRequiredService<ICommandService>();
            var commandTypes = commandService.GetCommandTypes().ToArray();
            return await Parser.Default.ParseArguments(args, commandTypes)
                .MapResult((BaseCommand cmd) =>
                {
                    cmd.SetServiceProvider(serviceProvider);
                    return cmd.Execute();
                }, errs => Task.FromResult(1));
        }

        private static void LoadPlugins(IEnumerable<IPluginFactory> plugins, IServiceProvider serviceProvider)
        {
            var pluginService = serviceProvider.GetRequiredService<IPluginService>();

            foreach (var pluginFactory in plugins)
                pluginService.RegisterPlugin(pluginFactory.CreatePlugin(serviceProvider));
        }

        private static void LoadCommands(ServiceCollection services, IServiceProvider serviceProvider)
        {
            var commandService = serviceProvider.GetRequiredService<ICommandService>();
            var commandTypes = (from service in services
                where typeof(BaseCommand).IsAssignableFrom(service.ImplementationType)
                select service.ImplementationType).ToList();

            // First register our own commands
            commandService.RegisterCommand<PluginListCommand>();
            commandService.RegisterCommand<ProfileListCommand>();

            // Then register plugin commands
            foreach (var commandType in commandTypes) commandService.RegisterCommand(commandType);
        }

        private static void LoadProfiles(ServiceCollection services, IServiceProvider serviceProvider)
        {
            var profileTypes = (from service in services
                where typeof(IProfile).IsAssignableFrom(service.ImplementationType)
                select service.ImplementationType).ToList();
            var profileService = serviceProvider.GetRequiredService<IProfileService>();
            foreach (var profileType in profileTypes) profileService.RegisterProfile(profileType);
        }

        private static void LoadStorageFormats(ServiceCollection services, IServiceProvider serviceProvider)
        {
            var storageFormatTypes = (from service in services
                where typeof(IDatabaseStorageFormat).IsAssignableFrom(service.ImplementationType)
                select service.ImplementationType).ToList();
            var storageFormatService = serviceProvider.GetRequiredService<IStorageFormatService>();
            foreach (var storageFormatType in storageFormatTypes)
                storageFormatService.RegisterStorageFormat(storageFormatType);
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
                    typeof(BaseCommand), typeof(IProfile),

                    // CommandLineParser
                    typeof(VerbAttribute)
                }, conf => conf.PreferSharedTypes = true)).ToList();
        }

        private static IEnumerable<IPluginFactory> ConfigurePlugins(IServiceCollection services,
            IEnumerable<PluginLoader> loaders)
        {
            var list = new List<IPluginFactory>();

            // Create an instance of plugin types
            foreach (var loader in loaders)
            foreach (var pluginType in loader
                .LoadDefaultAssembly()
                .GetTypes()
                .Where(t => typeof(IPluginFactory).IsAssignableFrom(t) && !t.IsAbstract))
            {
                // This assumes the implementation of IPluginFactory has a parameterless constructor
                var plugin = (IPluginFactory) Activator.CreateInstance(pluginType);

                if (plugin == null)
                    throw new Exception("Activator.CreateInstance returned null while trying to load plugin");

                plugin.Configure(services);
                list.Add(plugin);
            }

            return list;
        }
    }
}
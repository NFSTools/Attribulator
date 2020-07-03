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

            try
            {
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
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while initializing the application.");
                return 1;
            }
        }

        private static async Task<int> RunApplication(IServiceProvider serviceProvider, IEnumerable<string> args)
        {
            var commandService = serviceProvider.GetRequiredService<ICommandService>();
            var commandTypes = commandService.GetCommandTypes().ToArray();
            try
            {
                return await Parser.Default.ParseArguments(args, commandTypes)
                    .MapResult((BaseCommand cmd) =>
                    {
                        cmd.SetServiceProvider(serviceProvider);
                        return cmd.Execute();
                    }, errs => Task.FromResult(1));
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while running the application.");
                return 1;
            }
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
            commandService.RegisterCommand<FormatListCommand>();
            commandService.RegisterCommand<PackCommand>();
            commandService.RegisterCommand<UnpackCommand>();

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
            var idToFactoryMap = new Dictionary<string, IPluginFactory>();

            foreach (var loader in loaders)
            foreach (var pluginType in from pluginType in loader.LoadDefaultAssembly().GetTypes()
                where typeof(IPluginFactory).IsAssignableFrom(pluginType) && !pluginType.IsAbstract
                select pluginType)
            {
                var pluginFactory = (IPluginFactory) Activator.CreateInstance(pluginType);

                if (pluginFactory == null)
                    throw new Exception("Activator.CreateInstance returned null while trying to load plugin");

                idToFactoryMap.Add(pluginFactory.GetId(), pluginFactory);
            }

            var unresolved = new List<PluginResolutionNode>();
            var resolved = new List<PluginResolutionNode>();
            var dependents =
                new Dictionary<string, List<string>>(); // key: plugin; value: list of plugins that require it

            foreach (var (id, factory) in idToFactoryMap)
            {
                var node = new PluginResolutionNode(id);

                foreach (var requiredPlugin in factory.GetRequiredPlugins())
                {
                    node.AddEdge(new PluginResolutionNode(requiredPlugin));

                    if (!dependents.ContainsKey(requiredPlugin))
                        dependents.Add(requiredPlugin, new List<string>());
                    dependents[requiredPlugin].Add(id);
                }

                ResolveDependencies(node, resolved, unresolved);
            }

            resolved = resolved.Distinct(PluginResolutionNode.IdComparer).ToList();
            unresolved = unresolved.Distinct(PluginResolutionNode.IdComparer).ToList();

            if (unresolved.Count != 0) throw new Exception("unresolved.Count != 0");

            var list = new List<IPluginFactory>();

            foreach (var node in resolved)
                if (idToFactoryMap.TryGetValue(node.Id, out var factory))
                {
                    factory.Configure(services);
                    list.Add(factory);
                }
                else
                {
                    throw new Exception(
                        $"Encountered unresolved dependency while loading plugins: {node.Id} (dependents: {string.Join(", ", dependents[node.Id])})");
                }

            return list;
        }

        private static void ResolveDependencies(PluginResolutionNode node, List<PluginResolutionNode> resolved,
            List<PluginResolutionNode> unresolved)
        {
            unresolved.Add(node);

            foreach (var edge in node.Edges)
                if (!resolved.Contains(edge))
                    ResolveDependencies(edge, resolved, unresolved);

            resolved.Add(node);
            unresolved.Remove(node);
        }

        private class PluginResolutionNode
        {
            public PluginResolutionNode(string id)
            {
                Id = id;
                Edges = new List<PluginResolutionNode>();
            }

            public string Id { get; }

            public List<PluginResolutionNode> Edges { get; }

            public static IEqualityComparer<PluginResolutionNode> IdComparer { get; } = new IdEqualityComparer();

            public void AddEdge(PluginResolutionNode node)
            {
                Edges.Add(node);
            }

            private sealed class IdEqualityComparer : IEqualityComparer<PluginResolutionNode>
            {
                public bool Equals(PluginResolutionNode x, PluginResolutionNode y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return x.Id == y.Id;
                }

                public int GetHashCode(PluginResolutionNode obj)
                {
                    return obj.Id != null ? obj.Id.GetHashCode() : 0;
                }
            }
        }
    }
}
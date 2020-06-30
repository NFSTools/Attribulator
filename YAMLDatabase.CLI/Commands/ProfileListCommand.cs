using System;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.CLI.Commands
{
    [Verb("profiles", HelpText = "List the loaded profiles.")]
    [UsedImplicitly]
    public class ProfileListCommand : BaseCommand
    {
        public override Task<int> Execute()
        {
            var profileService = ServiceProvider.GetRequiredService<IProfileService>();

            foreach (var profile in profileService.GetProfiles())
                Console.WriteLine("{0} - ID: {1}", profile.GetName(), profile.GetGameId());

            return Task.FromResult(0);
        }
    }
}
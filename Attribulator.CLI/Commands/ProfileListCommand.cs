using System;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Attribulator.CLI.Commands
{
    [Verb("profiles", HelpText = "List the loaded profiles.")]
    [UsedImplicitly]
    public class ProfileListCommand : BaseCommand
    {
        private ILogger<ProfileListCommand> _logger;

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = serviceProvider.GetRequiredService<ILogger<ProfileListCommand>>();
        }

        public override Task<int> Execute()
        {
            var profileService = ServiceProvider.GetRequiredService<IProfileService>();
            var profiles = profileService.GetProfiles().ToList();

            _logger.LogInformation("Profiles ({NumProfiles}):", profiles.Count);
            foreach (var profile in profiles)
                _logger.LogInformation("{Name} - ID: {Id}; DB Type: {DbType}", profile.GetName(),
                    profile.GetGameId(), profile.GetDatabaseType());

            return Task.FromResult(0);
        }
    }
}
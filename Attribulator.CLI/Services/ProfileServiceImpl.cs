using System;
using System.Collections.Generic;
using Attribulator.API;
using Attribulator.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.CLI.Services
{
    public class ProfileServiceImpl : IProfileService
    {
        private readonly List<IProfile> _profiles = new List<IProfile>();
        private readonly IServiceProvider _serviceProvider;

        public ProfileServiceImpl(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void RegisterProfile<TProfile>() where TProfile : IProfile
        {
            RegisterProfile(typeof(TProfile));
        }

        public void RegisterProfile(Type profileType)
        {
            _profiles.Add((IProfile) _serviceProvider.GetRequiredService(profileType));
        }

        public IEnumerable<IProfile> GetProfiles()
        {
            return _profiles;
        }

        public IProfile GetProfile(string profileId)
        {
            foreach (var profile in _profiles)
                if (profile.GetProfileId() == profileId)
                    return profile;

            throw new KeyNotFoundException($"Cannot find profile: {profileId}");
        }
    }
}
using System;
using System.Collections.Generic;
using YAMLDatabase.API;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.CLI.Services
{
    public class ProfileServiceImpl : IProfileService
    {
        private readonly List<IProfile> _profiles = new List<IProfile>();
        
        public void RegisterProfile<TProfile>() where TProfile : IProfile
        {
            RegisterProfile(typeof(TProfile));
        }

        public void RegisterProfile(Type profileType)
        {
            _profiles.Add((IProfile) Activator.CreateInstance(profileType));
        }

        public IEnumerable<IProfile> GetProfiles()
        {
            return _profiles;
        }

        public IProfile GetProfile(string gameId)
        {
            foreach (var profile in _profiles)
            {
                if (profile.GetGameId() == gameId)
                {
                    return profile;
                }
            }
            
            throw new KeyNotFoundException($"Cannot find profile for game: {gameId}");
        }
    }
}
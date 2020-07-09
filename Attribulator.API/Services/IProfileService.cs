using System;
using System.Collections.Generic;

namespace Attribulator.API.Services
{
    /// <summary>
    /// Exposes an interface for registering and retrieving profiles.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Registers a new profile type.
        /// </summary>
        /// <typeparam name="TProfile">The profile type.</typeparam>
        void RegisterProfile<TProfile>() where TProfile : IProfile;
        
        /// <summary>
        /// Registers a new profile type.
        /// </summary>
        void RegisterProfile(Type profileType);

        /// <summary>
        /// Gets the registered profiles.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> that produces the profiles.</returns>
        IEnumerable<IProfile> GetProfiles();

        /// <summary>
        /// Gets the profile mapped to the given game ID.
        /// </summary>
        /// <param name="gameId">The game ID.</param>
        /// <returns>The <see cref="IProfile"/> object.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when a profile cannot be found.</exception>
        IProfile GetProfile(string gameId);
    }
}
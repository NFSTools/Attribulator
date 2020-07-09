using System;
using System.Threading.Tasks;
using Attribulator.API.Exceptions;
using Attribulator.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.API.Plugin
{
    /// <summary>
    ///     Base class for commands
    /// </summary>
    public abstract class BaseCommand
    {
        /// <summary>
        ///     Gets or sets the <see cref="IServiceProvider" /> instance.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        ///     Sets the <see cref="IServiceProvider" /> instance.
        /// </summary>
        /// <param name="serviceProvider">The new <see cref="IServiceProvider" /> instance.</param>
        public virtual void SetServiceProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        ///     Executes the command.
        /// </summary>
        /// <returns>The return code of the command (0 for success)</returns>
        public abstract Task<int> Execute();

        protected IProfile FindProfile(string gameId)
        {
            if (ServiceProvider == null) throw new CommandException("ServiceProvider is not set!");

            return ServiceProvider.GetRequiredService<IProfileService>().GetProfile(gameId);
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Exceptions;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.API.Plugin
{
    /// <summary>
    /// Base class for commands
    /// </summary>
    public abstract class BaseCommand
    {
        /// <summary>
        /// Gets or sets the <see cref="IServiceProvider"/> instance.
        /// </summary>
        protected IServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        /// <summary>
        /// Sets the <see cref="IServiceProvider"/> instance.
        /// </summary>
        /// <param name="serviceProvider">The new <see cref="IServiceProvider"/> instance.</param>
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <returns>The return code of the command (0 for success)</returns>
        public abstract Task<int> Execute();

        protected IProfile FindProfile(string gameId)
        {
            if (this.ServiceProvider == null)
            {
                throw new CommandException("ServiceProvider is not set!");
            }
            
            return this.ServiceProvider.GetRequiredService<IProfileService>().GetProfile(gameId);
        }
    }
}
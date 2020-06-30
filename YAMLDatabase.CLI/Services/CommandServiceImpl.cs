using System;
using System.Collections.Generic;
using System.Reflection;
using CommandLine;
using YAMLDatabase.API.Exceptions;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.CLI.Services
{
    public class CommandServiceImpl : ICommandService
    {
        private readonly ISet<Type> _commandTypes = new HashSet<Type>();
        
        public void RegisterCommand<TCommand>() where TCommand : ICommand
        {
            RegisterCommand(typeof(TCommand));
        }

        public void RegisterCommand(Type type)
        {
            if (!typeof(ICommand).IsAssignableFrom(type))
            {
                throw new CommandServiceException($"Command type [{type}] does not inherit from ICommand.");
            }

            if (type.GetCustomAttribute(typeof(VerbAttribute)) == null)
            {
                throw new CommandServiceException($"Command type [{type}] is not annotated with [Verb].");
            }
            
            if (!_commandTypes.Add(type))
            {
                throw new CommandServiceException($"Command type [{type}] is already registered.");
            }
        }

        public IEnumerable<Type> GetCommandTypes()
        {
            return _commandTypes;
        }
    }
}
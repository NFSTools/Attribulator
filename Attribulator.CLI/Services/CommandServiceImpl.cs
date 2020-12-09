using System;
using System.Collections.Generic;
using System.Reflection;
using Attribulator.API.Exceptions;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using CommandLine;

namespace Attribulator.CLI.Services
{
    public class CommandServiceImpl : ICommandService
    {
        private readonly ISet<Type> _commandTypes = new HashSet<Type>();
        
        public void RegisterCommand<TCommand>() where TCommand : BaseCommand
        {
            RegisterCommand(typeof(TCommand));
        }

        public void RegisterCommand(Type type)
        {
            if (!typeof(BaseCommand).IsAssignableFrom(type))
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
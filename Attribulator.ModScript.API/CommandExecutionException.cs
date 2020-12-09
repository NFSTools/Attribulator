using System;
using System.Runtime.Serialization;

namespace Attribulator.ModScript.API
{
    [Serializable]
    public class CommandExecutionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public CommandExecutionException()
        {
        }

        public CommandExecutionException(string message) : base(message)
        {
        }

        public CommandExecutionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CommandExecutionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
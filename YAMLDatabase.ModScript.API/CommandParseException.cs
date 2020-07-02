using System;
using System.Runtime.Serialization;

namespace YAMLDatabase.ModScript.API
{
    [Serializable]
    public class CommandParseException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public CommandParseException()
        {
        }

        public CommandParseException(string message) : base(message)
        {
        }

        public CommandParseException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CommandParseException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
using System;
using System.Runtime.Serialization;

namespace YAMLDatabase.Plugins.ModScript
{
    [Serializable]
    public class ModScriptCommandExecutionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ModScriptCommandExecutionException()
        {
        }

        public ModScriptCommandExecutionException(string message) : base(message)
        {
        }

        public ModScriptCommandExecutionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ModScriptCommandExecutionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
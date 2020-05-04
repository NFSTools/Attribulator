using System;
using System.Runtime.Serialization;

namespace YAMLDatabase
{
    [Serializable]
    public class SerializedDatabaseLoaderException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SerializedDatabaseLoaderException()
        {
        }

        public SerializedDatabaseLoaderException(string message) : base(message)
        {
        }

        public SerializedDatabaseLoaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SerializedDatabaseLoaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
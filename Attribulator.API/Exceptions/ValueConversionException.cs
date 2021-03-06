﻿using System;
using System.Runtime.Serialization;

namespace Attribulator.API.Exceptions
{
    [Serializable]
    public class ValueConversionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ValueConversionException()
        {
        }

        public ValueConversionException(string message) : base(message)
        {
        }

        public ValueConversionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ValueConversionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
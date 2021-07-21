using System;
using System.Runtime.Serialization;

namespace SerialPortUtility
{
    public class UnportException : Exception
    {
        public UnportException() { }
        public UnportException(string message) : base(message) { }
        public UnportException(string message, Exception innerException) : base(message,innerException) { }
        protected UnportException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

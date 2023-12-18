using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProiectPSSC.Domain.Models
{
    [Serializable]
    internal class InvalidCardNumberException : Exception
    {
        public InvalidCardNumberException()
        {
        }

        public InvalidCardNumberException(string? message) : base(message)
        {
        }

        public InvalidCardNumberException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected InvalidCardNumberException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

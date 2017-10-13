using System;

namespace POGOLib.Official.Exceptions
{
    public class SessionStateException : Exception
    {
        public SessionStateException(string message) : base(message)
        {
        }
    }
}

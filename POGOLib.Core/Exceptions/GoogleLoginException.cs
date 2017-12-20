using System;

namespace POGOLib.Official.Exceptions
{
    public class GoogleLoginException : Exception
    {
        public GoogleLoginException(string message) : base(message)
        {
        }
    }
}

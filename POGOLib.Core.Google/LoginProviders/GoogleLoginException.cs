using System;

namespace POGOLib.Official.LoginProviders
{
    public class GoogleLoginException : Exception
    {
        public GoogleLoginException(string message) : base(message)
        {
        }
    }
}

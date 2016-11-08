using System;

namespace POGOLib.Net.Authentication.Exceptions
{
    public class WrongCredentialsException : Exception
    {

        public WrongCredentialsException(string message) : base(message)
        {
            
        }

    }
}

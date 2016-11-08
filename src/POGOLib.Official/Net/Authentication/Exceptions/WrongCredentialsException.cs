using System;

namespace POGOLib.Official.Net.Authentication.Exceptions
{
    public class WrongCredentialsException : Exception
    {

        public WrongCredentialsException(string message) : base(message)
        {
            
        }

    }
}

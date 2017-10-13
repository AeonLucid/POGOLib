using System;

namespace POGOLib.Official.LoginProviders
{
    public class PtcLoginException : Exception
    {
        public PtcLoginException(string message) : base(message)
        {
        }
    }
}

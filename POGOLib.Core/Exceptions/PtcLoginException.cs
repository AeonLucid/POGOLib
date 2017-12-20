using System;

namespace POGOLib.Official.Exceptions
{
    public class PtcLoginException : Exception
    {
        public PtcLoginException(string message) : base(message)
        {
        }
    }
}

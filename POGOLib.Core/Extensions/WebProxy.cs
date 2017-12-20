using System;
using System.Net;

namespace POGOLib.Official.Extensions
{
    internal class WebProxy : IWebProxy
    {
        private string _proxyAddress;
        private bool v;

        public WebProxy(string proxyAddress, bool v)
        {
            _proxyAddress = proxyAddress;
            this.v = v;
        }

        public ICredentials Credentials { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Uri GetProxy(Uri destination)
        {
            throw new NotImplementedException();
        }

        public bool IsBypassed(Uri host)
        {
            throw new NotImplementedException();
        }
    }
}
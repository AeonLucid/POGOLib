using System;
using System.Net;

namespace POGOLib.Official.Extensions
{
    internal class WebProxy : IWebProxy
    {
        public Uri Address;
        public bool BypassProxyOnLocal;

        public WebProxy()
        {
        }

        public WebProxy(string proxyAddress, bool v)
        {
            Address =  new Uri(proxyAddress);
            BypassProxyOnLocal= v;
        }

       public ICredentials Credentials { get; set; }

        public Uri GetProxy(Uri destination)
        {
             return Address;
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }
    }
}
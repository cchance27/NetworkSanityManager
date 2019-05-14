using System.Net;

namespace NetworkSanityManager.MicrosoftDNS
{
    public struct DNSInputObject
    {
        public string hostname;
        public IPAddress IpAddress;
        public bool ptrCheck;
        public bool dnsCheck;
    }
}

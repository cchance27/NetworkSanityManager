using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetworkSanityManager
{
    class DnsUtility
    {
        public static bool CheckDns(string host, IPAddress ip)
        {
            try
            {
                var dnsEntry = Dns.GetHostByName(host);
                if (ip.Equals(dnsEntry.AddressList[0]))
                {
                    //Console.WriteLine($"IP Matches DNS {host} ({dnsEntry.AddressList[0].ToString()} == {ip})");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{host}: IP Does Not Match DNS ({dnsEntry.AddressList[0].ToString()} != {ip})");
                    return false;
                }
            }
            catch
            {
                // DNS Failed 
                Console.WriteLine($"DNS Not Found for {host}");
                return false;
            }
        }

        public static bool CheckPtr(IPAddress address, string host)
        {
            try
            {
                var dnsEntry = Dns.GetHostByAddress(address);
                if (dnsEntry.HostName == host)
                {
                    //Console.WriteLine($"HostName Matches IP {address.ToString()} ({dnsEntry.HostName} == {host})");
                    return true;
                }
                else
                {
                    Console.WriteLine($"{address.ToString()}: HostName Does Not Match IP  ({dnsEntry.HostName} != {host})");
                    return false;
                }
            }
            catch
            {
                // DNS Failed
                Console.WriteLine($"PTR Not Found for {address.ToString()}");
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSanityManager
{
    public static class IPTools
    {
        public static IPAddressValue[] BuildActiveIPList(List<IPAddressValue> IPList, int threads, ProgressBar progress = null)
        {
            var ResultBag = new System.Collections.Concurrent.ConcurrentBag<IPAddressValue>();
            Parallel.ForEach(
               IPList, new ParallelOptions { MaxDegreeOfParallelism = threads },
                   ip => {
                       ResultBag.Add(CheckIP(ip));
                       if (progress != null) progress.Report((double)ResultBag.Count / IPList.Count);
                   }
            );

            return ResultBag.Where(ip => ip != null).ToArray<IPAddressValue>();
        }
               
        public static List<IPAddressValue> SubnetsToIPAddresses(SubnetValue[] subnets)
        {
            List<IPAddressValue> IPList = new List<IPAddressValue>();
            foreach (SubnetValue subnet in subnets)
            {
                var network = IPNetwork.Parse(subnet.Subnet);
                network.ListIPAddress().ToList<IPAddress>().ForEach(ip => {
                    IPList.Add(new IPAddressValue()
                    {
                        IPAddress = ip,
                        Community = subnet.Community
                    });
                });
            }

            return IPList;
        }

        private static IPAddressValue CheckIP(IPAddressValue ipv)
        {
            var totalRetries = 3;
            var retry = 0;
            do
            {
                Ping ping = new Ping();
                var result = ping.Send(ipv.IPAddress, 500);
                if (result.Status == IPStatus.Success)
                {
                    //Console.WriteLine($"Pinging {ipv.IPAddress}... Success");
                    retry = 9999;
                    return ipv;
                }
                else
                {
                    //Console.WriteLine($"Pinging {ipv.IPAddress}... Failed (Retry# {retry})");
                    System.Threading.Thread.Sleep(1000);
                    retry++;
                }
            } while (retry < totalRetries);
            return null;
        }

    }
}

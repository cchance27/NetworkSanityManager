using System;
using System.Collections.Generic;
using System.Linq;

namespace NetworkSanityManager
{
    class Program
    {
        public static Configuration _config;
        //TODO: Add override to config to "wait for keypress on each plugin step, for debugging, or maybe when debug = true"
        //TODO: Autoloading plugins, plugins do the cleanup of DeviceResults?
        //TODO: Plugins need priority (so for instance we can have DNS always run first)
        //TODO: Plugins can be alerted if theirs a "plugin block" from a previous plugin, and if they care they can abort running.
        //TODO: Implement logging properly

        static void Main(string[] args)
        {
            _config = new Configuration();
            Console.WriteLine($"Threads for Processing: {_config.Settings.Threads}");
            Console.WriteLine($"Domain Name: {_config.Settings.DomainName}");

            // Convert Subnets to List of IPs
            var IPList = IPTools.SubnetsToIPAddresses(_config.Settings.Subnets);
            Console.WriteLine($"Total IPs for Processing: {IPList.Count}");
            Console.WriteLine();

            // Build a list based on subnet and return a list of IPs responding to pings.
            IPAddressValue[] ActiveIPs;
            using (var progress = new ProgressBar())
            {
                Console.WriteLine("Testing for Active IPs... ");
                 ActiveIPs = IPTools.BuildActiveIPList(IPList, _config.Settings.Threads, progress);
                
            };
            Console.WriteLine($"Total Active IPs Found: {ActiveIPs.Count()}");
            Console.WriteLine();

            // Run our SNMP Fetches against all known active IPs
            Device[] DeviceResults;
            using (var progress = new ProgressBar())
            {
                Console.WriteLine("Checking SNMP for Active IPs... ");
                var snmp = new SnmpTools(_config);
                DeviceResults = snmp.GetSnmpDevicesParallel(ActiveIPs, progress);
            }
            Console.WriteLine($"Total valid SNMP Responses: {DeviceResults.Where(r => String.IsNullOrWhiteSpace(r.Errors)).ToArray<Device>().Count()}");
            Console.WriteLine();

            // Save our results to static files (CSV)
            Console.WriteLine("Saving Results of Scan");
            ResultStorage.SaveResults(DeviceResults);
            Console.WriteLine();

            Console.WriteLine("Running Plugins");
            var pluginBlocked = false;
            if (_config.Settings.Plugins.Contains("microsoftdns"))
            {
                Console.WriteLine("MicrosoftDNS:");
                var msdns = new MicrosoftDNS.Plugin(
                    DeviceResults
                    .Where((dev) => dev.SysNameValid && (dev.PtrCheck == false || dev.DnsCheck == false))
                    .Select<Device, MicrosoftDNS.DNSInputObject>((dev) =>
                    new MicrosoftDNS.DNSInputObject()
                    {
                        hostname = dev.Name,
                        IpAddress = dev.IpAddress,
                        dnsCheck = dev.DnsCheck,
                        ptrCheck = dev.PtrCheck
                    }).ToArray());

                Console.WriteLine("MicrosoftDNS Preview: ");
                var preview = msdns.Preview();
                Console.WriteLine(preview);
                Console.WriteLine("MicrosoftDNS Commit: ");
                Console.WriteLine("\n" + msdns.Commit());

                if (String.IsNullOrWhiteSpace(preview) == false)
                {
                    Console.WriteLine("Blocking all Plugins future Plugins due to DNS Update Pending.");
                    pluginBlocked = true;
                    Console.WriteLine("\nPress Key to Continue...");
                    Console.ReadKey();
                }
            }

            if (_config.Settings.Plugins.Contains("librenms") && !pluginBlocked)
            {
                Console.WriteLine("\nLibreNMS:");
                var libre = new LibreNMS.Plugin(
                    DeviceResults
                    .Where((dev) => dev.SysNameValid)
                    .Select<Device, LibreNMS.LibreNMSInputModel>((dev) =>
                     new LibreNMS.LibreNMSInputModel
                     {
                         Hostname = $"{dev.Name}.{_config.Settings.DomainName}",
                         Community = dev.Community,
                         SnmpVersion = dev.SnmpVersion
                     }
                    ));

                Console.WriteLine("LibreNMS Preview:");
                Console.WriteLine(libre.Preview());
                Console.WriteLine("\nPress Key to Continue...");
                Console.ReadKey();
                Console.WriteLine("LibreNMS Commit: ");
                Console.WriteLine("\n" + libre.Commit());
                Console.WriteLine("\nPress Key to Continue...");
                Console.ReadKey();

            }

            if (_config.Settings.Plugins.Contains("oxidized") && !pluginBlocked)
            {
                var oxi = new Oxidized.Plugin(
                    DeviceResults
                    .Where((dev) => dev.SysNameValid)
                    .Select<Device, Oxidized.OxidizedInputModel>((dev) =>
                        new Oxidized.OxidizedInputModel
                        {
                            Name = dev.Name,
                            Address = dev.IpAddress.ToString(),
                            Vendor = dev.Vendor,
                            Model = dev.Model
                        }));

                Console.WriteLine("\nOxidized Preview: ");
                Console.WriteLine(oxi.Preview());
                Console.WriteLine("\nPress Key to Continue...");
                Console.ReadKey();
                Console.WriteLine("Oxidized Commit: ");
                Console.WriteLine("\n" + oxi.Commit());
                Console.WriteLine("\nPress Key to Continue...");
                Console.ReadKey();
            }

            if (_config.Settings.Plugins.Contains("prometheus") && !pluginBlocked)
            {
                var prom = new Prometheus.Plugin(
                    DeviceResults
                    .Where((dev) => dev.SysNameValid)
                    .Select<Device, KeyValuePair<string, string>>((dev) =>
                        new KeyValuePair<string, string>(dev.IpAddress.ToString(), dev.Vendor)));

                Console.WriteLine("Prometheus Preview: ");
                Console.WriteLine(prom.Preview());
                Console.WriteLine("\nPress Key to Continue...");
                Console.ReadKey();
                Console.WriteLine("Prometheus Commit: ");
                Console.WriteLine("\n" + prom.Commit());
                Console.WriteLine("\nPress Key to Continue...");
                Console.ReadKey();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace NetworkSanityManager
{
    internal class Program
    {
        public static Configuration _config;

        private static void Main(string[] args)
        {
            // Load general program configuration
            _config = new Configuration();

            // Convert configuration Subnets to List of IPs
            var IPList = IPTools.SubnetsToIPAddresses(_config.Settings.Subnets);

            // Some CLI Header Information
            Console.WriteLine($"Threads for Processing: {_config.Settings.Threads}");
            Console.WriteLine($"Domain Name: {_config.Settings.DomainName}");
            Console.WriteLine($"Total IPs for Processing: {IPList.Count}");
            Console.WriteLine();

            // Build a list based on subnet and return a list of IPs responding to pings.
            IPAddressValue[] ActiveIPs = findActiveIPsWithProgress(IPList);

            // Run our SNMP Fetches against all known active IPs
            Device[] DeviceResults = fetchSNMPWithProgress(ActiveIPs);

            // Save our results to static files (CSV)
            // TODO: Output to CSV should be shifted to a plugin it's just another "output" like the rest, just happens to be local.
            Console.WriteLine("Saving Results of Scan");
            ResultStorage.SaveResults(DeviceResults);
            Console.WriteLine();

            runPlugins(DeviceResults);
        }

        /// <summary>
        /// Run our plugins for output methods
        /// </summary>
        /// <param name="DeviceResults"></param>
        private static void runPlugins(Device[] DeviceResults)
        {
            //TODO: Add override to config to "wait for keypress on each plugin step, for debugging, or maybe when debug = true"
            //TODO: Autoloading plugins, plugins do the cleanup of DeviceResults?
            //TODO: Plugins need priority (so for instance we can have DNS always run first)
            //TODO: Plugins can be alerted if theirs a "plugin block" from a previous plugin, and if they care they can abort running.
            //TODO: Implement logging properly

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


        /// <summary>
        /// Takes a list of IPAddressValues and checks them via ping if they are active with retries.
        /// with Progress indicators for CLI mode
        /// </summary>
        /// <param name="IPList"></param>
        /// <returns></returns>
        private static IPAddressValue[] findActiveIPsWithProgress(List<IPAddressValue> IPList)
        {
            IPAddressValue[] ActiveIPs;
            using (var progress = new ProgressBar())
            {
                Console.WriteLine("Testing for Active IPs... ");
                ActiveIPs = IPTools.BuildActiveIPList(IPList, _config.Settings.Threads, progress);
            };
            Console.WriteLine($"Total Active IPs Found: {ActiveIPs.Count()}");
            Console.WriteLine();
            return ActiveIPs;
        }

        /// <summary>
        /// Fetch all active ip's snmp and return a an array of Device[] 
        /// with progress indicators for CLI mode
        /// </summary>
        /// <param name="ActiveIPs"></param>
        /// <returns></returns>
        private static Device[] fetchSNMPWithProgress(IPAddressValue[] ActiveIPs)
        {
            Device[] DeviceResults;
            using (var progress = new ProgressBar())
            {
                Console.WriteLine("Checking SNMP for Active IPs... ");
                var snmp = new SnmpTools(_config);
                DeviceResults = snmp.GetSnmpDevicesParallel(ActiveIPs, progress);
            }
            Console.WriteLine($"Total valid SNMP Responses: {DeviceResults.Where(r => String.IsNullOrWhiteSpace(r.Errors)).ToArray<Device>().Count()}");
            Console.WriteLine();
            return DeviceResults;
        }
    }
}
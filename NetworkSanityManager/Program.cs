﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NetworkSanityManager
{
    class Program
    {
        public static Configuration _config;

        static void Main(string[] args)
        {
            _config = new Configuration();
            Console.WriteLine($"Threads for Processing: {_config.Settings.Threads}");
            Console.WriteLine($"Domain Name: {_config.Settings.DomainName}");
            Console.WriteLine();

            IPAddressValue[] ActiveIPs = IPTools.BuildActiveIPList();

            Console.WriteLine();
            Console.WriteLine($"Total Active IPs Found: {ActiveIPs.Count()}");
            Console.WriteLine();

            var snmp = new SnmpTools(_config);
            Device[] DeviceResults = snmp.GetSnmpDevicesParallel(ActiveIPs);

            ResultStorage.SaveResults(DeviceResults);

            var pluginBlocked = false;
            if (_config.Settings.Plugins.Contains("microsoftdns"))
            {
                // Pass only devices that have a correct sysname and have either a dns or ptr incorrect.
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
                }
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

                Console.WriteLine("Oxidized Preview: ");
                Console.WriteLine(oxi.Preview());
                Console.WriteLine("Oxidized Commit: ");
                Console.WriteLine("\n" + oxi.Commit());
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
                Console.WriteLine("Prometheus Commit: ");
                Console.WriteLine("\n" + prom.Commit());
            }

            Console.ReadKey();
        }
    }
}

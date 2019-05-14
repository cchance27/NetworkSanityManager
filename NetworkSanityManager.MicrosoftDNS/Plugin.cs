using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Security;
using System.Text;

namespace NetworkSanityManager.MicrosoftDNS
{
    public class Plugin
    {
        // This plugin will handle updating the PTR and DNS entries in a microsoft dns server
        private Configuration _config { get; }
        private IEnumerable<DNSInputObject> _dnsInputObjects { get; }

        // Sadly because of WMI for MS DNS we have to include System.Management that is windows only.
        DnsProvider _provider;

        public Plugin(IEnumerable<DNSInputObject> dnsInputObjects)
        {
            _config = new Configuration();
            _dnsInputObjects = dnsInputObjects;

            Console.WriteLine();
            Console.WriteLine("MicrosoftDNS Plugin Started...");
            Console.WriteLine();

            _provider = new DnsProvider(_config.Settings.Server, _config.Settings.Username, _config.Settings.Password);

        }

        public string Preview()
        {
            if (_config.Settings.Enabled)
            {
                
                var dnsToCreate = _dnsInputObjects.Where<DNSInputObject>((dev) => dev.dnsCheck == false).ToArray();

                // Only create PTR's for units that have a good DNS but missing PTR, as DNS will create the PTR automatically
                var ptrToCreate = _dnsInputObjects.Where<DNSInputObject>((dev) => dev.ptrCheck == false && dev.dnsCheck == true).ToArray();

                var output = new StringBuilder();
                Array.ForEach(dnsToCreate, dev =>
                {
                    // Check for a domainPrefix, to use as part of the name
                    string fullDeviceName = String.IsNullOrWhiteSpace(_config.Settings.DomainPrefix) ? dev.hostname.ToUpper() : $"{ dev.hostname.ToUpper()}.{ _config.Settings.DomainPrefix}";
                    output.AppendLine($"Remove-DnsServerResourceRecord -ZoneName {_config.Settings.Domain} -RRType A -Name {fullDeviceName} -Force");
                    output.AppendLine($"Add-DnsServerResourceRecordA -ZoneName {_config.Settings.Domain} -Name {fullDeviceName} -TimeToLive 3600 -IPv4Address {dev.IpAddress} -CreatePtr");
                });
                //$"Create A {dev.hostname}.{_config.Settings.Domain} pointing to {dev.IpAddress}"));

                Array.ForEach(ptrToCreate, dev => {
                    string fullDeviceName = String.IsNullOrWhiteSpace(_config.Settings.DomainPrefix) ? dev.hostname.ToUpper() : $"{ dev.hostname.ToUpper()}.{ _config.Settings.DomainPrefix}";
                    string[] ipOctets = dev.IpAddress.ToString().Split(".");
                    string arpaDomain = $"{ipOctets[2]}.{ipOctets[1]}.{ipOctets[0]}.in-addr.arpa";
                    output.AppendLine($"Remove-DnsServerResourceRecord -ZoneName {arpaDomain} -RRType PTR -Name {ipOctets[3]} -Force");
                    output.AppendLine($"Add-DnsServerResourceRecordPtr -ZoneName {arpaDomain} -Name {ipOctets[3]} -TimeToLive 3600 -PtrDomainName {fullDeviceName}.{_config.Settings.Domain}");
                });
                //$"Create PTR {dev.IpAddress} pointing to {dev.hostname}.{_config.Settings.Domain}"));

                return output.ToString();
            }
            else
            {
                return "Plugin is Disabled";
            }
        }

        public string Commit()
        {
            //if (_config.Settings.Enabled)
            //{
            //    var ptrToCreate = _dnsInputObjects.Where<DNSInputObject>((dev) => dev.ptrCheck == false).ToArray();
            //    var dnsToCreate = _dnsInputObjects.Where<DNSInputObject>((dev) => dev.dnsCheck == false).ToArray();

            //    //Array.ForEach(dnsToCreate, dev => _provider.AddARecord(_config.Settings.Domain, dev.hostname, dev.IpAddress.ToString()));



            //    return $"MicrosoftDNS: Commit Complete";
            //}
            //else
            //{
            //    return "Plugin is Disabled";
            //}
            return "No Automated Commit, Run the Preview Code on your Microsoft DNS to update values.";
        }

    }
}

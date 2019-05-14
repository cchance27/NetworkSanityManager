using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace NetworkSanityManager
{
    public class Configuration
    {
        private IConfigurationRoot config;

        private DeviceType[] _deviceTypes ;
        public DeviceType[] DeviceTypes { get => _deviceTypes; }

        private Settings _settings;
        public Settings Settings { get => _settings; }

        public Configuration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true);

            config = builder.Build();

            _deviceTypes = config.GetSection("Devices").Get<DeviceType[]>();
            _settings = config.GetSection("Settings").Get<Settings>();
        }
    }

    public class DeviceType
    {
        public string Vendor { get; set; }
        public string Type { get; set; }
        public string ObjectOID { get; set; }
        public ModelCheck[] ModelChecks { get; set; }
    }

    public class ModelCheck
    {
        // After an OID match, we will then match based on Check Regex to see if we're using a sysVersion that matches.
        public string Check { get; set; } = "";

        // Set the device to a static model, based on check+deviceoid
        public string ModelFixed { get; set; } = "";
        // Parsing sysVersion for only Model value
        public string ModelRegex { get; set; } = "";
        // Direct SNMP Oid Query for Model String
        public string ModelSnmp { get; set; } = "";
        // Set the device to a static version (not useful)
        public string VersionFixed { get; set; } = "";
        // Parsing sysVersion for only Version value
        public string VersionRegex { get; set; } = "";
        // Direct SNMP Oid Query for Version string.
        public string VersionSnmp { get; set; } = "";

        // Generic Regex used to parse for version and model from sysVersion, will not be used if any more narrow check is specified.
        public string Regex { get; set; } = "";
    }

    public class Settings
    {
        // The subnets we will be scanning for the general program
        public SubnetValue[] Subnets { get; set; }

        // How many processing threads
        public Int32 Threads { get; set; } = 3;

        // Snmp Retries for failures?
        public Int32 SnmpRetries { get; set; } = 1;

        // Should we fallback to try SnmpV1 if V2 fails.
        public bool SnmpV1Fallback { get; set; } = false;

        // Used for validation that a SysName for a device is valid
        public string SysNameValidator { get; set; } = ".*";

        // For PTR validation
        public string DomainName { get; set; } = "localhost.localdomain";

        public string[] Plugins { get; set; }
    }

    public class SubnetValue
    {
        public string Subnet { get; set; }
        public string Community { get; set; } = "public";
    }

    public class IPAddressValue
    {
        public IPAddress IPAddress { get; set; }
        public string Community { get; set; } = "public";
    }
}
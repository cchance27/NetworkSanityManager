using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Csv;

namespace NetworkSanityManager
{
    public class Device
    {
        [CsvExported(0, "DeviceName")]
        public string Name { get; }
        [CsvExported(1, "Vendor")]
        public string Vendor { get; set; }
        [CsvExported(2, "Type")]
        public string Type { get; set; }
        [CsvExported(3, "IP")]
        public IPAddress IpAddress { get; set; }
        [CsvExported(4, "Location")]
        public string Location { get; }
        [CsvExported(5, "Model")]
        public string Model { get; set; }
        [CsvExported(6, "Version")]
        public string Version { get; set; }
        [CsvExported(7, "Contact")]
        public string Contact { get; }
        [CsvExported(8, "SysNameValid")]
        public bool SysNameValid { get; } = false;
        [CsvExported(9, "DnsCheck")]
        public bool DnsCheck { get; }
        [CsvExported(10, "PtrCheck")]
        public bool PtrCheck { get; }

        //TODO: find way to disable these for export if debug is disabled
        [CsvExported(11, "SysVersion")]
        public string SysVersion { get; set; }
        [CsvExported(12, "ObjectOID")]
        public string ObjectOID { get; set; }

        //TODO: find way to only show this if theirs something in it (errors present)
        [CsvExported(13, "Errors")]
        public string Errors { get; set; }

        private DeviceType _deviceType { get; set; }
        public string Community { get; }

        public Device() { }

        public Device(string ipAddress, string sysVersion, string objectOID, string contact, string name, string location, string community)
        {
            var _config = Program._config;

            IpAddress = IPAddress.Parse(ipAddress);
            SysVersion = sysVersion;
            ObjectOID = objectOID;
            Contact = contact;
            Name = name.ToLower();
            Location = location;
            Community = community;

            // Find and store what kind of device this is so we can use it later.
            _deviceType = _config.DeviceTypes.SingleOrDefault<DeviceType>(dt => ObjectOID.Contains(dt.ObjectOID));
            Type = _deviceType?.Type;
            Vendor = _deviceType?.Vendor;

            findSysVersionModel(sysVersion, community, _config);

            SysNameValid = isSysNameValid(Name, _config.Settings.SysNameValidator);

            if (SysNameValid)
                (DnsCheck, PtrCheck) = dnsValidation(IpAddress, Name, _config.Settings.DomainName);
        }

        /// <summary>
        /// Finds which model check to use, then uses the appropriate SysVersion check to get the correct model and version
        /// </summary>
        /// <param name="sysVersion"></param>
        /// <param name="community"></param>
        /// <param name="_config"></param>
        private void findSysVersionModel(string sysVersion, string community, Configuration _config)
        {
            if (_deviceType != null)
            {
                ModelCheck mc = getModelCheck(_deviceType, sysVersion);
                if (mc != null)
                {
                    if (string.IsNullOrWhiteSpace(mc.Regex))
                        (Model, Version) = altSysVersionChecks(mc, new IPAddressValue { IPAddress = IpAddress, Community = community }, _config);
                    else
                        (Model, Version) = basicSysVersionCheck(mc, sysVersion);
                }
                else
                {
                    Model = "Unknown";
                    Errors = "Device passed a known type, but failed to match the provided regex.";
                    Version = SysVersion;
                }
            }
            else
            {
                Model = "Unknown";
                Errors = "Unknown Device Type, Check ObjectOID vs Config.json.";
                Version = SysVersion;
            }
        }

        /// <summary>
        /// Use regex checks from ModelCheck array to figure out which modelCheck this device needs to use.
        /// </summary>
        /// <param name="devType"></param>
        /// <param name="sysVersion"></param>
        /// <returns></returns>
        private ModelCheck getModelCheck(DeviceType devType, string sysVersion)
        {
            foreach (ModelCheck modelCheck in devType.ModelChecks)
            {
                Match m = Regex.Match(sysVersion, modelCheck.Check, RegexOptions.IgnoreCase);
                // This is the correct model for this device type
                if (m.Success)
                {
                    return modelCheck;
                }
            }
            return null;
        }

        /// <summary>
        /// Quick check to see if sysName matches our validator, if not we shouldn't be doing DNS for instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="validator"></param>
        /// <returns></returns>
        private bool isSysNameValid(string name, string validator)
        {
            if (String.IsNullOrWhiteSpace(validator) == false)
                return Regex.Match(name, validator, RegexOptions.IgnoreCase).Success;

            // No validator supplied so assume it's good?
            return true;
        }

        /// <summary>
        /// Check if the DNS and PTR records are correct for a device ip/sysname
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="name"></param>
        /// <param name="domainName"></param>
        /// <returns></returns>
        private (bool dnscheck, bool ptrcheck) dnsValidation(IPAddress ipAddress, string name, string domainName)
        {
            bool dnsCheck = DnsUtility.CheckDns($"{name}.{domainName}", ipAddress);
            bool ptrCheck = DnsUtility.CheckPtr(ipAddress, $"{name}.{domainName}");

            return (dnsCheck, ptrCheck);
        }

        /// <summary>
        /// If we don't have a regex that combines Model and Version, we use this routine to check various other models
        /// Including Fixed, Regex and SNMP methods for fetching valid Model/Version
        /// </summary>
        /// <param name="modelCheck"></param>
        /// <param name="address"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private (string model, string version) altSysVersionChecks(ModelCheck modelCheck, IPAddressValue address, Configuration config)
        {
            var model = "";
            var version = "";

            // Use a fixed model (worst case)
            if (modelCheck.ModelFixed != "")
                model = modelCheck.ModelFixed;

            // Use a fixed Version (why would we want to, but oh well)
            if (modelCheck.VersionFixed != "")
                model = modelCheck.VersionFixed;

            // Use a custom regex of sysVersion to grab Version
            if (modelCheck.ModelRegex != "")
            {
                Regex r = new Regex(modelCheck.ModelRegex, RegexOptions.IgnoreCase);
                var mR = r.Matches(SysVersion);
                if (mR.Count > 0)
                    model = mR[0].Groups["Model"].Value;
            }

            // Use a custom regex of sysVersion to grab Version
            if (modelCheck.VersionRegex != "")
            {
                Regex r = new Regex(modelCheck.VersionRegex, RegexOptions.IgnoreCase);
                var mR = r.Matches(SysVersion);
                if (mR.Count > 0)
                    version = mR[0].Groups["Version"].Value;
            }

            // Fetch Model/Version from SNMP directly if it's set
            if (modelCheck.ModelSnmp != "" || modelCheck.VersionSnmp != "")
            {
                var snmp = new SnmpTools(config);

                // Grab Model from SNMP if it's supplied
                if (modelCheck.ModelSnmp != "")
                    model = snmp.GetSnmpStringWithFallback(address, modelCheck.ModelSnmp);

                // Grab Version from SNMP if it's supplied
                if (modelCheck.VersionSnmp != "")
                    version = snmp.GetSnmpStringWithFallback(address, modelCheck.VersionSnmp);
            }

            return (model, version);
        }

        /// <summary>
        /// Most basic model/version check using Regex on the SysVersion
        /// </summary>
        /// <param name="modelCheck"></param>
        /// <param name="sysVersion"></param>
        /// <returns></returns>
        private (string model, string version) basicSysVersionCheck(ModelCheck modelCheck, string sysVersion)
        {
            var model = "";
            var version = "";

            Regex modelRegex = new Regex(modelCheck.Regex, RegexOptions.IgnoreCase);
            var matches = modelRegex.Matches(sysVersion);

            if (matches.Count > 0)
            {
                if (matches?[0].Groups["Model"]?.Value != null) { model = matches[0].Groups["Model"].Value; };
                if (matches?[0].Groups["Version"]?.Value != null) { version = matches[0].Groups["Version"].Value; };
            }
            else
            {
                model = "ModelCheck Failed";
                version = sysVersion;
            }

            return (model, version);
        }
    }
}

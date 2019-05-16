using SnmpSharpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace NetworkSanityManager
{
    public class SnmpTools
    {
        private Configuration config;

        public SnmpTools(Configuration config)
        {
            if (this.config == null)
                this.config = config;
        }

        /// <summary>
        /// Take a List of IP's and Parallel poll all of them to get their Device Details.
        /// </summary>
        /// <param name="ActiveIPs"></param>
        public Device[] GetSnmpDevicesParallel(IPAddressValue[] ActiveIPs, ProgressBar progress = null)
        {
            ConcurrentBag<Device> ResultBag = new ConcurrentBag<Device>();

            Parallel.ForEach(
               ActiveIPs, new ParallelOptions { MaxDegreeOfParallelism = config.Settings.Threads },
               ip => {
                   ResultBag.Add(pollDeviceFromSnmpWithFallback(ip));
                   if (progress != null) progress.Report((double)ResultBag.Count / ActiveIPs.Length);
               }
            );

            return ResultBag.ToArray<Device>();
        }
        
        /// <summary>
        /// Get a single string from an ipaddress/oid via SNMP
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        public string GetSnmpStringWithFallback(IPAddressValue address, string oid)
        {
            AgentParameters snmpParam = createSnmpParameters(address.Community, 2);
            string result = pollStringFromSnmp(address.IPAddress, snmpParam, oid);

            if (result == null & config.Settings.SnmpV1Fallback == true)
            {
                snmpParam = createSnmpParameters(address.Community, 1);
                result = pollStringFromSnmp(address.IPAddress, snmpParam, oid);
            }

            return result;
        }
                
        /// <summary>
        /// Poll SNMP for a specific oid and return a string
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="snmpParameters"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        private string pollStringFromSnmp(IPAddress ipAddress, IAgentParameters snmpParameters, string oid)
        {
            string[] oids = new string[] { oid };
            try
            {
                VbCollection collection = GetSnmp(ipAddress, snmpParameters, oids);
                return collection[0].Value.ToString().Trim();
            }
            catch (Exception e)
            {

                //Console.WriteLine($"Error Fetching SNMP from: {ipAddress} ({e.Message})");
                return null;
            }
        }

        /// <summary>
        /// Wrapper for grabbing a device with fallback support to retry with v1
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        private Device pollDeviceFromSnmpWithFallback(IPAddressValue address)
        {
            AgentParameters snmpParam = createSnmpParameters(address.Community, 2);
            Device device = pollDeviceFromSnmp(address.IPAddress, snmpParam);

            if (device.Errors == "Request has reached maximum retries." && config.Settings.SnmpV1Fallback == true)
            {
                snmpParam = createSnmpParameters(address.Community, 1);
                device = pollDeviceFromSnmp(address.IPAddress, snmpParam);
            }   

            return device;
        }

        /// <summary>
        /// Poll SNMP for a device and return a Device or Null
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="snmpAgentParameters"></param>
        private Device pollDeviceFromSnmp(IPAddress ipAddress, AgentParameters snmpParameters)
        {
            string[] oids = new string[] {
                "1.3.6.1.2.1.1.1.0",
                "1.3.6.1.2.1.1.2.0",
                "1.3.6.1.2.1.1.4.0",
                "1.3.6.1.2.1.1.5.0",
                "1.3.6.1.2.1.1.6.0"
            };

            try
            {
                VbCollection collection = GetSnmp(ipAddress, snmpParameters, oids);

                Device resultObj = new Device(
                  ipAddress.ToString(),
                  collection[0]?.Value.ToString().Trim(),
                  collection[1]?.Value.ToString().Trim(),
                  collection[2]?.Value.ToString().Trim(),
                  collection[3]?.Value.ToString().Trim(),
                  collection[4]?.Value.ToString().Trim(), 
                  snmpParameters.Community.ToString(),
                  snmpParameters.Version
                );

                return resultObj;
            }
            catch (Exception e)
            {
                //Console.WriteLine($"{ipAddress}: Error Fetching SNMP ({e.Message})");
                return new Device() { IpAddress = ipAddress, Errors = e.Message };
            }
        }

        /// <summary>
        /// Grab SNMP VbCollection results form a array of OIDs
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="snmpParameters"></param>
        /// <param name="oids"></param>
        /// <returns></returns>
        private VbCollection GetSnmp(IPAddress ipAddress, IAgentParameters snmpParameters, IEnumerable<string> oids)
        {
            UdpTarget target = new UdpTarget(ipAddress, 161, 2000, config.Settings.SnmpRetries);
            Pdu pdu = new Pdu(PduType.Get);
            foreach (string oid in oids)
            {
                pdu.VbList.Add(oid);
            }
            try
            {
                SnmpPacket result;
                if (snmpParameters.Version == SnmpVersion.Ver2)
                    result = (SnmpV2Packet)target.Request(pdu, snmpParameters);
                else
                    result = (SnmpV1Packet)target.Request(pdu, snmpParameters);
                if (result != null)
                {
                    if (result.Pdu.ErrorStatus == 0)
                    {
                        return result.Pdu.VbList;
                    }
                    else
                    {
                        throw new SnmpErrorStatusException("Snmp Error Occured", result.Pdu.ErrorStatus, result.Pdu.ErrorIndex);
                    }
                }
                else
                {
                    throw new SnmpException("Snmp returned a null value");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Create snmpParameters with preconfigured Community and mode Set to Version 2
        /// </summary>
        /// <param name="community"></param>
        private AgentParameters createSnmpParameters(string community, Int16 version = 2)
        {
            OctetString oCommunity = new OctetString(community);
            AgentParameters snmpParam = new AgentParameters(oCommunity);

            if (version == 1)
                snmpParam.Version = SnmpVersion.Ver1;
            else
                snmpParam.Version = SnmpVersion.Ver2;

            return snmpParam;
        }
    }
}

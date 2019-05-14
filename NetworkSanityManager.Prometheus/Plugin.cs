using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetworkSanityManager.Prometheus
{
    public class Plugin
    {
        private Configuration _config { get; }
        private Dictionary<string, StringBuilder> _output;
        private AuthenticationMethod AuthMethod;
        private IEnumerable<KeyValuePair<string, string>> _devices;

        public Plugin(IEnumerable<KeyValuePair<string, string>> deviceKVP)
        {
            _config = new Configuration();
            _devices = deviceKVP.OrderBy((d) => d.Key);
            _output = new Dictionary<string, StringBuilder>();
            AuthMethod = CreateAuthMethod();

            Console.WriteLine();
            Console.WriteLine("Prometheus Plugin Started...");
            Console.WriteLine();

            if (_config.Settings.Enabled)
                Generate();
        }

        public void Generate()
        {
            foreach (KeyValuePair<string, string> device in _devices)
            {
                var deviceType = _config.DeviceTypes.FirstOrDefault((devType) => devType.Vendor == device.Value);
         
                if (deviceType != null)
                {
                    if (_output.ContainsKey(deviceType.TargetFilename) == false)
                    {
                        _output[deviceType.TargetFilename] = new StringBuilder();
                        _output[deviceType.TargetFilename].AppendLine("- targets:");
                    }
                    
                    _output[deviceType.TargetFilename].AppendLine($"  - {device.Key}");
                }
                else
                {
                    if (_config.Settings.Debug)
                        Console.WriteLine($"Prometheus: {device.Key} Device Type Unknown ({device.Value})");
                }
            }
        }

        public string Preview()
        {
            if (_config.Settings.Enabled) { 
                var output = new StringBuilder();

                foreach (var deviceType in _output)
                {
                    output.AppendLine(deviceType.Key);
                    output.AppendLine(deviceType.Value.ToString());
                }

                return output.ToString();
            } 
            else
            {
                return "Plugin is Disabled";
            }
        }

        public String Commit()
        {
            if (_config.Settings.Enabled)
            {
                ConnectionInfo connInfo = new ConnectionInfo(
                    _config.Settings.Server,
                    _config.Settings.Username,
                    AuthMethod);

                try
                {
                    using (var client = new ScpClient(connInfo))
                    {
                        client.Connect();
                        if (client.IsConnected)
                        {
                            //TODO: check if the file is changed before uploading a new one if it is new back it up
                            foreach (var newFile in _output)
                            {
                                var location = $"{ _config.Settings.TargetFolderPath }/{ newFile.Key }".Replace("//", "/");

                                var originalFileMS = new MemoryStream();
                                try
                                {
                                    client.Download(location, originalFileMS);
                                } catch (Exception e)
                                {
                                    Console.WriteLine($"{location}: File Check Failed ({e.Message})");
                                }

                                MemoryStream newDataStream = new MemoryStream();
                                ASCIIEncoding encoding = new ASCIIEncoding();
                                newDataStream.Write(encoding.GetBytes(newFile.Value.ToString()), 0, newFile.Value.Length);

                                var NewVsExisting = Tools.CompareMemoryStreams(originalFileMS, newDataStream);
                                if (NewVsExisting)
                                {
                                    Console.WriteLine($"Skipping - Not Changed: {location}");
                                }
                                else
                                {
                                    newDataStream.Position = 0;
                                    client.Upload(newDataStream, location);
                                    Console.WriteLine($"Uploaded: {location}");
                                }
                            }
                            client.Disconnect();
                        }
                    }
                }
                catch (Exception e)
                {
                    return $"Prometheus: Error with SSH Upload ({e.Message}";
                }

                // We made it past the try block so upload was successful;
                return $"Prometheus: Commit Complete";
            }
            else
            {
                return "Plugin is disabled";
            }
        }

        /// <summary>
        /// Based on configuration generate a correct SSH Authentication Method
        /// </summary>
        private AuthenticationMethod CreateAuthMethod()
        {
            if (string.IsNullOrWhiteSpace(_config.Settings.CertFile))
            {
                return new PasswordAuthenticationMethod(
                    _config.Settings.Username,
                    _config.Settings.Password);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_config.Settings.CertFilePassword))
                    return new PrivateKeyAuthenticationMethod(
                        _config.Settings.Username,
                        new PrivateKeyFile(_config.Settings.CertFile));
                else
                    return new PrivateKeyAuthenticationMethod(
                        _config.Settings.Username,
                        new PrivateKeyFile(_config.Settings.CertFile, _config.Settings.CertFilePassword));
            }
        }
    }
}

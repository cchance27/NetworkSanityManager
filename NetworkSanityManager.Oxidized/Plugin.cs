using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetworkSanityManager.Oxidized
{
    public class Plugin
    {
        private Configuration _config { get; }
        private IEnumerable<OxidizedInputModel> _devices { get; }
        private StringBuilder _output;
        public AuthenticationMethod AuthMethod { get; }

        public Plugin(IEnumerable<OxidizedInputModel> devices)
        {
            _config = new Configuration();
            _devices = devices.OrderBy((x) => x.Name);
            _output = new StringBuilder();
            AuthMethod = CreateAuthMethod();

            Console.WriteLine();
            Console.WriteLine("Oxidized Plugin Started...");
            Console.WriteLine();

            if (_config.Settings.Enabled)
                Generate();
        }

        /// <summary>
        /// Internal function that converts an array of DeviceInputModel to valid output string for oxidized
        /// </summary>
        private void Generate()
        {
            foreach (OxidizedInputModel device in _devices)
            {
                var deviceTypes = _config.DeviceTypes.Where<DeviceType>((devType) => devType.Vendor == device.Vendor).ToList<DeviceType>();
                if (deviceTypes.Count > 0)
                {
                    DeviceType actualDeviceType = null; 
                    foreach (DeviceType devType in deviceTypes)
                    {
                        if (devType.ModelRegex != null)
                        {
                            // Model restriction specified we need to check if this device matches. 
                            Match m = Regex.Match(device.Model, devType.ModelRegex, RegexOptions.IgnoreCase);
                            if (m.Success)
                            {
                                actualDeviceType = devType;
                                break; 
                            }
                        }
                        else
                        {
                            // No model restriction use whatever the first devType is
                            actualDeviceType = devType;
                            break;
                        }
                    } // foreach devtype
                    if (actualDeviceType != null)
                    {
                        // We have a deviceType 
                        var name = device.Name.ToLower().Trim();
                        var address = device.Address.Trim();
                        _output.AppendLine($"{name}:{address}:{actualDeviceType.Type}:{actualDeviceType.Username}:{actualDeviceType.Password}");
                    } 
                    else
                    {
                        // We had deviceTypes but none of the models allowed matched.
                        if (_config.Settings.Debug)
                            Console.WriteLine($"Oxidized: {device.Name} Device Type ({device.Vendor}) Model Unknown ({device.Model})");
                    }
                } // if deviceType.Count
                else
                {
                    if (_config.Settings.Debug)
                        Console.WriteLine($"Oxidized: {device.Name} Device Type Unknown ({device.Vendor})");
                }
            }
        }

        /// <summary>
        /// Return a string preview of the expected config output for oxidized
        /// </summary>
        public String Preview()
        {
            if (_config.Settings.Enabled)
            {
                return _output.ToString();
            }
            else
            {
                return "Plugin is disabled";
            }
        }

        /// <summary>
        /// Commit the changes and upload them to the server
        /// </summary>
        public String Commit()
        {
            if (_config.Settings.Enabled)
            {
                ConnectionInfo connInfo = new ConnectionInfo(
                    _config.Settings.Server,
                    _config.Settings.ScpUsername,
                    AuthMethod);

                try
                {
                    using (var client = new ScpClient(connInfo))
                    {
                        client.Connect();
                        if (client.IsConnected)
                        {
                            var originalFileMS = new MemoryStream();
                            try
                            {
                                client.Download(_config.Settings.FilePath, originalFileMS);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"{_config.Settings.FilePath}: File Check Failed ({e.Message})");
                            }

                            MemoryStream newDataStream = new MemoryStream();
                            UTF8Encoding encoding = new UTF8Encoding();
                            newDataStream.Write(encoding.GetBytes(_output.ToString()), 0, _output.Length);

                            var NewVsExisting = Tools.CompareMemoryStreams(originalFileMS, newDataStream);
                            if (NewVsExisting)
                            {
                                Console.WriteLine($"Skipping - Not Changed: {_config.Settings.FilePath}");
                            }
                            else
                            {
                                newDataStream.Position = 0;
                                client.Upload(newDataStream, _config.Settings.FilePath);
                                Console.WriteLine($"Uploaded: {_config.Settings.FilePath}");

                                ReloadOxidizedAsync();
                            }
                            client.Disconnect();
                        }
                    }
                }
                catch (Exception e)
                {
                    return $"Oxidized: Error with SSH Upload ({e.Message}";
                }

                // We made it past the try block so upload was successful;
                return $"Oxidized: Commit Complete";
            }
            else
            {
                return "Plugin is disabled";
            }
        }

        /// <summary>
        /// Called by Commit to reload the API and check the new config file that was uploaded.
        /// </summary>
        /// <returns></returns>
        private bool ReloadOxidizedAsync()
        {
            if (_config.Settings.ReloadAPI != null)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod("GET"), _config.Settings.ReloadAPI))
                        {
                            httpClient.SendAsync(request).Wait();
                            Console.WriteLine($"Oxidized: Reload Command Issued");
                        }
                    }
                    return true;
                } catch (Exception e) {
                    Console.WriteLine($"Oxidized: Reload API Error ({e.Message})");
                }
            }
            return false;
        }

        /// <summary>
        /// Based on configuration generate a correct SSH Authentication Method
        /// </summary>
        private AuthenticationMethod CreateAuthMethod()
        {
            if (string.IsNullOrWhiteSpace(_config.Settings.ScpCertFile))
            {
                return new PasswordAuthenticationMethod(
                    _config.Settings.ScpUsername,
                    _config.Settings.ScpPassword);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_config.Settings.ScpCertFilePassword))
                    return new PrivateKeyAuthenticationMethod(
                        _config.Settings.ScpUsername,
                        new PrivateKeyFile(_config.Settings.ScpCertFile));
                else
                    return new PrivateKeyAuthenticationMethod(
                        _config.Settings.ScpUsername,
                        new PrivateKeyFile(_config.Settings.ScpCertFile, _config.Settings.ScpCertFilePassword));
            }
        }
    }
}
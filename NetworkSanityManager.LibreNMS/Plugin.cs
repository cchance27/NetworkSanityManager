using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSanityManager.LibreNMS
{
    public class Plugin
    {
        private Configuration _config { get; }
        private IEnumerable<LibreNMSInputModel> _allDevices { get; }
        private IEnumerable<LibreNMSInputModel> _devices { get; }
        private HttpClient _httpClient { get; }

        public Plugin(IEnumerable<LibreNMSInputModel> devices)
        {
            _config = new Configuration();
            _allDevices = devices;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _config.Settings.key);

            if (_config.Settings.Enabled)
            {
                _devices = CheckAPIForMissingDevices();
            }
        }

        private IEnumerable<LibreNMSInputModel> CheckAPIForMissingDevices() => _allDevices.AsParallel().WithDegreeOfParallelism(4).Where(CheckAPIDevIsNew).ToArray();

        private bool CheckAPIDevIsNew(LibreNMSInputModel dev)
        {
            // poll api return devices that are missing data so are obviously not present
            var url = $"{_config.Settings.url}devices/{dev.Hostname}";
            return _httpClient.GetStringAsync(url).GetAwaiter().GetResult().Contains("sysObjectID") == false;
        }

        public string Preview()
        {
            if (_config.Settings.Enabled)
            {
                var sb = new StringBuilder();
                foreach(var dev in _devices)
                {
                    sb.AppendLine($"{dev.Hostname}");
                }
                return sb.ToString();
            }
            return null;
        }

        public string Commit()
        {
            if (_config.Settings.Enabled)
            {
                var url = $"{_config.Settings.url}devices";
                var sb = new StringBuilder();
                foreach (var dev in _devices)
                {
                    var content = new StringContent("{\"hostname\":\"" + dev.Hostname + "\", \"community\": \"" + dev.Community + "\", \"version\": \"" + dev.GetVersionString() + "\"}");

                    var post = _httpClient.PostAsync(url, content).GetAwaiter().GetResult();
                    var resp = post.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var result = JsonConvert.DeserializeObject<LibreAPIAddResponse>(resp);
                    sb.AppendLine(dev.Hostname + ": " + result.message);
                }
                return sb.ToString();
            }
            return null;
        }
    }
}

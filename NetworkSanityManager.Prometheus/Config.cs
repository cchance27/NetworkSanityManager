using Microsoft.Extensions.Configuration;
using System.IO;

namespace NetworkSanityManager.Prometheus
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
                .AddJsonFile("config.prometheus.json", optional: false, reloadOnChange: true);

            config = builder.Build();

            _deviceTypes = config.GetSection("Devices").Get<DeviceType[]>();
            _settings = config.GetSection("Settings").Get<Settings>();
        }
    }

    public class DeviceType
    {
        public string Vendor { get; set; }
        public string TargetFilename { get; set; }
    }

    public class Settings
    {
        public bool Enabled { get; set; } = false;
        public string TargetFolderPath { get; set; } = "/tmp/targets";
        public string Server { get; set; } = "127.0.0.1";
        public string Username { get; set; } = "root";
        public string Password { get; set; } = "password";
        public string CertFile { get; set; } = null;
        public string CertFilePassword { get; set; } = null;
        public bool Debug { get; set; } = false;
    }
}
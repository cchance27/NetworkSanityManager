using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace NetworkSanityManager.Oxidized
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
                .AddJsonFile("config.oxidized.json", optional: false, reloadOnChange: true);

            config = builder.Build();

            _deviceTypes = config.GetSection("Devices").Get<DeviceType[]>();
            _settings = config.GetSection("Settings").Get<Settings>();
        }
    }

    public class DeviceType
    {
        public string Vendor { get; set; }

        // Restrict based on a regex
        public string ModelRegex { get; set; } = null;

        public string Type { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    
    public class Settings
    {
        public bool Enabled { get; set; } = false;
        public string FilePath { get; set; } = "/tmp/router.db";
        // Example reload API: http://127.0.0.1:8888/reload?format=json
        public string ReloadAPI { get; set; } = null;
        public string Server { get; set; } = "127.0.0.1";
        public string ScpUsername { get; set; } = "root";
        public string ScpPassword { get; set; } = "password";
        public string ScpCertFile { get; set; } = null;
        public string ScpCertFilePassword { get; set; } = null;
        public bool Debug { get; set; } = false;
    }
}
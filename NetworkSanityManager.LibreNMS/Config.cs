using Microsoft.Extensions.Configuration;
using System.IO;

namespace NetworkSanityManager.LibreNMS
{
    public class Configuration
    {
        private IConfigurationRoot config;

        private Settings _settings;
        public Settings Settings { get => _settings; }

        public Configuration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.librenms.json", optional: false, reloadOnChange: true);

            config = builder.Build();

            _settings = config.GetSection("Settings").Get<Settings>();
        }
    }

    public class Settings
    {
        public bool Enabled { get; set; } = false;
        public string url { get; set; }
        public string key { get; set; }
        public int threads { get; set; } = 1;
    }
}
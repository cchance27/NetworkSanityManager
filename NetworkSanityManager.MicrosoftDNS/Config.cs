using Microsoft.Extensions.Configuration;
using System.IO;

namespace NetworkSanityManager.MicrosoftDNS
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
                .AddJsonFile("config.microsoftdns.json", optional: false, reloadOnChange: true);

            config = builder.Build();

            _settings = config.GetSection("Settings").Get<Settings>();
        }
    }

    public class Settings
    {
        public bool Enabled { get; set; } = false;
        public string Server { get; set; } = "127.0.0.1";
        public string Domain { get; set; } = "domain.tld";
        public string DomainPrefix { get; set; } = null; // device.prefix.domain.tld
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
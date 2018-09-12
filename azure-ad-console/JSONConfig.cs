using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace azure_ad_console
{
    public class JSONConfig : IConfiguration
    {
        private IConfiguration _conf;
        public JSONConfig() {
            _conf = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
        }

        public string this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _conf.GetChildren();
        }

        public IChangeToken GetReloadToken()
        {
            return _conf.GetReloadToken();
        }

        public IConfigurationSection GetSection(string key)
        {
            return _conf.GetSection(key);
        }
    }
}
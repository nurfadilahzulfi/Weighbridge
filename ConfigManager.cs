using System;
using System.IO;
using Weighbridge.Core.Config;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public static class ConfigManager
    {
        private static readonly IConfigXmlService _xmlService = new ConfigXmlService();

        public static string GetConfigPath()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "Config.xml");
        }

        public static SBOConfig sboConfig { get; set; }
        public static WBConfig wbConfig { get; set; }

        private static readonly string ConfigPath = GetConfigPath();

        public static void Load()
        {
            var (wb, sbo) = _xmlService.LoadFromFile(ConfigPath);
            wbConfig = wb;
            sboConfig = sbo;
        }

        public static void Save()
        {
            _xmlService.SaveToFile(ConfigPath, wbConfig, sboConfig);
        }
    }
}

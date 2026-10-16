using System.IO;
using System.Xml.Linq;
using Weighbridge.Core.Config;
using Weighbridge.Core.Encryptions;

namespace Weighbridge.Core.Services
{
    public interface IConfigXmlService
    {
        (WBConfig wb, SBOConfig sbo) ParseXml(string xmlContent);
        string GenerateXml(WBConfig wb, SBOConfig sbo);
        (WBConfig wb, SBOConfig sbo) LoadFromFile(string filePath);
        void SaveToFile(string filePath, WBConfig wb, SBOConfig sbo);
    }

    public class ConfigXmlService : IConfigXmlService
    {
        private readonly Encryption _encryption;

        public ConfigXmlService() : this(new Encryption()) { }

        public ConfigXmlService(Encryption encryption)
        {
            _encryption = encryption ?? new Encryption();
        }

        public (WBConfig wb, SBOConfig sbo) ParseXml(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return (new WBConfig(), new SBOConfig());
            }

            var xml = XDocument.Parse(xmlContent);
            return ExtractConfigsFromXml(xml);
        }

        public (WBConfig wb, SBOConfig sbo) LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return (new WBConfig(), new SBOConfig());
            }

            var xml = XDocument.Load(filePath);
            return ExtractConfigsFromXml(xml);
        }

        private (WBConfig wb, SBOConfig sbo) ExtractConfigsFromXml(XDocument xml)
        {
            var configNode = xml.Element("ADDON_CONFIGURATION")?.Element("CONFIGURATION");
            var wbNode = configNode?.Element("WEIGHBRIDGE");
            var sboNode = configNode?.Element("SBO_DATABASE");

            var wb = new WBConfig
            {
                portName = wbNode?.Element("PORT_NAME")?.Value ?? "",
                baudRate = wbNode?.Element("BAUD_RATE")?.Value ?? "",
                dataBits = wbNode?.Element("DATA_BITS")?.Value ?? "",
                readTimeout = int.TryParse(wbNode?.Element("READ_TIMEOUT")?.Value, out int readTimeout) ? readTimeout : 0,
                writeTimeout = int.TryParse(wbNode?.Element("WRITE_TIMEOUT")?.Value, out int writeTimeout) ? writeTimeout : 0,
                prefix = wbNode?.Element("PREFIX")?.Value ?? ""
            };

            string dbPass = sboNode?.Element("SBO_DATABASE_PASSWORD")?.Value ?? "";
            string sboPass = sboNode?.Element("SBO_PASSWORD")?.Value ?? "";

            var sbo = new SBOConfig
            {
                serverType = sboNode?.Element("DATABASE_SERVER_TYPE")?.Value ?? "",
                serverName = sboNode?.Element("DATABASE_SERVER_NAME")?.Value ?? "",
                database = sboNode?.Element("SBO_DATABASE_NAME")?.Value ?? "",
                databaseUsername = sboNode?.Element("SBO_DATABASE_USERNAME")?.Value ?? "",
                databasePassword = SafeDecrypt(dbPass),
                sboUsername = sboNode?.Element("SBO_USER_NAME")?.Value ?? "",
                sboPassword = SafeDecrypt(sboPass)
            };

            return (wb, sbo);
        }

        private string SafeDecrypt(string encrypted)
        {
            if (string.IsNullOrWhiteSpace(encrypted)) return "";
            try
            {
                return _encryption.decrypt(encrypted);
            }
            catch
            {
                return encrypted;
            }
        }

        public string GenerateXml(WBConfig wb, SBOConfig sbo)
        {
            wb = wb ?? new WBConfig();
            sbo = sbo ?? new SBOConfig();

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("ADDON_CONFIGURATION",
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                    new XElement("CONFIGURATION",
                        new XElement("WEIGHBRIDGE",
                            new XElement("PORT_NAME", wb.portName ?? ""),
                            new XElement("BAUD_RATE", wb.baudRate ?? ""),
                            new XElement("DATA_BITS", wb.dataBits ?? ""),
                            new XElement("READ_TIMEOUT", wb.readTimeout),
                            new XElement("WRITE_TIMEOUT", wb.writeTimeout),
                            new XElement("PREFIX", wb.prefix ?? "")
                        ),
                        new XElement("SBO_DATABASE",
                            new XElement("DATABASE_SERVER_TYPE", sbo.serverType ?? ""),
                            new XElement("DATABASE_SERVER_NAME", sbo.serverName ?? ""),
                            new XElement("SBO_DATABASE_NAME", sbo.database ?? ""),
                            new XElement("SBO_DATABASE_USERNAME", sbo.databaseUsername ?? ""),
                            new XElement("SBO_DATABASE_PASSWORD", sbo.databasePassword ?? ""),
                            new XElement("SBO_USER_NAME", sbo.sboUsername ?? ""),
                            new XElement("SBO_PASSWORD", sbo.sboPassword ?? "")
                        )
                    )
                )
            );

            return doc.ToString();
        }

        public void SaveToFile(string filePath, WBConfig wb, SBOConfig sbo)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            wb = wb ?? new WBConfig();
            sbo = sbo ?? new SBOConfig();

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("ADDON_CONFIGURATION",
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                    new XElement("CONFIGURATION",
                        new XElement("WEIGHBRIDGE",
                            new XElement("PORT_NAME", wb.portName ?? ""),
                            new XElement("BAUD_RATE", wb.baudRate ?? ""),
                            new XElement("DATA_BITS", wb.dataBits ?? ""),
                            new XElement("READ_TIMEOUT", wb.readTimeout),
                            new XElement("WRITE_TIMEOUT", wb.writeTimeout),
                            new XElement("PREFIX", wb.prefix ?? "")
                        ),
                        new XElement("SBO_DATABASE",
                            new XElement("DATABASE_SERVER_TYPE", sbo.serverType ?? ""),
                            new XElement("DATABASE_SERVER_NAME", sbo.serverName ?? ""),
                            new XElement("SBO_DATABASE_NAME", sbo.database ?? ""),
                            new XElement("SBO_DATABASE_USERNAME", sbo.databaseUsername ?? ""),
                            new XElement("SBO_DATABASE_PASSWORD", sbo.databasePassword ?? ""),
                            new XElement("SBO_USER_NAME", sbo.sboUsername ?? ""),
                            new XElement("SBO_PASSWORD", sbo.sboPassword ?? "")
                        )
                    )
                )
            );

            doc.Save(filePath);
        }
    }
}

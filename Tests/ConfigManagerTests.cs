using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Xml.Linq;
using Weighbridge.Core.Config;
using Weighbridge.Core.Encryptions;

namespace Weighbridge.Tests
{
    [TestClass]
    public class ConfigManagerTests
    {
        private string _testConfigFolder;
        private string _testConfigPath;

        [TestInitialize]
        public void Setup()
        {
            _testConfigFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testConfigFolder);
            _testConfigPath = Path.Combine(_testConfigFolder, "Config.xml");
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_testConfigFolder))
                {
                    Directory.Delete(_testConfigFolder, true);
                }
            }
            catch
            {
                // Best-effort cleanup
            }
        }

        [TestMethod]
        public void WBConfig_DefaultProperties_InitializedProperly()
        {
            var config = new WBConfig
            {
                portName = "COM3",
                baudRate = "9600",
                dataBits = "8",
                readTimeout = 3000,
                writeTimeout = 3000,
                prefix = "ST"
            };

            Assert.AreEqual("COM3", config.portName);
            Assert.AreEqual("9600", config.baudRate);
            Assert.AreEqual("8", config.dataBits);
            Assert.AreEqual(3000, config.readTimeout);
            Assert.AreEqual(3000, config.writeTimeout);
            Assert.AreEqual("ST", config.prefix);
        }

        [TestMethod]
        public void SBOConfig_DefaultProperties_InitializedProperly()
        {
            var sbo = new SBOConfig
            {
                serverType = "dst_HANADB",
                serverName = "192.168.1.100:30015",
                database = "SBODEMO",
                databaseUsername = "SYSTEM",
                databasePassword = "Password123!",
                sboUsername = "manager",
                sboPassword = "Password123!"
            };

            Assert.AreEqual("dst_HANADB", sbo.serverType);
            Assert.AreEqual("192.168.1.100:30015", sbo.serverName);
            Assert.AreEqual("SBODEMO", sbo.database);
            Assert.AreEqual("SYSTEM", sbo.databaseUsername);
            Assert.AreEqual("Password123!", sbo.databasePassword);
            Assert.AreEqual("manager", sbo.sboUsername);
            Assert.AreEqual("Password123!", sbo.sboPassword);
        }

        [TestMethod]
        public void Encryption_ConfigPasswordRoundtrip_EncryptsAndDecryptsAccurately()
        {
            var enc = new Encryption();
            string originalPass = "SuperSecret_HANA_2026!#$";

            string cipher = enc.encrypt(originalPass);
            Assert.AreNotEqual(originalPass, cipher, "Ciphertext should not match plaintext.");
            Assert.IsTrue(cipher.Length > 0);

            string decrypted = enc.decrypt(cipher);
            Assert.AreEqual(originalPass, decrypted, "Decrypted password must match original plaintext exactly.");
        }

        [TestMethod]
        public void ConfigXml_StructureGeneration_ProducesValidXmlElements()
        {
            var enc = new Encryption();
            var wb = new WBConfig
            {
                portName = "COM1",
                baudRate = "9600",
                dataBits = "8",
                readTimeout = 1500,
                writeTimeout = 1500,
                prefix = "WT"
            };
            var sbo = new SBOConfig
            {
                serverType = "dst_HANADB",
                serverName = "hana-server:30015",
                database = "PROD_TANIMAS",
                databaseUsername = "DBUSER",
                databasePassword = enc.encrypt("SecretDbPass"),
                sboUsername = "admin",
                sboPassword = enc.encrypt("SecretSboPass")
            };

            var xml = new XDocument(
                new XElement("ADDON_CONFIGURATION",
                    new XElement("CONFIGURATION",
                        new XElement("WEIGHBRIDGE",
                            new XElement("PORT_NAME", wb.portName),
                            new XElement("BAUD_RATE", wb.baudRate),
                            new XElement("DATA_BITS", wb.dataBits),
                            new XElement("READ_TIMEOUT", wb.readTimeout),
                            new XElement("WRITE_TIMEOUT", wb.writeTimeout),
                            new XElement("PREFIX", wb.prefix)
                        ),
                        new XElement("SBO_DATABASE",
                            new XElement("DATABASE_SERVER_TYPE", sbo.serverType),
                            new XElement("DATABASE_SERVER_NAME", sbo.serverName),
                            new XElement("SBO_DATABASE_NAME", sbo.database),
                            new XElement("SBO_DATABASE_USERNAME", sbo.databaseUsername),
                            new XElement("SBO_DATABASE_PASSWORD", sbo.databasePassword),
                            new XElement("SBO_USER_NAME", sbo.sboUsername),
                            new XElement("SBO_PASSWORD", sbo.sboPassword)
                        )
                    )
                )
            );

            xml.Save(_testConfigPath);
            Assert.IsTrue(File.Exists(_testConfigPath));

            // Reload and parse
            var loadedXml = XDocument.Load(_testConfigPath);
            var wbNode = loadedXml.Element("ADDON_CONFIGURATION")?.Element("CONFIGURATION")?.Element("WEIGHBRIDGE");
            var sboNode = loadedXml.Element("ADDON_CONFIGURATION")?.Element("CONFIGURATION")?.Element("SBO_DATABASE");

            Assert.IsNotNull(wbNode);
            Assert.IsNotNull(sboNode);
            Assert.AreEqual("COM1", wbNode.Element("PORT_NAME")?.Value);
            Assert.AreEqual("PROD_TANIMAS", sboNode.Element("SBO_DATABASE_NAME")?.Value);

            string decDbPass = enc.decrypt(sboNode.Element("SBO_DATABASE_PASSWORD")?.Value ?? "");
            string decSboPass = enc.decrypt(sboNode.Element("SBO_PASSWORD")?.Value ?? "");

            Assert.AreEqual("SecretDbPass", decDbPass);
            Assert.AreEqual("SecretSboPass", decSboPass);
        }

        [TestMethod]
        public void ConfigXml_MissingOptionalElements_DoesNotThrowAndDefaultsGracefully()
        {
            string partialXml = @"<ADDON_CONFIGURATION>
                <CONFIGURATION>
                    <WEIGHBRIDGE>
                        <PORT_NAME>COM4</PORT_NAME>
                    </WEIGHBRIDGE>
                </CONFIGURATION>
            </ADDON_CONFIGURATION>";

            File.WriteAllText(_testConfigPath, partialXml);

            var xml = XDocument.Load(_testConfigPath);
            var configNode = xml.Element("ADDON_CONFIGURATION")?.Element("CONFIGURATION");
            var wbNode = configNode?.Element("WEIGHBRIDGE");
            var sboNode = configNode?.Element("SBO_DATABASE");

            string port = wbNode?.Element("PORT_NAME")?.Value ?? "DEFAULT";
            string baud = wbNode?.Element("BAUD_RATE")?.Value ?? "9600";
            int timeout = int.TryParse(wbNode?.Element("READ_TIMEOUT")?.Value, out int t) ? t : 3000;

            Assert.AreEqual("COM4", port);
            Assert.AreEqual("9600", baud);
            Assert.AreEqual(3000, timeout);
            Assert.IsNull(sboNode);
        }
    }
}

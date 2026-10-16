using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge;
using Weighbridge.Connection;
using Weighbridge.Core.Config;

namespace Weighbridge.Tests
{
    [TestClass]
    public class HanaConnectionParsingTests
    {
        [TestMethod]
        public void HanaConnectionStrings_LegacyInstancePort30013_TranslatesToSqlPort30015()
        {
            var hanaConn = new HANAConnetion();
            var config = new SBOConfig
            {
                serverName = "NDB@sbo-hana-tri:30013",
                database = "TANIMASTRAINING",
                databaseUsername = "SYSTEM",
                databasePassword = "secretPassword"
            };

            string connStr = hanaConn.HanaConnectionStrings(config);

            Assert.IsTrue(connStr.Contains("server=sbo-hana-tri:30015"));
            Assert.IsTrue(connStr.Contains("currentSchema=TANIMASTRAINING"));
            Assert.IsTrue(connStr.Contains("userid=SYSTEM"));
            Assert.IsTrue(connStr.Contains("password=secretPassword"));
        }

        [TestMethod]
        public void HanaConnectionStrings_CustomPublicPort_PreservesCustomPort()
        {
            var hanaConn = new HANAConnetion();
            var config = new SBOConfig
            {
                serverName = "NDB@182.160.55.95:65488",
                database = "TANIMASTESTING",
                databaseUsername = "SYSTEM",
                databasePassword = "secretPassword"
            };

            string connStr = hanaConn.HanaConnectionStrings(config);

            Assert.IsTrue(connStr.Contains("server=182.160.55.95:65488"), "Port kustom NAT forward harus tetap dipertahankan.");
            Assert.IsTrue(connStr.Contains("currentSchema=TANIMASTESTING"));
        }

        [TestMethod]
        public void HanaConnectionStrings_DirectIpWithoutPrefix_ParsesSafely()
        {
            var hanaConn = new HANAConnetion();
            var config = new SBOConfig
            {
                serverName = "182.160.55.95:65488",
                database = "TANIMASDEV",
                databaseUsername = "SYSTEM",
                databasePassword = "secretPassword"
            };

            string connStr = hanaConn.HanaConnectionStrings(config);

            Assert.IsTrue(connStr.Contains("server=182.160.55.95:65488"));
            Assert.IsTrue(connStr.Contains("currentSchema=TANIMASDEV"));
        }

        [TestMethod]
        public void HanaConnectionStrings_HostWithoutPort_DefaultsTo30015()
        {
            var hanaConn = new HANAConnetion();
            var config = new SBOConfig
            {
                serverName = "192.168.1.50",
                database = "TANIMAS_DB",
                databaseUsername = "SYSTEM",
                databasePassword = "secretPassword"
            };

            string connStr = hanaConn.HanaConnectionStrings(config);

            Assert.IsTrue(connStr.Contains("server=192.168.1.50:30015"));
        }

#pragma warning disable CS0618
        [TestMethod]
        public void HanaConnectionString_ModGlobalLegacyInstancePort_TranslatesToSqlPort30015()
        {
            var hanaConn = new HANAConnetion();
            var modGlobal = new ModGlobal
            {
                serverName = "NDB@sbo-hana-tri:30013",
                database = "TANIMASTRAINING",
                databaseUsername = "SYSTEM",
                databasePassword = "secretPassword"
            };

            string connStr = hanaConn.HanaConnectionString(modGlobal);

            Assert.IsTrue(connStr.Contains("server=sbo-hana-tri:30015"));
            Assert.IsTrue(connStr.Contains("currentSchema=TANIMASTRAINING"));
            Assert.IsTrue(connStr.Contains("userid=SYSTEM"));
            Assert.IsTrue(connStr.Contains("password=secretPassword"));
        }

        [TestMethod]
        public void HanaConnectionString_ModGlobalCustomPort_PreservesCustomPort()
        {
            var hanaConn = new HANAConnetion();
            var modGlobal = new ModGlobal
            {
                serverName = "182.160.55.95:65488",
                database = "TANIMASLIVE",
                databaseUsername = "SYSTEM",
                databasePassword = "secretPassword"
            };

            string connStr = hanaConn.HanaConnectionString(modGlobal);

            Assert.IsTrue(connStr.Contains("server=182.160.55.95:65488"));
            Assert.IsTrue(connStr.Contains("currentSchema=TANIMASLIVE"));
        }
#pragma warning restore CS0618
    }
}

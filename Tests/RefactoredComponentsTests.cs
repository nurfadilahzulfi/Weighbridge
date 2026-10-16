using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Config;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class RefactoredComponentsTests
    {
        private class MockHanaRepository : IHanaRepository
        {
            public bool AuthSuccess { get; set; } = true;
            public string MockFullName { get; set; } = "Operator Test";
            public string MockRole { get; set; } = "SUPERUSER";
            public string MockErrorMessage { get; set; } = "";

            public (bool Success, string FullName, string Role, string ErrorMessage) AuthenticateUser(string username, string password)
            {
                if (AuthSuccess)
                {
                    return (true, MockFullName, MockRole, "");
                }
                return (false, "", "", string.IsNullOrEmpty(MockErrorMessage) ? "User atau Password salah!" : MockErrorMessage);
            }

            public string GenerateTiketNumber() => "TK-001";
            public string GetNextWBCode() => "WB-001";
            public List<string> GetKodeDokumenPabean() => new List<string> { "BC 2.0" };
            public DataTable GetTransporterInternal() => new DataTable();
            public DataTable SearchBaseDocument(string docType, string vendorRef) => new DataTable();
            public DocQuotaInfo GetDocQuotaInfo(string docType, int docEntry, int lineNum) => new DocQuotaInfo();
            public (double openQty, double totalQty) GetFreshQuota(string docType, int docEntry, int lineNum) => (1000, 1000);
            public string GetFreshKontrakNo(string docType, int docEntry) => "KONTRAK-01";
            public PabeanData GetPabeanData(string docType, int docEntry) => new PabeanData();
            public List<DocumentLookupItem> GetOpenDocuments(string docType) => new List<DocumentLookupItem>();
            public List<PendingTicketInfo> GetPendingTickets(string docType, string searchKey) => new List<PendingTicketInfo>();
            public string GetWBCodeByTiket(string tiketNum) => "WB-001";
            public System.Collections.Generic.List<string> GetWBCodesByTiket(string tiketNum) => new System.Collections.Generic.List<string> { "WB-001" };
            public (string isIns, int baseType, int docEntry, int lineNum) SearchPOSOReserve(string docType, int docEntrySO) => ("N", 0, 0, 0);
            public string GetDocNumByEntry(string tableName, string docEntry) => "DOC-01";
            public string GetPostedDocNumsForTicket(string tableName, string tiketNum) => "";
            public List<string> GetNomorTangki(string itemName) => new List<string>();
            public DataTable SearchBaseDocJoinWbData(string docType, string docNum) => new DataTable();
            public DataTable SearchTicketByTiketNum(string noTiket) => new DataTable();
            public List<TicketLookupItem> GetPendingTicketLookupList() => new List<TicketLookupItem>();
            public (string itemCode, string whsCode) GetItemAndWhsCodeFromSO(int docEntry, int lineNum) => ("", "");
            public (string cardCode, string itemCode) GetPOVendorAndItem(int docEntry, int lineNum) => ("", "");
            public List<(int DocEntry, string DocNum, int LineNum, double OpenQty)> GetOtherOpenLinesForContract(string contractNo, int excludeDocEntry, int excludeLineNum) => new List<(int, string, int, double)>();
            public List<(string BatchNum, double Qty)> GetAvailableBatches(string itemCode, string whsCode) => new List<(string, double)>();
            public DataTable SearchBaseDocGrpoDo(string docType, string docNum) => new DataTable();
            public List<string> GetCommodityList() => new List<string> { "CPO", "PK" };
            public DataTable GetReportData(string noTiket, string noPolisi, string commodity, string fromDate, string toDate) => new DataTable();
            public DataTable GetPrintTicketData(string ticketNumber) => new DataTable();
        }

        [TestMethod]
        public void AuthService_EmptyUsername_ReturnsErrorMessage()
        {
            var repo = new MockHanaRepository();
            var service = new AuthService(repo);

            var result = service.Authenticate("", "password123");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Silakan masukkan Username operator.", result.ErrorMessage);
        }

        [TestMethod]
        public void AuthService_EmptyPassword_ReturnsErrorMessage()
        {
            var repo = new MockHanaRepository();
            var service = new AuthService(repo);

            var result = service.Authenticate("admin", "");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Silakan masukkan Kata Sandi.", result.ErrorMessage);
        }

        [TestMethod]
        public void AuthService_ValidCredentials_ReturnsSuccessfulAuth()
        {
            var repo = new MockHanaRepository
            {
                AuthSuccess = true,
                MockFullName = "Budi Santoso",
                MockRole = "SUPERUSER"
            };
            var service = new AuthService(repo);

            var result = service.Authenticate("budi", "secret");

            Assert.IsTrue(result.Success);
            Assert.AreEqual("Budi Santoso", result.FullName);
            Assert.AreEqual("SUPERUSER", result.Role);
            Assert.IsTrue(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [TestMethod]
        public void AuthService_InvalidCredentials_ReturnsFailedAuth()
        {
            var repo = new MockHanaRepository
            {
                AuthSuccess = false,
                MockErrorMessage = "Kredensial tidak valid."
            };
            var service = new AuthService(repo);

            var result = service.Authenticate("unknown", "wrong");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Kredensial tidak valid.", result.ErrorMessage);
        }

        [TestMethod]
        public void ConfigurationValidator_ValidWbConfig_ReturnsOk()
        {
            var validator = new ConfigurationValidator();
            var result = validator.ValidateWbConfig("COM1", "9600", "8", "3000", "3000");

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("", result.ErrorMessage);
        }

        [TestMethod]
        public void ConfigurationValidator_EmptyPort_ReturnsFail()
        {
            var validator = new ConfigurationValidator();
            var result = validator.ValidateWbConfig("", "9600", "8", "3000", "3000");

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "Port Cannot be Empty");
        }

        [TestMethod]
        public void ConfigurationValidator_ValidSboConfig_ReturnsOk()
        {
            var validator = new ConfigurationValidator();
            var result = validator.ValidateSboConfig("HANA", "10.0.0.1:30015", "SBODB", "SYSTEM", "manager", "operator", "1234");

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("", result.ErrorMessage);
        }

        [TestMethod]
        public void ConfigurationValidator_MissingDatabase_ReturnsFail()
        {
            var validator = new ConfigurationValidator();
            var result = validator.ValidateSboConfig("HANA", "10.0.0.1:30015", "", "SYSTEM", "manager", "operator", "1234");

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains(result.ErrorMessage, "Database Cannot be Empty");
        }

        [TestMethod]
        public void ConfigXmlService_GenerateAndParseXml_RoundtripsAccurately()
        {
            var service = new ConfigXmlService();
            var wb = new WBConfig
            {
                portName = "COM3",
                baudRate = "19200",
                dataBits = "8",
                readTimeout = 5000,
                writeTimeout = 5000,
                prefix = "WB"
            };

            var sbo = new SBOConfig
            {
                serverType = "HANA",
                serverName = "hana-server:30015",
                database = "LIVE_DB",
                databaseUsername = "SYSTEM",
                databasePassword = "secretPassword123",
                sboUsername = "adminUser",
                sboPassword = "secretPassword456"
            };

            string xml = service.GenerateXml(wb, sbo);
            Assert.IsFalse(string.IsNullOrWhiteSpace(xml));

            var (parsedWb, parsedSbo) = service.ParseXml(xml);

            Assert.AreEqual("COM3", parsedWb.portName);
            Assert.AreEqual("19200", parsedWb.baudRate);
            Assert.AreEqual(5000, parsedWb.readTimeout);
            Assert.AreEqual("WB", parsedWb.prefix);

            Assert.AreEqual("HANA", parsedSbo.serverType);
            Assert.AreEqual("LIVE_DB", parsedSbo.database);
            Assert.AreEqual("SYSTEM", parsedSbo.databaseUsername);
            Assert.AreEqual("adminUser", parsedSbo.sboUsername);
        }

        [TestMethod]
        public void ExcelExportService_ExportDataTable_GeneratesValidExcelFile()
        {
            var service = new ExcelExportService();

            var dt = new DataTable("WeighbridgeReport");
            dt.Columns.Add("NoTiket", typeof(string));
            dt.Columns.Add("NoPolisi", typeof(string));
            dt.Columns.Add("Netto", typeof(double));
            dt.Columns.Add("Tanggal", typeof(DateTime));

            dt.Rows.Add("TK-1001", "BK 1234 AB", 15240.5, DateTime.Now);
            dt.Rows.Add("TK-1002", "BK 5678 CD", 18320.0, DateTime.Now);

            string tempFile = Path.Combine(Path.GetTempPath(), $"WB_Test_{Guid.NewGuid():N}.xlsx");
            try
            {
                service.ExportDataTableToExcel(dt, tempFile);

                Assert.IsTrue(File.Exists(tempFile));
                var fileInfo = new FileInfo(tempFile);
                Assert.IsTrue(fileInfo.Length > 0);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }
    }
}
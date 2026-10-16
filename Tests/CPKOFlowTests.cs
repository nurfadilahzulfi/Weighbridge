using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class CPKOFlowTests
    {
        private class MockHanaRepoForCPKO : IHanaRepository
        {
            public (bool Success, string FullName, string Role, string ErrorMessage) AuthenticateUser(string u, string p) => (true, "Op", "SUPERUSER", "");
            public string GenerateTiketNumber() => "TK-CPKO-001";
            public string GetNextWBCode() => "WB-CPKO-001";
            public List<string> GetKodeDokumenPabean() => new List<string> { "0407613 - KEK - Pemasukan dari TLDDP" };
            public DataTable GetTransporterInternal() => new DataTable();
            public DataTable SearchBaseDocument(string docType, string vendorRef)
            {
                var dt = new DataTable();
                dt.Columns.Add("DOCENTRY", typeof(int));
                dt.Columns.Add("DOCNUM", typeof(string));
                dt.Columns.Add("VENDORREF", typeof(string));
                dt.Columns.Add("KONTRAK", typeof(string));
                dt.Columns.Add("ITEMCODE", typeof(string));
                dt.Columns.Add("LINENUM", typeof(int));
                dt.Rows.Add(1047, "261000080", "899/TRI/CPKO/IX/2026", "", "CRUDE PALM KERNEL OIL-CPKO", 0);
                return dt;
            }
            public DocQuotaInfo GetDocQuotaInfo(string docType, int docEntry, int lineNum) => new DocQuotaInfo { HeaderStatus = "O", LineStatus = "O", OpenQty = 450000, TotalQty = 450000 };
            public (double openQty, double totalQty) GetFreshQuota(string docType, int docEntry, int lineNum) => (450000, 450000);
            public string GetFreshKontrakNo(string docType, int docEntry) => "";
            public PabeanData GetPabeanData(string docType, int docEntry) => new PabeanData { KodeDokumen = "0407613", NomorDokumen = "787498739", TanggalDokumen = DateTime.Today, StatusJalur = "HIJAU", FasilitasPabean = "FASILITAS" };
            public List<DocumentLookupItem> GetOpenDocuments(string docType) => new List<DocumentLookupItem>
            {
                new DocumentLookupItem { DocEntry = 1047, DocNum = "261000080", NumAtCard = "899/TRI/CPKO/IX/2026", ItemCode = "11000002", ItemName = "CRUDE PALM KERNEL OIL-CPKO", OpenQty = 450000, Quantity = 450000, ContractNo = "" }
            };
            public List<PendingTicketInfo> GetPendingTickets(string docType, string searchKey) => new List<PendingTicketInfo>();
            public string GetWBCodeByTiket(string tiketNum) => "WB-CPKO-001";
            public System.Collections.Generic.List<string> GetWBCodesByTiket(string tiketNum) => new System.Collections.Generic.List<string> { "WB-CPKO-001" };
            public (string isIns, int baseType, int docEntry, int lineNum) SearchPOSOReserve(string docType, int docEntrySO) => ("N", 0, 0, 0);
            public string GetDocNumByEntry(string tableName, string docEntry) => "261000080";
            public string GetPostedDocNumsForTicket(string tableName, string tiketNum) => "";
            public List<string> GetNomorTangki(string itemName) => new List<string> { "2112", "2113", "2114" };
            public DataTable SearchBaseDocJoinWbData(string docType, string docNum) => new DataTable();
            public DataTable SearchTicketByTiketNum(string noTiket) => new DataTable();
            public List<TicketLookupItem> GetPendingTicketLookupList() => new List<TicketLookupItem>();
            public (string itemCode, string whsCode) GetItemAndWhsCodeFromSO(int docEntry, int lineNum) => ("", "");
            public (string cardCode, string itemCode) GetPOVendorAndItem(int docEntry, int lineNum) => ("", "");
            public List<(int DocEntry, string DocNum, int LineNum, double OpenQty)> GetOtherOpenLinesForContract(string contractNo, int excludeDocEntry, int excludeLineNum) => new List<(int, string, int, double)>();
            public List<(string BatchNum, double Qty)> GetAvailableBatches(string itemCode, string whsCode) => new List<(string, double)>();
            public DataTable SearchBaseDocGrpoDo(string docType, string docNum) => new DataTable();
            public List<string> GetCommodityList() => new List<string> { "CRUDE PALM KERNEL OIL-CPKO" };
            public DataTable GetReportData(string noTiket, string noPolisi, string commodity, string fromDate, string toDate) => new DataTable();
            public DataTable GetPrintTicketData(string ticketNumber) => new DataTable();
        }

        private class MockSapRepoForCPKO : ISapRepository
        {
            public WeighInData LastSavedWeighIn { get; private set; }
            public WeighOutData LastSavedGRPO { get; private set; }

            public bool SaveWeighInUDO(WeighInData data, out string error)
            {
                error = "";
                LastSavedWeighIn = data;
                return true;
            }

            public WeighOutData LastUpdatedWeighOut { get; private set; }

            public bool UpdateWeighOutUDO(WeighOutData data, out string error)
            {
                error = "";
                LastUpdatedWeighOut = data;
                return true;
            }

            public bool UpdateWeighOutUDO(string code, double beratKeluar, double beratNetto, string status, string weighout, string tangkiNo, out string error)
            {
                error = "";
                return true;
            }

            public bool CreateGRPO(WeighOutData data, out int createdDocEntry, out string error)
            {
                error = "";
                createdDocEntry = 9999;
                LastSavedGRPO = data;
                return true;
            }

            public bool CreateSplitGRPO(System.Collections.Generic.IList<SplitGRPOPart> parts, out System.Collections.Generic.List<int> docEntries, out string error)
            {
                error = "";
                docEntries = new System.Collections.Generic.List<int>();
                for (int i = 0; i < parts.Count; i++) docEntries.Add(1001 + i);
                return true;
            }

            public bool CreateDeliveryOrder(WeighOutData data, string itemCode, string whsCode, out int createdDocEntry, out string error)
            {
                error = "";
                createdDocEntry = 8888;
                return true;
            }

            public bool SetUDOReprintFlag(string code, out string error)
            {
                error = "";
                return true;
            }

            public bool UpdateGrpoLineWeight(int docEntry, int lineNum, double weight, out string error)
            {
                error = "";
                return true;
            }

            public bool UpdateDoLineWeight(int docEntry, int lineNum, double weight, out string error)
            {
                error = "";
                return true;
            }
        }

        [TestMethod]
        public void CPKO_WeighIn_SucceedsWithNullOrEmptyContract()
        {
            var hana = new MockHanaRepoForCPKO();
            var sap = new MockSapRepoForCPKO();
            var service = new WeighInService(hana, sap);

            var data = new WeighInData
            {
                DocType = "PO",
                DocNum = "261000080",
                VendorRef = "899/TRI/CPKO/IX/2026",
                DocEntry = 1047,
                LineNum = 0,
                TiketNum = "TK-CPKO-001",
                Commodity = "CRUDE PALM KERNEL OIL-CPKO",
                NoKontrak = null,
                Nopol = "BK 1234 XY",
                NamaSupir = "Budi",
                BeratMasuk = 25000,
                NomorSpb = null,
                NomorAju = null
            };

            bool ok = service.SaveWeighIn(data, out string err);
            Assert.IsTrue(ok);
            Assert.AreEqual("TK-CPKO-001", sap.LastSavedWeighIn.TiketNum);
            Assert.AreEqual("WB-CPKO-001", sap.LastSavedWeighIn.Code);
        }

        [TestMethod]
        public void CPKO_WeighOut_SyncGRPO_SucceedsWithTangki()
        {
            var hana = new MockHanaRepoForCPKO();
            var sap = new MockSapRepoForCPKO();
            var service = new WeighOutService(hana, sap);

            var tangkis = hana.GetNomorTangki("CRUDE PALM KERNEL OIL-CPKO");
            Assert.IsTrue(tangkis.Contains("2112"));

            var data = new WeighOutData
            {
                TiketNum = "TK-CPKO-001",
                Code = "WB-CPKO-001",
                DocType = "PO",
                DocNum = "261000080",
                DocEntry = 1047,
                LineNum = 0,
                NoKontrak = "",
                TangkiNo = tangkis[0],
                BeratMasuk = 25000,
                BeratKeluar = 10000,
                BeratNetto = 15000,
                NomorSpb = "",
                NomorAju = ""
            };

            bool ok = service.SyncGRPO(data, out int docEntry, out string err);
            Assert.IsTrue(ok);
            Assert.AreEqual(9999, docEntry);
            Assert.AreEqual("2112", sap.LastSavedGRPO.TangkiNo);
        }

        [TestMethod]
        public void CPKO_DocumentLookup_MatchesByDocNumOrCommodity()
        {
            var hana = new MockHanaRepoForCPKO();
            var lookup = new DocumentLookupService(hana);

            var docs = lookup.GetOpenDocuments("PO");
            var byDocNum = DocumentLookupFilter.Filter(docs, "261000080");
            Assert.AreEqual(1, byDocNum.Count);

            var byCPKO = DocumentLookupFilter.Filter(docs, "CPKO");
            Assert.AreEqual(1, byCPKO.Count);

            var byVendorRef = DocumentLookupFilter.Filter(docs, "899/TRI");
            Assert.AreEqual(1, byVendorRef.Count);
        }

        [TestMethod]
        public void CPKO_SafeDataConversion_HandlesEmptyValuesWithoutThrowing()
        {
            Assert.AreEqual(0.0, DataConversionHelper.SafeToDouble(""));
            Assert.AreEqual(0.0, DataConversionHelper.SafeToDouble(null));
            Assert.AreEqual(0.0, DataConversionHelper.SafeToDouble(DBNull.Value));
            Assert.AreEqual(15000.0, DataConversionHelper.SafeToDouble("15000"));
            Assert.AreEqual(15000.5, DataConversionHelper.SafeToDouble("15000.5"));
        }

        [TestMethod]
        public void WeighOut_SyncGRPO_UpdatesLabResultsToUDO()
        {
            var hana = new MockHanaRepoForCPKO();
            var sap = new MockSapRepoForCPKO();
            var service = new WeighOutService(hana, sap);

            var data = new WeighOutData
            {
                TiketNum = "TK-CPKO-001",
                Code = "WB-CPKO-001",
                DocType = "PO",
                DocNum = "261000080",
                DocEntry = 1047,
                LineNum = 0,
                TangkiNo = "2112",
                BeratMasuk = 25000,
                BeratKeluar = 10000,
                BeratNetto = 15000,
                LabFfa = "1.25",
                LabMoist = "0.15",
                LabImp = "0.02",
                LabDobi = "2.8",
                LabCarotine = "500",
                Note = "Catatan timbang keluar"
            };

            bool ok = service.SyncGRPO(data, out int docEntry, out string err);
            Assert.IsTrue(ok);
            Assert.IsNotNull(sap.LastUpdatedWeighOut);
            Assert.AreEqual("1.25", sap.LastUpdatedWeighOut.LabFfa);
            Assert.AreEqual("0.15", sap.LastUpdatedWeighOut.LabMoist);
            Assert.AreEqual("0.02", sap.LastUpdatedWeighOut.LabImp);
            Assert.AreEqual("2.8", sap.LastUpdatedWeighOut.LabDobi);
            Assert.AreEqual("500", sap.LastUpdatedWeighOut.LabCarotine);
        }
    }
}

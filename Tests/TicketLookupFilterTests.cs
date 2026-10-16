using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class TicketLookupFilterTests
    {
        private List<TicketLookupItem> GetSampleTickets()
        {
            return new List<TicketLookupItem>
            {
                new TicketLookupItem
                {
                    TiketNum = "WB00430",
                    Status = "WEIGHIN",
                    Nopol = "BK 1234 AB",
                    NamaSupir = "Budi",
                    DocType = "PO",
                    DocNum = "261000075",
                    ContractNo = "KTR-2026-001",
                    Commodity = "CPO"
                },
                new TicketLookupItem
                {
                    TiketNum = "WB00431",
                    Status = "SYNCSAP",
                    Nopol = "B 9999 CD",
                    NamaSupir = "Santoso",
                    DocType = "PO",
                    DocNum = "261000076",
                    ContractNo = "KTR-2026-002",
                    Commodity = "PK"
                },
                new TicketLookupItem
                {
                    TiketNum = "AS2609240001",
                    Status = "WEIGHIN",
                    Nopol = "BK 5678 EF",
                    NamaSupir = "Joko",
                    DocType = "SO",
                    DocNum = "262000159",
                    ContractNo = "",
                    Commodity = "CPO"
                }
            };
        }

        [TestMethod]
        public void Filter_ByTicketNumberDigits_Matches()
        {
            var list = GetSampleTickets();
            var results = TicketLookupFilter.Filter(list, "430");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("WB00430", results[0].TiketNum);
        }

        [TestMethod]
        public void Filter_ByNopol_Matches()
        {
            var list = GetSampleTickets();
            var results = TicketLookupFilter.Filter(list, "1234");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("WB00430", results[0].TiketNum);
        }

        [TestMethod]
        public void Filter_BySupir_Matches()
        {
            var list = GetSampleTickets();
            var results = TicketLookupFilter.Filter(list, "joko");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("AS2609240001", results[0].TiketNum);
        }

        [TestMethod]
        public void Filter_ByContract_Matches()
        {
            var list = GetSampleTickets();
            var results = TicketLookupFilter.Filter(list, "KTR-2026-002");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("WB00431", results[0].TiketNum);
        }

        [TestMethod]
        public void Filter_PrioritizesWeighInOverSyncSap()
        {
            var list = GetSampleTickets();
            var results = TicketLookupFilter.Filter(list, "WB");
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("WB00430", results[0].TiketNum, "WEIGHIN harus muncul lebih dulu dari SYNCSAP");
            Assert.AreEqual("WB00431", results[1].TiketNum);
        }

        [TestMethod]
        public void Filter_EmptyOrNullQuery_ReturnsEmpty()
        {
            var list = GetSampleTickets();
            Assert.AreEqual(0, TicketLookupFilter.Filter(list, null).Count);
            Assert.AreEqual(0, TicketLookupFilter.Filter(list, "").Count);
            Assert.AreEqual(0, TicketLookupFilter.Filter(list, "   ").Count);
            Assert.AreEqual(0, TicketLookupFilter.Filter(null, "430").Count);
        }
    }
}

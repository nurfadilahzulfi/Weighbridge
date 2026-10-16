using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class DocumentLookupFilterTests
    {
        private List<DocumentLookupItem> CreateSampleDocuments()
        {
            return new List<DocumentLookupItem>
            {
                new DocumentLookupItem
                {
                    DocEntry = 101,
                    DocNum = "2400021",
                    NumAtCard = "SJ-2024-001",
                    CardCode = "V001",
                    CardName = "PT SUMATRA SAWIT MAKMUR",
                    ItemCode = "TBS-01",
                    ItemName = "Tandan Buah Segar",
                    OpenQty = 25000,
                    Quantity = 50000,
                    DocStatus = "O",
                    LineStatus = "O"
                },
                new DocumentLookupItem
                {
                    DocEntry = 102,
                    DocNum = "2400022",
                    NumAtCard = "SJ-021-X",
                    CardCode = "V002",
                    CardName = "PT KELAPA SAWIT NUSANTARA",
                    ItemCode = "CPO-01",
                    ItemName = "Crude Palm Oil",
                    OpenQty = 0, // Kuota habis!
                    Quantity = 30000,
                    DocStatus = "O",
                    LineStatus = "O"
                },
                new DocumentLookupItem
                {
                    DocEntry = 103,
                    DocNum = "2400099",
                    NumAtCard = "REF-99",
                    CardCode = "V003",
                    CardName = "CV ANUGERAH 021 LESTARI",
                    ItemCode = "PK-01",
                    ItemName = "Palm Kernel",
                    OpenQty = 15000,
                    Quantity = 15000,
                    DocStatus = "O",
                    LineStatus = "O"
                }
            };
        }

        [TestMethod]
        public void Filter_WithMatchingDocNum_ReturnsMatchingDocument()
        {
            var docs = CreateSampleDocuments();
            var results = DocumentLookupFilter.Filter(docs, "021");

            // Harus menemukan dokumen yang mengandung "021":
            // 1. DocNum "2400021"
            // 2. NumAtCard "SJ-021-X"
            // 3. CardName "CV ANUGERAH 021 LESTARI"
            Assert.AreEqual(3, results.Count);
        }

        [TestMethod]
        public void Filter_CaseInsensitive_ReturnsMatches()
        {
            var docs = CreateSampleDocuments();
            var results = DocumentLookupFilter.Filter(docs, "sumatra");

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("2400021", results[0].DocNum);
        }

        [TestMethod]
        public void Filter_EmptyOrNullQuery_ReturnsEmptyList()
        {
            var docs = CreateSampleDocuments();
            var resultNull = DocumentLookupFilter.Filter(docs, null);
            var resultEmpty = DocumentLookupFilter.Filter(docs, "");
            var resultSpaces = DocumentLookupFilter.Filter(docs, "   ");

            Assert.AreEqual(0, resultNull.Count);
            Assert.AreEqual(0, resultEmpty.Count);
            Assert.AreEqual(0, resultSpaces.Count);
        }

        [TestMethod]
        public void Filter_NullSourceList_ReturnsEmptyList()
        {
            var results = DocumentLookupFilter.Filter(null, "021");
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Filter_MaxResultsLimit_RestrictsReturnedCount()
        {
            var docs = new List<DocumentLookupItem>();
            for (int i = 0; i < 30; i++)
            {
                docs.Add(new DocumentLookupItem
                {
                    DocNum = $"PO-{i:D3}-021",
                    CardName = $"Vendor {i}",
                    OpenQty = 1000,
                    Quantity = 1000
                });
            }

            var results = DocumentLookupFilter.Filter(docs, "021", maxResults: 10);
            Assert.AreEqual(10, results.Count);
        }

        [TestMethod]
        public void CanProceedWithQuota_SufficientQuota_ReturnsTrue()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "2400021",
                CardName = "PT SUMATRA SAWIT MAKMUR",
                OpenQty = 60000, // Di atas threshold minimum 55.000 kg
                Quantity = 100000
            };

            bool canProceed = DocumentLookupFilter.CanProceedWithQuota(item, out string reason);

            Assert.IsTrue(canProceed);
            Assert.IsTrue(string.IsNullOrEmpty(reason));
        }

        [TestMethod]
        public void CanProceedWithQuota_BelowMinimumQuota_NowAllowedUnderNewFlow()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "2400021-B",
                CardName = "PT SUMATRA SAWIT MAKMUR",
                OpenQty = 54999, // Di bawah batas minimum 55.000 kg
                Quantity = 100000
            };

            bool canProceed = DocumentLookupFilter.CanProceedWithQuota(item, out string reason);

            Assert.IsTrue(canProceed, "Batas minimum kuota telah dihapus, dokumen harus diizinkan.");
            Assert.IsTrue(string.IsNullOrEmpty(reason));
        }

        [TestMethod]
        public void CanProceedWithQuota_ExactlyMinimumQuota_ReturnsTrue()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "2400021-C",
                CardName = "PT SUMATRA SAWIT MAKMUR",
                OpenQty = 55000, // Tepat batas minimum 55.000 kg
                Quantity = 100000
            };

            bool canProceed = DocumentLookupFilter.CanProceedWithQuota(item, out string reason);

            Assert.IsTrue(canProceed, "OpenQty == 55.000 kg harus diizinkan.");
            Assert.IsTrue(string.IsNullOrEmpty(reason));
        }

        [TestMethod]
        public void CanProceedWithQuota_ZeroThresholdForNonPO_AllowsSmallerPositiveQuota()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "SO-240001",
                CardName = "PT BUYER SAWIT",
                OpenQty = 25000,
                Quantity = 50000
            };

            // Untuk dokumen non-PO (misal SO), minimum threshold adalah 0
            bool canProceed = DocumentLookupFilter.CanProceedWithQuota(item, out string reason, 0);

            Assert.IsTrue(canProceed, "Dokumen non-PO dengan sisa kuota positif harus diizinkan jika threshold = 0.");
            Assert.IsTrue(string.IsNullOrEmpty(reason));
        }

        [TestMethod]
        public void CanProceedWithQuota_ZeroRemainingQuota_NowAllowedUnderNewFlow()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "2400022",
                CardName = "PT KELAPA SAWIT NUSANTARA",
                OpenQty = 0, // Kuota habis
                Quantity = 30000
            };

            bool canProceed = DocumentLookupFilter.CanProceedWithQuota(item, out string reason);

            Assert.IsTrue(canProceed, "Validasi kuota habis telah dihapus sesuai alur baru.");
            Assert.IsTrue(string.IsNullOrEmpty(reason));
        }

        [TestMethod]
        public void CanProceedWithQuota_NegativeRemainingQuota_NowAllowedUnderNewFlow()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "2400023",
                CardName = "PT OVER DELIVERED",
                OpenQty = -500, // Kuota minus (over-delivery)
                Quantity = 20000
            };

            bool canProceed = DocumentLookupFilter.CanProceedWithQuota(item, out string reason);

            Assert.IsTrue(canProceed, "Validasi kuota minus telah dihapus sesuai alur baru.");
            Assert.IsTrue(string.IsNullOrEmpty(reason));
        }

        [TestMethod]
        public void CanProceedWithQuota_NullItem_ReturnsFalse()
        {
            bool canProceed = DocumentLookupFilter.CanProceedWithQuota(null, out string reason);

            Assert.IsFalse(canProceed);
            Assert.AreEqual("Dokumen tidak valid.", reason);
        }

        [TestMethod]
        public void DisplayText_ShowsExhaustedWarningWhenOpenQtyIsZero()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "2400022",
                CardName = "PT TEST",
                OpenQty = 0,
                Quantity = 10000
            };

            string text = item.DisplayText;
            Assert.IsTrue(text.Contains("[KUOTA HABIS / 0 kg"));
        }

        [TestMethod]
        public void DisplayText_ShowsRemainingQuotaWhenOpenQtyIsPositive()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "2400021",
                CardName = "PT TEST",
                OpenQty = 12500,
                Quantity = 20000
            };

            string text = item.DisplayText;
            Assert.IsTrue(text.Contains("Sisa: 12,500 kg") || text.Contains("12.500"));
        }

        [TestMethod]
        public void DisplayText_ShowsTotalQuotaAndLineNumber()
        {
            var item = new DocumentLookupItem
            {
                DocNum = "262000160",
                LineNum = 0,
                CardName = "PT TEST",
                OpenQty = 25000,
                Quantity = 50000
            };

            string text = item.DisplayText;
            Assert.IsTrue(text.Contains("Line 1"), "Harus menampilkan nomor baris Line 1.");
            Assert.IsTrue(text.Contains("Kuota: 50,000 kg") || text.Contains("50.000"), "Harus menampilkan total kuota.");
            Assert.IsTrue(text.Contains("Sisa: 25,000 kg") || text.Contains("25.000"), "Harus menampilkan sisa kuota.");
        }
    }
}

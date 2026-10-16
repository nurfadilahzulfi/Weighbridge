using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    // Perbedaan SP TANIMASLIVE: kontrak kosong dikirim "0", pencarian dokumen hanya Vendor Ref atau hanya No. PO
    [TestClass]
    public class GoLiveCompatibilityTests
    {
        [TestMethod]
        public void Kontrak_NolDariSPLive_DianggapKosong()
        {
            Assert.AreEqual("", DataConversionHelper.NormalizeKontrak("0"));
            Assert.AreEqual("", DataConversionHelper.NormalizeKontrak(" 0 "));
            Assert.AreEqual("", DataConversionHelper.NormalizeKontrak(null));
            Assert.AreEqual("", DataConversionHelper.NormalizeKontrak(System.DBNull.Value));
            Assert.AreEqual("208/AJ/PERJ/CPO/IX/2026", DataConversionHelper.NormalizeKontrak("208/AJ/PERJ/CPO/IX/2026"));
        }

        private static List<DocumentLookupItem> OpenDocs()
        {
            return new List<DocumentLookupItem>
            {
                new DocumentLookupItem { DocEntry = 1, DocNum = "261000150", NumAtCard = "0023/ANS-TRI/CPO/VIII/2026", LineNum = 0, OpenQty = 0 },
                new DocumentLookupItem { DocEntry = 1, DocNum = "261000150", NumAtCard = "0023/ANS-TRI/CPO/VIII/2026", LineNum = 1, OpenQty = 560 },
                new DocumentLookupItem { DocEntry = 2, DocNum = "261000164", NumAtCard = "0024/ANS-TRI/CPO/VIII/2026", LineNum = 0, OpenQty = 130 }
            };
        }

        [TestMethod]
        public void CariDokumen_NoPO_KetemuBarisYangMasihAdaSisa()
        {
            var item = DocumentLookupFilter.FindExact(OpenDocs(), "261000150");

            Assert.IsNotNull(item);
            Assert.AreEqual(1, item.LineNum);
        }

        [TestMethod]
        public void CariDokumen_VendorRef_KetemuNoPO()
        {
            var item = DocumentLookupFilter.FindExact(OpenDocs(), " 0024/ans-tri/cpo/viii/2026 ");

            Assert.IsNotNull(item);
            Assert.AreEqual("261000164", item.DocNum);
        }

        [TestMethod]
        public void CariDokumen_SebagianTeks_TidakDianggapSama()
        {
            Assert.IsNull(DocumentLookupFilter.FindExact(OpenDocs(), "26100015"));
            Assert.IsNull(DocumentLookupFilter.FindExact(OpenDocs(), ""));
        }
    }
}

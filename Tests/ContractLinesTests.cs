using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>Contract No / Contract Qty di slip: tiket split mencetak dua kontrak, K1 lebih dulu.</summary>
    [TestClass]
    public class ContractLinesTests
    {
        private static DataTable PrintRows()
        {
            var dt = new DataTable();
            dt.Columns.Add("Contract No", typeof(int));
            dt.Columns.Add("Contract Qty", typeof(decimal));
            return dt;
        }

        [TestMethod]
        public void TiketSplit_DataTestingAS2610140001_DuaKontrakK1Dulu()
        {
            var dt = PrintRows();
            dt.Rows.Add(262000165, 135000m);   // urutan dari SP tidak dijamin
            dt.Rows.Add(262000164, 75000m);

            PrintService.BuildContractLines(dt, "262000164", out List<string> nos, out List<string> qtys);

            CollectionAssert.AreEqual(new[] { "262000164", "262000165" }, nos);
            CollectionAssert.AreEqual(new[] { "75,000", "135,000" }, qtys);
        }

        [TestMethod]
        public void TiketBiasa_SatuKontrak()
        {
            var dt = PrintRows();
            dt.Rows.Add(262000164, 75000m);

            PrintService.BuildContractLines(dt, (string)null, out List<string> nos, out List<string> qtys);

            CollectionAssert.AreEqual(new[] { "262000164" }, nos);
            CollectionAssert.AreEqual(new[] { "75,000" }, qtys);
        }

        [TestMethod]
        public void ContractNoNol_Dilewati()
        {
            // SP production mengirim 0 pada tahap cetak tertentu
            var dt = PrintRows();
            dt.Rows.Add(0, 0m);

            PrintService.BuildContractLines(dt, (string)null, out List<string> nos, out List<string> qtys);

            Assert.AreEqual(0, nos.Count);
            Assert.AreEqual("", PrintService.JoinContractValues(nos));
        }

        [TestMethod]
        public void JoinContractValues_SatuBarisDenganGarisMiring()
        {
            Assert.AreEqual("262000164 / 262000165", PrintService.JoinContractValues(new[] { "262000164", "262000165" }));
            Assert.AreEqual("75,000 / 135,000", PrintService.JoinContractValues(new[] { "75,000", "135,000" }));
            Assert.AreEqual("75,000", PrintService.JoinContractValues(new[] { "75,000" }));
        }

        private static DataTable ConsolidatedRow()
        {
            var dt = new DataTable();
            dt.Columns.Add("Sold to", typeof(string));
            dt.Columns.Add("Note", typeof(string));
            dt.Columns.Add("Contract No", typeof(int));
            dt.Columns.Add("Contract Qty", typeof(decimal));
            dt.Rows.Add("", "REF1: 600 KG SPLIT REF2: 19,920 KG", 262000164, 210000m);
            return dt;
        }

        [TestMethod]
        public void SplitContractText_FieldAngkaLamaDibuatNol()
        {
            // Rumus visibilitas report ({Contract No} = 0) yang menyembunyikan field angka lama
            var dt = ConsolidatedRow();
            PrintService.ApplySplitContractText(dt, new[] { "262000164", "262000165" }, new[] { "75,000", "135,000" }, masukSlip: false);

            Assert.AreEqual(0, dt.Rows[0]["Contract No"]);
            Assert.AreEqual(0m, dt.Rows[0]["Contract Qty"]);
        }

        [TestMethod]
        public void SplitContractText_SlipMasuk_SoldToKontrakNoteQty()
        {
            var dt = ConsolidatedRow();
            bool applied = PrintService.ApplySplitContractText(dt, new[] { "262000164", "262000165" }, new[] { "75,000", "135,000" }, masukSlip: true);

            Assert.IsTrue(applied);
            Assert.AreEqual("262000164 / 262000165", dt.Rows[0]["Sold to"]);
            Assert.AreEqual("75,000 / 135,000", dt.Rows[0]["Note"]);
        }

        [TestMethod]
        public void SplitContractText_Reprint_NoteTetapQtyIkutSoldTo()
        {
            var dt = ConsolidatedRow();
            bool applied = PrintService.ApplySplitContractText(dt, new[] { "262000164", "262000165" }, new[] { "75,000", "135,000" }, masukSlip: false);

            Assert.IsTrue(applied);
            Assert.AreEqual("262000164 / 262000165\r\n\r\n75,000 / 135,000", dt.Rows[0]["Sold to"]);
            Assert.AreEqual("REF1: 600 KG SPLIT REF2: 19,920 KG", dt.Rows[0]["Note"]);
        }

        [TestMethod]
        public void SplitContractText_TiketBiasa_TidakDiubah()
        {
            var dt = ConsolidatedRow();
            bool applied = PrintService.ApplySplitContractText(dt, new[] { "262000164" }, new[] { "75,000" }, masukSlip: true);

            Assert.IsFalse(applied);
            Assert.AreEqual("", dt.Rows[0]["Sold to"]);
        }
    }
}

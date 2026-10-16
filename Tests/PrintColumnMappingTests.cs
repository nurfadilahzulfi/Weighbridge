using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    // Kartu timbang: Net Weight = netto WB pabrik, Mills Net Weight = netto SPB kebun, Difference = WB - SPB.
    [TestClass]
    public class PrintColumnMappingTests
    {
        private static DataTable CreateTable()
        {
            var dt = new DataTable("SOL_SP_ADDON_WB_PRINT_TIKET");
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Contract No", typeof(string));
            dt.Columns.Add("First Weight", typeof(string));
            dt.Columns.Add("Second Weight", typeof(string));
            dt.Columns.Add("Net Weight", typeof(string));
            dt.Columns.Add("Mills Net Weight", typeof(string));
            dt.Columns.Add("Difference", typeof(string));
            dt.Columns.Add("Note", typeof(string));
            return dt;
        }

        [TestMethod]
        public void Consolidate_PO_NetWeightIsWBAndMillsIsSPB()
        {
            // Data SP: WB pabrik 12.130, SPB kebun 12.100
            var dt = CreateTable();
            dt.Rows.Add("PO", "261000312", "17000 kg", "4870 kg", "12130 kg", "12100 kg", "30 kg", "");

            var result = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual("12130 kg", result.Rows[0]["Net Weight"]);
            Assert.AreEqual("12100 kg", result.Rows[0]["Mills Net Weight"]);
            Assert.AreEqual("30 kg", result.Rows[0]["Difference"]);
        }

        [TestMethod]
        public void Consolidate_SO_Unchanged()
        {
            var dt = CreateTable();
            dt.Rows.Add("SO", "", "12000 kg", "40000 kg", "28000 kg", "", "", "");

            var result = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual("28000 kg", result.Rows[0]["Net Weight"]);
        }

        [TestMethod]
        public void Consolidate_SplitPO_TotalsNotSwapped()
        {
            // Split K1/K2 dalam satu tiket: netto WB per kontrak + netto SPB per kontrak
            var dt = CreateTable();
            dt.Rows.Add("PO", "261000302", "40410 kg", "11680 kg", "4140 kg", "4140 kg", "0 kg", "");
            dt.Rows.Add("PO", "261000317", "40410 kg", "11680 kg", "24590 kg", "24610 kg", "-20 kg", "");

            var result = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual(1, result.Rows.Count);
            Assert.AreEqual("28730 kg", result.Rows[0]["Net Weight"]);        // total WB pabrik
            Assert.AreEqual("28750 kg", result.Rows[0]["Mills Net Weight"]);  // total SPB kebun
            Assert.AreEqual("-20 kg", result.Rows[0]["Difference"]);          // WB pabrik - SPB kebun
        }

        // SP cetak LIVE mengosongkan header untuk record SYNCSAP yang flag REPRINT-nya masih N.
        // Kalau record itu kebetulan baris pertama, header kartu reprint split tidak boleh ikut kosong.
        [TestMethod]
        public void Consolidate_SplitReprint_HeaderTakenFromFilledRecord()
        {
            var dt = CreateTable();
            dt.Columns.Add("Nomor Tiket", typeof(string));
            dt.Columns.Add("Vehicle No", typeof(string));
            var r1 = dt.Rows.Add("PO", "0", "", "11680 kg", "4140 kg", "4140 kg", "0 kg", "");
            r1["Nomor Tiket"] = ""; r1["Vehicle No"] = "";
            var r2 = dt.Rows.Add("PO", "261000317", "40410 kg", "11680 kg", "24590 kg", "24610 kg", "-20 kg", "");
            r2["Nomor Tiket"] = "AS2610030001"; r2["Vehicle No"] = "BK 8374 GC";

            var result = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual("AS2610030001", result.Rows[0]["Nomor Tiket"]);
            Assert.AreEqual("BK 8374 GC", result.Rows[0]["Vehicle No"]);
            Assert.AreEqual("261000317", result.Rows[0]["Contract No"]);
            Assert.AreEqual("40410 kg", result.Rows[0]["First Weight"]);
            Assert.AreEqual("28730 kg", result.Rows[0]["Net Weight"]);
            Assert.AreEqual("28750 kg", result.Rows[0]["Mills Net Weight"]);
        }

        // Cetak timbang keluar biasa: semua record header-nya kosong (kartu pre-print) -> tetap kosong
        [TestMethod]
        public void Consolidate_WeighOutPrint_HeaderStaysBlank()
        {
            var dt = CreateTable();
            dt.Columns.Add("Nomor Tiket", typeof(string));
            var r1 = dt.Rows.Add("PO", "0", "", "11680 kg", "4140 kg", "4140 kg", "0 kg", ""); r1["Nomor Tiket"] = "";
            var r2 = dt.Rows.Add("PO", "0", "", "11680 kg", "24590 kg", "24610 kg", "-20 kg", ""); r2["Nomor Tiket"] = "";

            var result = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual("", result.Rows[0]["Nomor Tiket"]);
            Assert.AreEqual("28730 kg", result.Rows[0]["Net Weight"]);
        }
    }
}

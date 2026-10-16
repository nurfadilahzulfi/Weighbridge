using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Slip masuk dan keluar dicetak di kertas yang sama; Note hanya dicetak di slip keluar
    /// (atau reprint tiket yang sudah selesai) karena qty asli baru final saat timbang keluar.
    /// </summary>
    [TestClass]
    public class PrintSlipNoteTests
    {
        [TestMethod]
        public void CetakTimbangMasuk_AdalahSlipMasuk_NoteTidakDicetak()
        {
            Assert.IsTrue(PrintService.IsMasukSlip(TicketPrintType.Masuk, isMasukOnly: true));
        }

        [TestMethod]
        public void CetakTimbangKeluar_BukanSlipMasuk_NoteDicetak()
        {
            Assert.IsFalse(PrintService.IsMasukSlip(TicketPrintType.Keluar, isMasukOnly: false));
        }

        [TestMethod]
        public void ReprintTiketBelumKeluar_AdalahSlipMasuk_NoteTidakDicetak()
        {
            Assert.IsTrue(PrintService.IsMasukSlip(TicketPrintType.Reprint, isMasukOnly: true));
        }

        [TestMethod]
        public void ReprintTiketSelesai_BukanSlipMasuk_NoteDicetak()
        {
            Assert.IsFalse(PrintService.IsMasukSlip(TicketPrintType.Reprint, isMasukOnly: false));
        }

        private static System.Data.DataTable PrintRows(params string[] notes)
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("Note", typeof(string));
            foreach (var n in notes) dt.Rows.Add(n);
            return dt;
        }

        private static System.Data.DataTable WbRows(params string[] notes)
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("U_SOL_NOTE", typeof(string));
            foreach (var n in notes) dt.Rows.Add(n);
            return dt;
        }

        [TestMethod]
        public void NoteFallback_SPMengirimKosong_DiisiDariRecordWB()
        {
            // SP production mengosongkan Note saat cetak timbang keluar
            var print = PrintRows("");
            PrintService.ApplyNoteFallback(print, WbRows("TRUK PERTAMA HARI INI"));
            Assert.AreEqual("TRUK PERTAMA HARI INI", print.Rows[0]["Note"]);
        }

        [TestMethod]
        public void NoteFallback_SPSudahMengirimNote_TidakDiubah()
        {
            var print = PrintRows("NOTE DARI SP");
            PrintService.ApplyNoteFallback(print, WbRows("NOTE LAIN"));
            Assert.AreEqual("NOTE DARI SP", print.Rows[0]["Note"]);
        }

        [TestMethod]
        public void NoteFallback_TiketSplit_SemuaRecordKosongDiisi()
        {
            var print = PrintRows("", "");
            PrintService.ApplyNoteFallback(print, WbRows("", "REF1: 12,000 KG SPLIT REF2: 16,000 KG"));
            Assert.AreEqual("REF1: 12,000 KG SPLIT REF2: 16,000 KG", print.Rows[0]["Note"]);
            Assert.AreEqual("REF1: 12,000 KG SPLIT REF2: 16,000 KG", print.Rows[1]["Note"]);
        }

        [TestMethod]
        public void NoteFallback_RecordWBTanpaNote_TetapKosong()
        {
            var print = PrintRows("");
            PrintService.ApplyNoteFallback(print, WbRows(""));
            Assert.AreEqual("", print.Rows[0]["Note"]);
        }
    }
}

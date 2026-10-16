using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Helpers;

namespace Weighbridge.Tests
{
    [TestClass]
    public class ReportDataHelperTests
    {
        private static DataTable ReportRows()
        {
            var dt = new DataTable();
            dt.Columns.Add("Nomor Tiket", typeof(string));
            dt.Columns.Add("Document Number SAP", typeof(string));
            dt.Columns.Add("Berat Masuk", typeof(string));
            dt.Columns.Add("Berat Keluar", typeof(string));
            dt.Columns.Add("Berat Netto", typeof(string));
            return dt;
        }

        [TestMethod]
        public void TiketSplit_DataTestingAS2610140001_BrutoPerKontrak()
        {
            var dt = ReportRows();
            dt.Rows.Add("AS2610140001", "262000164", "31910 kg", "11390 kg", "600 kg");
            dt.Rows.Add("AS2610140001", "262000165", "31910 kg", "11390 kg", "19920 kg");

            ReportDataHelper.ApplySplitBrutoPerKontrak(dt);

            Assert.AreEqual("11990 kg", dt.Rows[0]["Berat Masuk"]);   // K1 = 600 + 11.390
            Assert.AreEqual("31310 kg", dt.Rows[1]["Berat Masuk"]);   // K2 = 31.910 - 600
        }

        [TestMethod]
        public void TiketBiasa_BeratMasukTidakBerubah()
        {
            var dt = ReportRows();
            dt.Rows.Add("AS2610140002", "262000164", "50500 kg", "15190 kg", "35310 kg");

            ReportDataHelper.ApplySplitBrutoPerKontrak(dt);

            Assert.AreEqual("50500 kg", dt.Rows[0]["Berat Masuk"]);
        }

        [TestMethod]
        public void TiketSplitBelumTimbangKeluar_BeratMasukTidakBerubah()
        {
            var dt = ReportRows();
            dt.Rows.Add("AS2610150001", "262000164", "31910 kg", "0 kg", "0 kg");
            dt.Rows.Add("AS2610150001", "262000165", "31910 kg", "0 kg", "0 kg");

            ReportDataHelper.ApplySplitBrutoPerKontrak(dt);

            Assert.AreEqual("31910 kg", dt.Rows[0]["Berat Masuk"]);
            Assert.AreEqual("31910 kg", dt.Rows[1]["Berat Masuk"]);
        }

        [TestMethod]
        public void ParseKg_FormatReport()
        {
            Assert.AreEqual(31910, ReportDataHelper.ParseKg("31910 kg"));
            Assert.AreEqual(12500.5, ReportDataHelper.ParseKg("12500.5 kg"));
            Assert.AreEqual(0, ReportDataHelper.ParseKg(""));
            Assert.AreEqual(0, ReportDataHelper.ParseKg(System.DBNull.Value));
        }
    }
}

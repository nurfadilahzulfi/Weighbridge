using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Contoh perhitungan pada dokumen "Perhitungan Split Kontrak Berdasarkan SPB" untuk operator:
    /// SPB Bruto 40.200 / Tara 12.000 / SPB K1 12.000; PO K1 100.000 - GRPO 82.000, pending 3.000;
    /// PO K2 80.000 - GRPO 30.000; WB Bruto Real 40.000 / Tara Real 12.000.
    /// </summary>
    [TestClass]
    public class SplitSpbWorkedExampleTests
    {
        private const double BrutoKebun = 40200;
        private const double TaraKebun = 12000;
        private const double SpbK1 = 12000;
        private const double SisaQtyPOK1 = 100000 - 82000;
        private const double PendingK1 = 3000;
        private const double SisaQtyPOK2 = 80000 - 30000;
        private const double BrutoReal = 40000;
        private const double TaraReal = 12000;

        [TestMethod]
        public void TimbangMasuk_Langkah1Sampai4()
        {
            double nettoSpbTotal = BrutoKebun - TaraKebun;
            Assert.AreEqual(28200, nettoSpbTotal);

            double kuotaK1 = WeighbridgeCalculator.CalculateEffectiveQuota(SisaQtyPOK1, PendingK1);
            double kuotaK2 = WeighbridgeCalculator.CalculateEffectiveQuota(SisaQtyPOK2, 0);
            Assert.AreEqual(15000, kuotaK1);
            Assert.AreEqual(50000, kuotaK2);

            var masuk = WeighbridgeCalculator.CalculateSplitBySPB(nettoSpbTotal, SpbK1, TaraKebun);
            Assert.IsTrue(masuk.NeedsSplit);
            Assert.IsTrue(masuk.NettoK1 <= kuotaK1);
            Assert.AreEqual(12000, masuk.NettoK1);
            Assert.AreEqual(16200, masuk.NettoK2);
            Assert.AreEqual(24000, masuk.BrutoK1);
            Assert.AreEqual(28200, masuk.BrutoK2);

            var cekK2 = WeighbridgeCalculator.ValidateK2Quota(masuk.NettoK2, kuotaK2);
            Assert.IsTrue(cekK2.IsSufficient);
            Assert.AreEqual(33800, cekK2.RemainingQuotaAfterDeduction);
        }

        [TestMethod]
        public void TimbangKeluar_Langkah5Sampai8()
        {
            double nettoWB = WeighbridgeCalculator.CalculateNetto(BrutoReal, TaraReal);
            Assert.AreEqual(28000, nettoWB);

            var keluar = WeighbridgeCalculator.CalculateSplitGRPONetto(nettoWB, SpbK1, 16200, sisaKuotaK1SAP: SisaQtyPOK1);
            Assert.AreEqual(12000, keluar.NettoWB_K1);
            Assert.AreEqual(16000, keluar.NettoWB_K2);
            Assert.AreEqual(BrutoReal - keluar.NettoWB_K1 - TaraReal, keluar.NettoWB_K2);
            Assert.IsTrue(keluar.ShouldCreateGRPO_K2);
            Assert.IsFalse(keluar.K2ExceedsPlanned);
            Assert.IsFalse(keluar.K1LimitedBySAPQuota);
            Assert.AreEqual(-200, nettoWB - (BrutoKebun - TaraKebun));
        }

        [TestMethod]
        public void KalauBerbeda_Surplus_K2MelebihiRencana()
        {
            var keluar = WeighbridgeCalculator.CalculateSplitGRPONetto(BrutoReal - 11600, SpbK1, 16200, sisaKuotaK1SAP: SisaQtyPOK1);
            Assert.AreEqual(12000, keluar.NettoWB_K1);
            Assert.AreEqual(16400, keluar.NettoWB_K2);
            Assert.IsTrue(keluar.K2ExceedsPlanned);
            Assert.AreEqual(200, keluar.K2ExceedAmount);
        }

        [TestMethod]
        public void KalauBerbeda_SusutSangatBesar_TanpaGRPOK2()
        {
            var keluar = WeighbridgeCalculator.CalculateSplitGRPONetto(11000, SpbK1, 16200, sisaKuotaK1SAP: SisaQtyPOK1);
            Assert.AreEqual(11000, keluar.NettoWB_K1);
            Assert.AreEqual(0, keluar.NettoWB_K2);
            Assert.IsFalse(keluar.ShouldCreateGRPO_K2);
        }

        [TestMethod]
        public void KalauBerbeda_SisaQtyPOK1TurunDiSAP()
        {
            var keluar = WeighbridgeCalculator.CalculateSplitGRPONetto(28000, SpbK1, 16200, sisaKuotaK1SAP: 11500);
            Assert.AreEqual(11500, keluar.NettoWB_K1);
            Assert.AreEqual(16500, keluar.NettoWB_K2);
            Assert.IsTrue(keluar.K1LimitedBySAPQuota);
        }

        [TestMethod]
        public void IsianOtomatisFormatKg_DibacaUlangDenganBenar()
        {
            foreach (double kg in new[] { 950d, 12000d, 15000d, 16200d, 28200d, 1250500d, 12500.5d })
            {
                string shown = DocumentLookupItem.FormatKg(kg);
                Assert.AreEqual(kg, DataConversionHelper.ParseWeightInput(shown), 0.001, "Gagal membaca ulang '" + shown + "'");
            }
        }

        [TestMethod]
        public void ParseWeightInput_FormatKetikanOperator()
        {
            Assert.AreEqual(12000, DataConversionHelper.ParseWeightInput("12000"));
            Assert.AreEqual(12000, DataConversionHelper.ParseWeightInput("12.000"));
            Assert.AreEqual(12000, DataConversionHelper.ParseWeightInput("12,000"));
            Assert.AreEqual(12500.5, DataConversionHelper.ParseWeightInput("12.500,5"));
            Assert.AreEqual(12500.5, DataConversionHelper.ParseWeightInput("12,500.5"));
            Assert.AreEqual(12.5, DataConversionHelper.ParseWeightInput("12,5"));
            Assert.AreEqual(12.5, DataConversionHelper.ParseWeightInput("12.5"));
            Assert.AreEqual(0.5, DataConversionHelper.ParseWeightInput("0,500"));
            Assert.AreEqual(0, DataConversionHelper.ParseWeightInput(""));
            Assert.AreEqual(0, DataConversionHelper.ParseWeightInput("abc"));
        }

        private static DataTable SplitRows(string firstCode, double firstNetto, string secondCode, double secondNetto)
        {
            var dt = new DataTable();
            dt.Columns.Add("Code", typeof(string));
            dt.Columns.Add("U_SOL_NETTO_KEBUN", typeof(double));
            dt.Rows.Add(firstCode, firstNetto);
            dt.Rows.Add(secondCode, secondNetto);
            return dt;
        }

        [TestMethod]
        public void SplitRows_K1AdalahRecordYangDisimpanLebihDulu()
        {
            SplitTicketRowHelper.GetK1K2(SplitRows("0000101", 16200, "0000100", 12000), out DataRow k1, out DataRow k2);
            Assert.AreEqual(12000d, k1["U_SOL_NETTO_KEBUN"]);
            Assert.AreEqual(16200d, k2["U_SOL_NETTO_KEBUN"]);

            SplitTicketRowHelper.GetK1K2(SplitRows("0000100", 12000, "0000101", 16200), out k1, out k2);
            Assert.AreEqual(12000d, k1["U_SOL_NETTO_KEBUN"]);
        }

        [TestMethod]
        public void SplitRows_CodeNumerikTanpaNolDepan()
        {
            SplitTicketRowHelper.GetK1K2(SplitRows("WB10", 16200, "WB9", 12000), out DataRow k1, out _);
            Assert.AreEqual(12000d, k1["U_SOL_NETTO_KEBUN"]);
        }

        [TestMethod]
        public void CombineKebun_DataTestingTiketAS2610130003_SatuTrukUtuh()
        {
            // K1 netto SPB 25.900, K2 4.200, tara kebun 10.000
            var total = SplitTicketRowHelper.CombineKebun(25900, 4200, 10000);
            Assert.AreEqual(30100, total.Netto);
            Assert.AreEqual(10000, total.Tara);
            Assert.AreEqual(40100, total.Bruto);
        }

        [TestMethod]
        public void PreferVendorRef_AdaRefVendor_TampilkanRefVendor()
        {
            Assert.AreEqual("SPB/045/TNM", DataConversionHelper.PreferVendorRef(" SPB/045/TNM ", "261000088"));
        }

        [TestMethod]
        public void PreferVendorRef_RefVendorKosong_TampilkanNomorPO()
        {
            Assert.AreEqual("261000088", DataConversionHelper.PreferVendorRef("", "261000088"));
            Assert.AreEqual("261000088", DataConversionHelper.PreferVendorRef(null, "261000088"));
        }

        [TestMethod]
        public void SplitRows_TanpaKolomCode_UrutanAsli()
        {
            var dt = new DataTable();
            dt.Columns.Add("U_SOL_NETTO_KEBUN", typeof(double));
            dt.Rows.Add(12000d);
            dt.Rows.Add(16200d);
            SplitTicketRowHelper.GetK1K2(dt, out DataRow k1, out DataRow k2);
            Assert.AreEqual(12000d, k1["U_SOL_NETTO_KEBUN"]);
            Assert.AreEqual(16200d, k2["U_SOL_NETTO_KEBUN"]);
        }
    }
}

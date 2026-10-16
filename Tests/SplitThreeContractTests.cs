using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Split 3 kontrak, dibebankan ke kontrak terakhir:
    /// Bruto K3 = Bruto Real - Netto K1 - Netto K2, Netto K3 = Bruto K3 - Tara Real.
    /// Angka dari kasus nyata AS2608270020 (0023 / 0024 / 0025 ANS-TRI): sisa 0023 = 560 kg, sisa 0024 = 130 kg,
    /// SPB kebun 16.910 / 4.820 / 12.090, WB real 16.990 / 4.850 / 12.140.
    /// </summary>
    [TestClass]
    public class SplitThreeContractTests
    {
        [TestMethod]
        public void TimbangMasuk_K3AdalahSisaSPB_BrutoPerKontrakPakaiTaraKebun()
        {
            var res = WeighbridgeCalculator.CalculateSplitBySPB(12090, new double[] { 560, 130 }, 4820);

            Assert.IsTrue(res.NeedsSplit, res.Error);
            CollectionAssert.AreEqual(new double[] { 560, 130, 11400 }, res.Netto);
            CollectionAssert.AreEqual(new double[] { 5380, 4950, 16220 }, res.Bruto);
        }

        [TestMethod]
        public void TimbangMasuk_K2BelumDiisi_Ditolak()
        {
            var res = WeighbridgeCalculator.CalculateSplitBySPB(12090, new double[] { 560, 0 }, 4820);

            Assert.IsFalse(res.NeedsSplit);
            StringAssert.Contains(res.Error, "Netto SPB K2 belum diisi");
        }

        [TestMethod]
        public void TimbangMasuk_K1TambahK2HabiskanSPB_Ditolak()
        {
            var res = WeighbridgeCalculator.CalculateSplitBySPB(12090, new double[] { 6000, 6090 }, 4820);

            Assert.IsFalse(res.NeedsSplit);
            StringAssert.Contains(res.Error, "Kontrak K3");
        }

        [TestMethod]
        public void TimbangMasuk_Split2_HasilSamaDenganRumusLama()
        {
            var lama = WeighbridgeCalculator.CalculateSplitBySPB(28750, 4140, 11360);
            var baru = WeighbridgeCalculator.CalculateSplitBySPB(28750, new double[] { 4140 }, 11360);

            Assert.AreEqual(lama.NettoK1, baru.Netto[0]);
            Assert.AreEqual(lama.NettoK2, baru.Netto[1]);
            Assert.AreEqual(lama.BrutoK1, baru.Bruto[0]);
            Assert.AreEqual(lama.BrutoK2, baru.Bruto[1]);
        }

        [TestMethod]
        public void TimbangKeluar_K3MenampungSurplus_SesuaiRumusBrutoReal()
        {
            double brutoReal = 16990, taraReal = 4850;
            var res = WeighbridgeCalculator.CalculateSplitGRPONetto(brutoReal - taraReal, new double[] { 560, 130, 11400 }, null);

            double brutoK3 = brutoReal - 560 - 130;
            CollectionAssert.AreEqual(new double[] { 560, 130, brutoK3 - taraReal }, res.NettoWB);
            Assert.AreEqual(11450, res.NettoWB[2]);
            Assert.IsTrue(res.LastExceedsPlanned);
            Assert.AreEqual(50, res.LastExceedAmount);
        }

        [TestMethod]
        public void TimbangKeluar_K3MenampungSusut()
        {
            var res = WeighbridgeCalculator.CalculateSplitGRPONetto(12000, new double[] { 560, 130, 11400 }, null);

            CollectionAssert.AreEqual(new double[] { 560, 130, 11310 }, res.NettoWB);
            Assert.IsFalse(res.LastExceedsPlanned);
        }

        [TestMethod]
        public void TimbangKeluar_K1TidakCukup_LimpahanKeK2Dulu()
        {
            // Sisa PO K1 di SAP tinggal 500: 60 kg dari K1 pindah ke K2 (sisa PO K2 masih 1.000), bukan langsung ke K3
            var res = WeighbridgeCalculator.CalculateSplitGRPONetto(12140, new double[] { 560, 130, 11400 }, new double[] { 500, 1000 });

            CollectionAssert.AreEqual(new double[] { 500, 190, 11450 }, res.NettoWB);
            CollectionAssert.AreEqual(new[] { true, false, false }, res.LimitedBySAPQuota);
            CollectionAssert.AreEqual(new double[] { 0, 60, 0 }, res.LimpahanMasuk);
        }

        [TestMethod]
        public void TimbangKeluar_K1DanK2TidakCukup_SisanyaKeK3()
        {
            // Sisa PO K2 hanya 150: K2 menerima 20 kg dari limpahan K1, 40 kg sisanya lanjut ke K3
            var res = WeighbridgeCalculator.CalculateSplitGRPONetto(12140, new double[] { 560, 130, 11400 }, new double[] { 500, 150 });

            CollectionAssert.AreEqual(new double[] { 500, 150, 11490 }, res.NettoWB);
            CollectionAssert.AreEqual(new[] { true, true, false }, res.LimitedBySAPQuota);
            CollectionAssert.AreEqual(new double[] { 0, 60, 40 }, res.LimpahanMasuk);
            Assert.AreEqual(12140, res.NettoWB.Sum());
        }

        [TestMethod]
        public void KontrakLanjutan_TrukNonSplit_POPenuhDulu_SisanyaKeKontrakBaru()
        {
            // Truk non-split SPB 7.000, netto WB 7.050, sisa PO tinggal 6.900 -> operator pilih lanjut ke kontrak lain (rencana 0)
            var res = WeighbridgeCalculator.CalculateSplitGRPONetto(7050, new double[] { 7000, 0 }, new double[] { 6900 });

            CollectionAssert.AreEqual(new double[] { 6900, 150 }, res.NettoWB);
            Assert.IsFalse(res.LastExceedsPlanned, "Kontrak lanjutan tidak punya rencana SPB, jangan dianggap melebihi rencana");
        }

        [TestMethod]
        public void KontrakLanjutan_SplitK2Penuh_PODiisiSampaiSisaSAP()
        {
            // K2 (kontrak terakhir lama) SPB 11.400 menampung surplus jadi 11.450, sisa PO K2 hanya 11.420:
            // K2 diisi penuh 11.420, kontrak lanjutan K3 menerima 30
            var res = WeighbridgeCalculator.CalculateSplitGRPONetto(12010, new double[] { 560, 11400, 0 }, new double[] { 1000, 11420 });

            CollectionAssert.AreEqual(new double[] { 560, 11420, 30 }, res.NettoWB);
        }

        [TestMethod]
        public void WaktuMasuk_DariAngkaHHmm_DateTime_DanKosong()
        {
            var tgl = new System.DateTime(2026, 10, 14);

            Assert.AreEqual(new System.DateTime(2026, 10, 14, 16, 31, 0), DataConversionHelper.ParseWaktuMasuk(1631, tgl));
            Assert.AreEqual(new System.DateTime(2026, 10, 14, 7, 5, 0), DataConversionHelper.ParseWaktuMasuk("0705", tgl));
            Assert.AreEqual(new System.DateTime(2026, 10, 14, 9, 15, 0), DataConversionHelper.ParseWaktuMasuk(new System.DateTime(1899, 12, 30, 9, 15, 0), tgl));
            Assert.IsNull(DataConversionHelper.ParseWaktuMasuk(System.DBNull.Value, tgl));
        }

        [TestMethod]
        public void TimbangKeluar_SusutBesar_K3Nol()
        {
            var res = WeighbridgeCalculator.CalculateSplitGRPONetto(600, new double[] { 560, 130, 11400 }, null);

            CollectionAssert.AreEqual(new double[] { 560, 40, 0 }, res.NettoWB);
        }

        [TestMethod]
        public void TimbangKeluar_Split2_HasilSamaDenganRumusLama()
        {
            var lama = WeighbridgeCalculator.CalculateSplitGRPONetto(28730, 4140, 24610, sisaKuotaK1SAP: 3000);
            var baru = WeighbridgeCalculator.CalculateSplitGRPONetto(28730, new double[] { 4140, 24610 }, new double[] { 3000 });

            Assert.AreEqual(lama.NettoWB_K1, baru.NettoWB[0]);
            Assert.AreEqual(lama.NettoWB_K2, baru.NettoWB[1]);
            Assert.AreEqual(lama.K1LimitedBySAPQuota, baru.LimitedBySAPQuota[0]);
            Assert.AreEqual(lama.K2ExceedsPlanned, baru.LastExceedsPlanned);
        }

        private static DataTable WbRows(params string[] codes)
        {
            var dt = new DataTable();
            dt.Columns.Add("Code", typeof(string));
            dt.Columns.Add("U_SOL_DOCNUM_SAP", typeof(string));
            foreach (string code in codes) dt.Rows.Add(code, "PO-" + code);
            return dt;
        }

        [TestMethod]
        public void UrutanRecord_K1K2K3MenurutCode()
        {
            var rows = SplitTicketRowHelper.GetOrderedRows(WbRows("WB10", "WB9", "WB11"));

            CollectionAssert.AreEqual(new[] { "WB9", "WB10", "WB11" }, rows.Select(r => r["Code"].ToString()).ToArray());
        }

        [TestMethod]
        public void KebunSatuTruk_JumlahTigaKontrak()
        {
            var kebun = SplitTicketRowHelper.CombineKebun(new double[] { 560, 130, 11400 }, 4820);

            Assert.AreEqual(12090, kebun.Netto);
            Assert.AreEqual(16910, kebun.Bruto);
        }

        [TestMethod]
        public void NoteSplit_TigaKontrak()
        {
            string note = SplitTicketRowHelper.BuildSplitNote(new[] { "A", "B", "C" }, new double[] { 560, 130, 11450 });

            Assert.AreEqual($"A: 560 KG SPLIT B: 130 KG SPLIT C: {11450:N0} KG", note);
        }

        [TestMethod]
        public void SyncSplitGRPO_TigaKontrak_K3NolTetapSyncsapTanpaGRPO()
        {
            var sap = new FakeSapRepoForSplit();
            var service = new WeighOutService(new FakePrintHanaRepo(), sap);
            var parts = new List<SplitGRPOPart>
            {
                new SplitGRPOPart { Data = new WeighOutData { TiketNum = "T-03", Code = "WB001" }, Netto = 560 },
                new SplitGRPOPart { Data = new WeighOutData { TiketNum = "T-03", Code = "WB002" }, Netto = 40 },
                new SplitGRPOPart { Data = new WeighOutData { TiketNum = "T-03", Code = "WB003" }, Netto = 0 }
            };

            bool ok = service.SyncSplitGRPO(parts, out List<int> docEntries, out string error);

            Assert.IsTrue(ok, error);
            CollectionAssert.AreEqual(new[] { 101, 102, 0 }, docEntries);
            Assert.AreEqual(3, sap.LastSplitParts.Count);
            foreach (var p in parts)
            {
                Assert.AreEqual("SYNCSAP", p.Data.Status);
                Assert.AreEqual(p.Netto, p.Data.BeratNetto);
            }
        }

        [TestMethod]
        public void Slip_UrutanKontrakIkutRecordWB()
        {
            var dt = new DataTable();
            dt.Columns.Add("Contract No", typeof(int));
            dt.Columns.Add("Contract Qty", typeof(decimal));
            dt.Rows.Add(261000171, 50000m);
            dt.Rows.Add(261000150, 100000m);
            dt.Rows.Add(261000164, 50000m);

            PrintService.BuildContractLines(dt, new[] { "261000150", "261000164", "261000171" }, out List<string> nos, out List<string> qtys);

            CollectionAssert.AreEqual(new[] { "261000150", "261000164", "261000171" }, nos);
            CollectionAssert.AreEqual(new[] { "100,000", "50,000", "50,000" }, qtys);
        }

        [TestMethod]
        public void Slip_DuaKontrakTetapHurufNormal_TigaKontrakDiperkecil()
        {
            var arial = new FontFamily("Arial");
            const int lebarKolom = 2523;   // CONTRACT_WIDTH di PrintService

            float dua = PrintService.FitFontSize("262000164 / 262000165", arial, FontStyle.Regular, lebarKolom, 10f);
            float tiga = PrintService.FitFontSize("262000164 / 262000165 / 262000166", arial, FontStyle.Regular, lebarKolom, 8.5f);

            Assert.AreEqual(10f, dua, "Tiket split 2 kontrak tidak boleh berubah dari tampilan sekarang");
            Assert.IsTrue(tiga < 8.5f && tiga >= 6f, "3 kontrak harus diperkecil agar muat, tetapi tetap terbaca: " + tiga);
        }
    }
}

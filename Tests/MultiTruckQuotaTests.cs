using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Beberapa truk timbang masuk di kontrak yang sama sebelum ada timbang keluar.
    /// Contoh: sisa PO K1 di SAP 30.000 kg, tiap truk SPB 12.000 kg, tara 5.000 kg.
    /// </summary>
    [TestClass]
    public class MultiTruckQuotaTests
    {
        private const double SisaK1SAP = 30000;
        private const double SPB = 12000;
        private const double Tara = 5000;

        private static PendingTicketInfo Pending(string tiket, double netto, double bruto = 0)
            => new PendingTicketInfo { TiketNum = tiket, Netto = netto, Bruto = bruto };

        // ---------- Timbang masuk: kuota efektif ----------

        [TestMethod]
        public void Truk1_TidakAdaPending_Normal()
        {
            double efektif = WeighbridgeCalculator.CalculateEffectiveQuota(SisaK1SAP, WeighbridgeCalculator.SumPendingNetto(new List<PendingTicketInfo>()));
            Assert.AreEqual(30000, efektif);
            Assert.IsFalse(WeighbridgeCalculator.RequiresSplitKontrak(SPB, efektif));
        }

        [TestMethod]
        public void Truk2_SatuPending_MasihCukup()
        {
            var pending = new List<PendingTicketInfo> { Pending("T1", SPB) };
            double efektif = WeighbridgeCalculator.CalculateEffectiveQuota(SisaK1SAP, WeighbridgeCalculator.SumPendingNetto(pending));
            Assert.AreEqual(18000, efektif);
            Assert.IsFalse(WeighbridgeCalculator.RequiresSplitKontrak(SPB, efektif));
        }

        [TestMethod]
        public void Truk3_DuaPending_WajibSplit_K1SisaK2Lebihnya()
        {
            var pending = new List<PendingTicketInfo> { Pending("T1", SPB), Pending("T2", SPB) };
            double efektif = WeighbridgeCalculator.CalculateEffectiveQuota(SisaK1SAP, WeighbridgeCalculator.SumPendingNetto(pending));
            Assert.AreEqual(6000, efektif);
            Assert.IsTrue(WeighbridgeCalculator.RequiresSplitKontrak(SPB, efektif));

            // Supplier menerbitkan SPB K1 sesuai sisa kontrak (6.000), sisanya SPB K2
            var split = WeighbridgeCalculator.CalculateSplitBySPB(SPB, 6000, Tara);
            Assert.IsTrue(split.NeedsSplit);
            Assert.IsTrue(split.NettoK1 <= efektif);
            Assert.AreEqual(6000, split.NettoK1);
            Assert.AreEqual(6000, split.NettoK2);
        }

        [TestMethod]
        public void Truk4_KuotaK1Habis_SPBK1ApaPunMelebihiKuota_PilihK2Langsung()
        {
            // Truk 1, 2 normal + porsi K1 truk 3 (6.000) masih pending di PO K1
            var pending = new List<PendingTicketInfo> { Pending("T1", SPB), Pending("T2", SPB), Pending("T3", 6000) };
            double efektif = WeighbridgeCalculator.CalculateEffectiveQuota(SisaK1SAP, WeighbridgeCalculator.SumPendingNetto(pending));
            Assert.AreEqual(0, efektif);

            // Tidak ada lagi pengalihan otomatis: SPB K1 berapa pun > kuota 0 sehingga ditolak, operator memilih PO K2 langsung
            var split = WeighbridgeCalculator.CalculateSplitBySPB(SPB, 1, Tara);
            Assert.IsTrue(split.NettoK1 > efektif);
        }

        [TestMethod]
        public void SplitBySPB_K1DariSPBSupplier()
        {
            var split = WeighbridgeCalculator.CalculateSplitBySPB(SPB, 2500, Tara);
            Assert.IsTrue(split.NeedsSplit);
            Assert.AreEqual(2500, split.NettoK1);
            Assert.AreEqual(SPB - 2500, split.NettoK2);
        }

        [TestMethod]
        public void Truk5_PilihK2Langsung_KuotaK2DikurangiPendingTruk3DanTruk4()
        {
            // Sisa PO K2 di SAP 50.000; pending di K2: porsi K2 truk 3 (6.000) + truk 4 (12.000)
            var pendingK2 = new List<PendingTicketInfo> { Pending("T3", 6000), Pending("T4", SPB) };
            double efektifK2 = WeighbridgeCalculator.CalculateEffectiveQuota(50000, WeighbridgeCalculator.SumPendingNetto(pendingK2));
            Assert.AreEqual(32000, efektifK2);
            Assert.IsFalse(WeighbridgeCalculator.RequiresSplitKontrak(SPB, efektifK2));
        }

        [TestMethod]
        public void KuotaK2Efektif_TidakCukup_TerdeteksiValidasiK2()
        {
            var pendingK2 = new List<PendingTicketInfo> { Pending("T3", 6000) };
            double efektifK2 = WeighbridgeCalculator.CalculateEffectiveQuota(10000, WeighbridgeCalculator.SumPendingNetto(pendingK2));
            Assert.AreEqual(4000, efektifK2);

            var valK2 = WeighbridgeCalculator.ValidateK2Quota(6000, efektifK2);
            Assert.IsFalse(valK2.IsSufficient);
            Assert.AreEqual(2000, valK2.Shortage);
        }

        // ---------- SumPendingNetto ----------

        [TestMethod]
        public void SumPendingNetto_TiketSamaDihitungSekali_BrutoTidakDipakai()
        {
            var pending = new List<PendingTicketInfo>
            {
                Pending("T1", 12000),
                Pending("t1", 12000),          // duplikat (beda huruf besar/kecil)
                Pending("T2", 0, bruto: 17000), // bruto termasuk berat truk -> tidak boleh jadi cadangan kuota
                Pending("", 1000),             // tanpa nomor tiket tetap dihitung
                null
            };
            Assert.AreEqual(13000, WeighbridgeCalculator.SumPendingNetto(pending));
            Assert.AreEqual(0, WeighbridgeCalculator.SumPendingNetto(null));
        }

        // ---------- Perkiraan netto truk pending ----------

        [TestMethod]
        public void EstimatePendingNetto_NettoKebunAda_DipakaiLangsung()
        {
            Assert.AreEqual(12100, WeighbridgeCalculator.EstimatePendingNetto(16910, 4810, 12100, 17000));
        }

        [TestMethod]
        public void EstimatePendingNetto_NettoKosong_BrutoMinusTara()
        {
            Assert.AreEqual(12100, WeighbridgeCalculator.EstimatePendingNetto(16910, 4810, 0, 17000));
        }

        [TestMethod]
        public void EstimatePendingNetto_HanyaTaraDanBeratMasuk_BeratMasukMinusTara()
        {
            Assert.AreEqual(12190, WeighbridgeCalculator.EstimatePendingNetto(0, 4810, 0, 17000));
        }

        [TestMethod]
        public void EstimatePendingNetto_RecordSplitK1Alokasi0_Nol()
        {
            // Record K1 lama dengan alokasi 0: Bruto = 0 + Tara -> bukan seluruh muatan truk
            Assert.AreEqual(0, WeighbridgeCalculator.EstimatePendingNetto(4950, 4950, 0, 17450));
        }

        [TestMethod]
        public void EstimatePendingNetto_TanpaDataKebun_TidakPakaiBeratMasukUtuh()
        {
            // Dulu: netto = berat masuk 17.000 (termasuk truk ±5 ton). Sekarang tidak ditebak.
            Assert.AreEqual(0, WeighbridgeCalculator.EstimatePendingNetto(0, 0, 0, 17000));
            Assert.AreEqual(0, WeighbridgeCalculator.EstimatePendingNetto(16910, 0, 0, 17000));
        }

        // ---------- Netto SPB wajib ----------

        [TestMethod]
        public void IsNettoKebunMissing_PO_TanpaNetto_Wajib()
        {
            Assert.IsTrue(WeighbridgeCalculator.IsNettoKebunMissing("PO", 0));
            Assert.IsTrue(WeighbridgeCalculator.IsNettoKebunMissing("po", -1));
            Assert.IsFalse(WeighbridgeCalculator.IsNettoKebunMissing("PO", 12100));
        }

        [TestMethod]
        public void IsNettoKebunMissing_SO_TidakWajib()
        {
            Assert.IsFalse(WeighbridgeCalculator.IsNettoKebunMissing("SO", 0));
        }

        [TestMethod]
        public void CalculateEffectiveQuota_TidakPernahNegatif()
        {
            Assert.AreEqual(0, WeighbridgeCalculator.CalculateEffectiveQuota(10000, 25000));
            Assert.AreEqual(10000, WeighbridgeCalculator.CalculateEffectiveQuota(10000, -5));
        }

        // ---------- Timbang keluar split: K1 dibatasi sisa PO K1 di SAP ----------

        [TestMethod]
        public void TimbangKeluarTruk3_Truk1Dan2Surplus_K1DibatasiSisaSAP()
        {
            // Truk 1 & 2 sudah GRPO 12.050 + 12.030 -> sisa PO K1 = 5.920. Truk 3 real 12.000.
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(12000, 6000, 6000, sisaKuotaK1SAP: 5920);
            Assert.AreEqual(5920, r.NettoWB_K1);
            Assert.AreEqual(6080, r.NettoWB_K2);
            Assert.IsTrue(r.K1LimitedBySAPQuota);
            Assert.AreEqual(12000, r.NettoWB_K1 + r.NettoWB_K2);
        }

        [TestMethod]
        public void TimbangKeluarTruk3_Truk1Dan2Susut_K1TetapSesuaiRencana()
        {
            // Sisa PO K1 6.100 (susut), K1 rencana 6.000 -> K1 tetap 6.000
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(11950, 6000, 6000, sisaKuotaK1SAP: 6100);
            Assert.AreEqual(6000, r.NettoWB_K1);
            Assert.AreEqual(5950, r.NettoWB_K2);
            Assert.IsFalse(r.K1LimitedBySAPQuota);
        }

        [TestMethod]
        public void TimbangKeluarSplit_SisaK1SAPNol_SemuaKeK2()
        {
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(12000, 6000, 6000, sisaKuotaK1SAP: 0);
            Assert.AreEqual(0, r.NettoWB_K1);
            Assert.AreEqual(12000, r.NettoWB_K2);
            Assert.IsTrue(r.K1LimitedBySAPQuota);
        }

        [TestMethod]
        public void TimbangKeluarSplit_TanpaBatasSAP_PerilakuLamaTetapSama()
        {
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(12000, 6000, 6000);
            Assert.AreEqual(6000, r.NettoWB_K1);
            Assert.AreEqual(6000, r.NettoWB_K2);
            Assert.IsFalse(r.K1LimitedBySAPQuota);
        }

        // Bug ditemukan saat uji TANIMASTESTFORM: netto pending 12.000 terbaca 12.000.000.000
        // karena "12000,000000" di-parse InvariantCulture (koma dianggap pemisah ribuan).
        [TestMethod]
        public void PendingNetto_FromHanaDecimal_NotInflated()
        {
            Assert.AreEqual(12000.0, Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(new Sap.Data.Hana.HanaDecimal(12000.000000m)), 0.001);
            Assert.AreEqual(12000.0, Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble("12000,000000"), 0.001);
            Assert.AreEqual(12000.0, Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(12000.000000m), 0.001);

            var pending = new List<PendingTicketInfo>
            {
                new PendingTicketInfo { TiketNum = "T1", Netto = Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble("12000,000000") }
            };
            double ef = WeighbridgeCalculator.CalculateEffectiveQuota(12850, WeighbridgeCalculator.SumPendingNetto(pending));
            Assert.AreEqual(850, ef, 0.001);
        }
    }
}

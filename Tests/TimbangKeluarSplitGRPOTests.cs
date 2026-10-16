using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Unit test untuk logika pembagian Netto WB saat rilis GRPO Split Kontrak (Timbang Keluar).
    ///
    /// Aturan Bisnis yang Diverifikasi:
    ///   1. Netto SELALU dari timbangan WB perusahaan, bukan kebun.
    ///   2. K1 mendapat Min(totalNettoWB, nettoKebunK1) — menghabiskan sisa kuota K1.
    ///   3. K2 mendapat SELURUH sisa: K1 + K2 == totalNettoWB (tidak ada sisa/hilang).
    ///   4. GRPO K2 hanya dibuat jika K2 >= 1 kg (minimum threshold).
    ///   5. Peringatan diberikan jika K2 melebihi rencana dari timbang masuk.
    ///   6. totalNettoWB &lt;= 0 harus menghasilkan semua nilai nol (diblokir di UI).
    /// </summary>
    [TestClass]
    public class TimbangKeluarSplitGRPOTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // GRUP 1: Alur Normal (WB ≈ Kebun) — tidak ada sisa
        // ─────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void Normal_WBSamaDenganKebun_SplitTepatDanTidakAdaSisa()
        {
            // Skenario paling umum: WB perusahaan = netto kebun
            // Kebun: 21.300 kg → K1: 15.000, K2: 6.300
            // WB   : 21.300 kg → semua terserap, tidak ada sisa
            double totalNettoWB = 21300;
            double nettoKebunK1 = 15000;
            double nettoKebunK2 = 6300;

            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(totalNettoWB, nettoKebunK1, nettoKebunK2);

            Assert.AreEqual(15000, r.NettoWB_K1, "K1 harus habiskan kuota K1.");
            Assert.AreEqual(6300, r.NettoWB_K2, "K2 = sisa setelah K1.");
            Assert.AreEqual(totalNettoWB, r.NettoWB_K1 + r.NettoWB_K2,
                "INVARIANT: K1 + K2 harus selalu = totalNettoWB, tidak ada sisa.");
            Assert.IsFalse(r.K2ExceedsPlanned, "K2 tidak melebihi rencana pada kasus normal.");
            Assert.IsTrue(r.ShouldCreateGRPO_K2, "K2 6.300 kg >= 1 kg, GRPO K2 harus dibuat.");
        }

        [TestMethod]
        public void Normal_KontrakK1HabisTepat_K2NolTidakBuatGRPO()
        {
            // WB tepat sama dengan kuota K1 → K2 = 0 → tidak buat GRPO K2
            double totalNettoWB = 15000;
            double nettoKebunK1 = 15000;
            double nettoKebunK2 = 6300;

            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(totalNettoWB, nettoKebunK1, nettoKebunK2);

            Assert.AreEqual(15000, r.NettoWB_K1);
            Assert.AreEqual(0, r.NettoWB_K2);
            Assert.AreEqual(totalNettoWB, r.NettoWB_K1 + r.NettoWB_K2,
                "Invariant K1+K2=total harus tetap terpenuhi.");
            Assert.IsFalse(r.ShouldCreateGRPO_K2, "K2 = 0 kg, GRPO K2 tidak perlu dibuat.");
        }

        [TestMethod]
        public void Normal_WBLebihKecilDariK1Quota_SemuaKeK1TidakAdaK2()
        {
            // Truk ternyata membawa lebih sedikit dari kuota K1 → tidak perlu split
            double totalNettoWB = 12500;
            double nettoKebunK1 = 15000;
            double nettoKebunK2 = 6300;

            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(totalNettoWB, nettoKebunK1, nettoKebunK2);

            Assert.AreEqual(12500, r.NettoWB_K1, "K1 = seluruh WB (tidak melebihi kuota K1).");
            Assert.AreEqual(0, r.NettoWB_K2, "K2 = 0 karena WB < kuota K1.");
            Assert.AreEqual(totalNettoWB, r.NettoWB_K1 + r.NettoWB_K2, "Invariant tetap terpenuhi.");
            Assert.IsFalse(r.ShouldCreateGRPO_K2, "Tidak ada K2, tidak buat GRPO K2.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // GRUP 2: Invariant K1 + K2 = totalNettoWB (berbagai skenario)
        // ─────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void Invariant_WBSedikit_Lebih_Besar_DariKebun()
        {
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(21500, 15000, 6300);
            Assert.AreEqual(21500, r.NettoWB_K1 + r.NettoWB_K2, 0.01,
                "Invariant: K1+K2 = totalNettoWB meskipun WB > rencana kebun.");
        }

        [TestMethod]
        public void Invariant_Netto350Ton_BalanceSempurna()
        {
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(350000, 200000, 150000);
            Assert.AreEqual(350000, r.NettoWB_K1 + r.NettoWB_K2, 0.01,
                "350 ton: K1(200t) + K2(150t) = totalNettoWB.");
        }

        [TestMethod]
        public void Invariant_AnkgaDesimal_BalanceDalamToleransiPembulatan()
        {
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(21300.567, 15000, 6300);
            Assert.AreEqual(21300.567, r.NettoWB_K1 + r.NettoWB_K2, 0.01,
                "Angka desimal: invariant terpenuhi dalam toleransi pembulatan 0.01.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // GRUP 3: Threshold minimum GRPO K2
        // ─────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void Threshold_K2TepetSamaMinimum1Kg_BuatGRPO()
        {
            // K2 = tepat 1 kg (sama dengan threshold default) → GRPO K2 harus dibuat
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(15001, 15000, 6300);

            Assert.AreEqual(1.0, r.NettoWB_K2, 0.01);
            Assert.IsTrue(r.ShouldCreateGRPO_K2, "K2 = 1 kg (= threshold), GRPO K2 harus dibuat.");
        }

        [TestMethod]
        public void Threshold_K2DiBawahMinimum_TidakBuatGRPO()
        {
            // K2 = 0.5 kg → di bawah threshold 1 kg → GRPO K2 tidak dibuat
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(15000.5, 15000, 6300);

            Assert.AreEqual(0.5, r.NettoWB_K2, 0.01);
            Assert.IsFalse(r.ShouldCreateGRPO_K2,
                "K2 = 0.5 kg < 1 kg, GRPO K2 tidak boleh dibuat.");
        }

        [TestMethod]
        public void Threshold_CustomMinimum10Kg_DigunakanDenganBenar()
        {
            // Custom threshold: 10 kg, K2 = 5 kg → tidak buat GRPO
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(15005, 15000, 6300, minimumGRPO_K2_Kg: 10.0);

            Assert.AreEqual(5.0, r.NettoWB_K2, 0.01);
            Assert.IsFalse(r.ShouldCreateGRPO_K2, "K2 5 kg < custom threshold 10 kg.");
            Assert.AreEqual(10.0, r.MinimumGRPO_K2_Kg, "Threshold tersimpan di result.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // GRUP 4: K2 melebihi rencana (WB lebih besar dari kebun)
        // ─────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void K2MelebihiRencana_TandaiDanHitungSelisih()
        {
            // WB perusahaan lebih besar dari kebun
            // K2 rencana = 6.300, K2 aktual WB = 7.000 → selisih 700 kg
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(22000, 15000, 6300);

            Assert.AreEqual(15000, r.NettoWB_K1);
            Assert.AreEqual(7000, r.NettoWB_K2);
            Assert.AreEqual(22000, r.NettoWB_K1 + r.NettoWB_K2, 0.01, "Invariant tetap terpenuhi.");
            Assert.IsTrue(r.K2ExceedsPlanned, "K2 aktual > rencana, harus diberi peringatan.");
            Assert.AreEqual(700, r.K2ExceedAmount, 0.01, "Selisih K2 vs rencana = 700 kg.");
        }

        [TestMethod]
        public void K2TidakMelebihiRencana_TidakAdaPeringatan()
        {
            // K2 aktual (6.000) < K2 rencana (6.300) → tidak ada peringatan
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(21000, 15000, 6300);

            Assert.AreEqual(6000, r.NettoWB_K2);
            Assert.IsFalse(r.K2ExceedsPlanned, "K2 < rencana, tidak ada peringatan.");
            Assert.AreEqual(0, r.K2ExceedAmount, "Tidak ada selisih.");
        }

        [TestMethod]
        public void K2TepatSamaRencana_TidakDianggapMelebihi()
        {
            // K2 aktual = K2 rencana persis → tidak ada peringatan
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(21300, 15000, 6300);

            Assert.AreEqual(6300, r.NettoWB_K2);
            Assert.IsFalse(r.K2ExceedsPlanned, "K2 tepat sama rencana, tidak dianggap melebihi.");
            Assert.AreEqual(0, r.K2ExceedAmount);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GRUP 5: Guard — totalNettoWB wajib > 0
        // ─────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void NettoWBNol_SemuaHasilNol_TidakAdaGRPO()
        {
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(0, 15000, 6300);

            Assert.AreEqual(0, r.NettoWB_K1);
            Assert.AreEqual(0, r.NettoWB_K2);
            Assert.IsFalse(r.ShouldCreateGRPO_K2, "Netto WB 0, tidak ada GRPO yang dibuat.");
            Assert.IsFalse(r.K2ExceedsPlanned);
        }

        [TestMethod]
        public void NettoWBNegatif_SemuaHasilNol_TidakAdaGRPO()
        {
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(-500, 15000, 6300);

            Assert.AreEqual(0, r.NettoWB_K1);
            Assert.AreEqual(0, r.NettoWB_K2);
            Assert.IsFalse(r.ShouldCreateGRPO_K2);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GRUP 6: Skenario riil bisnis
        // ─────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void Skenario_KuotaK1HampirHabis_K2Besar()
        {
            // K1 hampir habis: sisa hanya 3.200 kg
            // Netto WB truk: 21.300 kg → K1: 3.200, K2: 18.100 kg
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(21300, 3200, 18100);

            Assert.AreEqual(3200, r.NettoWB_K1, "K1 mendapat sisa kuota yang ada.");
            Assert.AreEqual(18100, r.NettoWB_K2, "K2 mendapat sisa besar.");
            Assert.AreEqual(21300, r.NettoWB_K1 + r.NettoWB_K2, 0.01, "Invariant terpenuhi.");
            Assert.IsFalse(r.K2ExceedsPlanned, "K2 aktual = rencana K2, tidak ada peringatan.");
            Assert.IsTrue(r.ShouldCreateGRPO_K2);
        }

        [TestMethod]
        public void Skenario_BugTiga_K2SangatKecil9Gram_TidakBuatGRPO()
        {
            // Kasus bug #3: K2 hanya 9 gram (0.009 kg), sebelumnya sistem tetap buat GRPO!
            // totalNettoWB sedikit di atas K1 quota
            var r = WeighbridgeCalculator.CalculateSplitGRPONetto(15000.009, 15000, 6300);

            Assert.AreEqual(15000, r.NettoWB_K1, 0.001);
            Assert.IsTrue(r.NettoWB_K2 < 1.0, "K2 harus sangat kecil.");
            Assert.IsFalse(r.ShouldCreateGRPO_K2,
                "K2 = 9 gram < 1 kg, GRPO K2 TIDAK boleh dibuat (Bug #3 fix).");
            // Invariant tetap
            Assert.AreEqual(15000.009, r.NettoWB_K1 + r.NettoWB_K2, 0.01);
        }
    }
}

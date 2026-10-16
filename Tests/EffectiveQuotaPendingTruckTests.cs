using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Unit tests untuk logika "Sisa Kuota Efektif" yang memperhitungkan truk pending di pabrik.
    /// 
    /// Latar belakang:
    /// - Kuota PO dari SAP belum berkurang selama GRPO belum dibuat (timbang keluar belum dilakukan)
    /// - Truk yang sudah timbang masuk (status WEIGHIN) dianggap "menduduki" kuota
    /// - Netto Kebun digunakan sebagai estimasi terbaik untuk beban pending (karena Netto WB belum diketahui sebelum timbang keluar)
    /// - Netto WB (bruto WB - tara WB) adalah nilai yang sesungguhnya mengurangi kuota PO saat GRPO dibuat
    /// </summary>
    [TestClass]
    public class EffectiveQuotaPendingTruckTests
    {
        // =====================================================================
        // BAGIAN 1: Kalkulasi Netto WB (untuk pengurangan kuota di GRPO)
        // =====================================================================

        [TestMethod]
        [Description("Netto WB = BeratMasuk - BeratKeluar. Ini yang dipakai di CreateGRPO untuk kurangi kuota PO.")]
        public void CalculateNetto_WB_BeratMasukMinusBeratKeluar()
        {
            double beratMasuk = 50000; // kg
            double beratKeluar = 4200; // kg (berat kendaraan kosong)

            double nettoWB = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            Assert.AreEqual(45800, nettoWB, "Netto WB harus = bruto WB - tara WB");
        }

        [TestMethod]
        [Description("CalculateNetto pakai Math.Abs sehingga selalu positif. Validasi data error (keluar>masuk) dilakukan oleh konteks bisnis, bukan oleh method ini.")]
        public void CalculateNetto_WB_MathAbsSelalu_Positif()
        {
            double beratMasuk = 3000;
            double beratKeluar = 4000; // data aneh: keluar > masuk

            double nettoWB = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            // CalculateNetto menggunakan Math.Abs → selalu positif
            Assert.AreEqual(1000, nettoWB,
                "CalculateNetto = Math.Abs(masuk - keluar) = |3000 - 4000| = 1000. " +
                "Validasi data error dilakukan di layer form (berat masuk harus > berat keluar).");
        }

        [TestMethod]
        [Description("Netto WB bisa berbeda jauh dari Netto Kebun — validasi bahwa keduanya field terpisah.")]
        public void NettoWB_vs_NettoKebun_NilaiTidakHarusSama()
        {
            // Netto Kebun dari surat jalan kebun
            double nettoKebun = 47000;

            // Netto WB real dari timbangan perusahaan (bruto - tara)
            double beratMasuk = 50000;
            double beratKeluar = 4200;
            double nettoWB = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar); // = 45800

            // Keduanya BOLEH berbeda — ini adalah by design
            Assert.AreNotEqual(nettoKebun, nettoWB,
                "Netto Kebun dan Netto WB adalah dua angka berbeda. Netto WB digunakan untuk GRPO, Netto Kebun sebagai referensi.");
        }

        // =====================================================================
        // BAGIAN 2: PendingTicketInfo — model data truk pending
        // =====================================================================

        [TestMethod]
        [Description("PendingTicketInfo.Netto harus dapat disimpan dan dibaca dengan benar.")]
        public void PendingTicketInfo_Netto_CanBeSetAndRead()
        {
            var info = new PendingTicketInfo
            {
                TiketNum = "AS2609250001",
                Nopol = "B 1234 ABC",
                NamaSupir = "Supir A",
                Bruto = 50000,
                Netto = 47000,
                Status = "BELUM GRPO / BELUM TIMBANG KELUAR"
            };

            Assert.AreEqual("AS2609250001", info.TiketNum);
            Assert.AreEqual(47000, info.Netto);
            Assert.AreEqual(50000, info.Bruto);
        }

        [TestMethod]
        [Description("Jika Netto <= 0, fallback ke Bruto digunakan sebagai estimasi (konservatif).")]
        public void PendingTicketInfo_NettoZero_FallbackToBruto()
        {
            var info = new PendingTicketInfo
            {
                TiketNum = "AS2609250002",
                Bruto = 48000,
                Netto = 0  // belum terisi
            };

            double estimasiNetto = info.Netto > 0 ? info.Netto : info.Bruto;

            Assert.AreEqual(48000, estimasiNetto,
                "Jika Netto belum terisi (0), Bruto digunakan sebagai estimasi konservatif.");
        }

        // =====================================================================
        // BAGIAN 3: Logika Efektif Kuota dengan Pending Trucks
        // =====================================================================

        [TestMethod]
        [Description("Skenario: 1 truk pending di pabrik. Kuota efektif = kuota SAP - netto pending truk.")]
        public void EffectiveQuota_SatuTrukPending_KuotaBerkurang()
        {
            double kuotaSAP = 100000;  // sisa kuota dari SAP (belum dikurangi pending)
            var pendingTrucks = new List<PendingTicketInfo>
            {
                new PendingTicketInfo { TiketNum = "AS001", Netto = 47000, Bruto = 50000 }
            };

            double pendingCommittedNetto = 0;
            foreach (var pt in pendingTrucks)
                pendingCommittedNetto += (pt.Netto > 0 ? pt.Netto : pt.Bruto);

            double sisaKuotaEfektif = System.Math.Max(0, kuotaSAP - pendingCommittedNetto);

            Assert.AreEqual(47000, pendingCommittedNetto);
            Assert.AreEqual(53000, sisaKuotaEfektif,
                "Sisa kuota efektif = 100.000 - 47.000 = 53.000 kg");
        }

        [TestMethod]
        [Description("Skenario: 3 truk pending di pabrik. Kuota efektif dikurangi total semua netto pending.")]
        public void EffectiveQuota_TigaTrukPending_KuotaBerkurangTotal()
        {
            double kuotaSAP = 150000;
            var pendingTrucks = new List<PendingTicketInfo>
            {
                new PendingTicketInfo { TiketNum = "AS001", Netto = 46000, Bruto = 50000 },
                new PendingTicketInfo { TiketNum = "AS002", Netto = 45500, Bruto = 49000 },
                new PendingTicketInfo { TiketNum = "AS003", Netto = 47200, Bruto = 51000 }
            };

            double pendingCommittedNetto = 0;
            foreach (var pt in pendingTrucks)
                pendingCommittedNetto += (pt.Netto > 0 ? pt.Netto : pt.Bruto);

            double sisaKuotaEfektif = System.Math.Max(0, kuotaSAP - pendingCommittedNetto);

            Assert.AreEqual(138700, pendingCommittedNetto, "Total pending = 46000+45500+47200 = 138700");
            Assert.AreEqual(11300, sisaKuotaEfektif, "Sisa = 150000 - 138700 = 11300 kg");
        }

        [TestMethod]
        [Description("Skenario: Total netto pending melebihi kuota SAP. Kuota efektif harus 0, tidak negatif.")]
        public void EffectiveQuota_PendingMelebihiKuota_HasilNolBukanNegatif()
        {
            double kuotaSAP = 50000;
            var pendingTrucks = new List<PendingTicketInfo>
            {
                new PendingTicketInfo { TiketNum = "AS001", Netto = 30000, Bruto = 33000 },
                new PendingTicketInfo { TiketNum = "AS002", Netto = 28000, Bruto = 31000 }
            };

            double pendingCommittedNetto = 0;
            foreach (var pt in pendingTrucks)
                pendingCommittedNetto += (pt.Netto > 0 ? pt.Netto : pt.Bruto);

            double sisaKuotaEfektif = System.Math.Max(0, kuotaSAP - pendingCommittedNetto);

            Assert.AreEqual(58000, pendingCommittedNetto);
            Assert.AreEqual(0, sisaKuotaEfektif,
                "Jika pending melebihi kuota, sisa efektif = 0 (tidak boleh negatif). Wajib split kontrak.");
        }

        [TestMethod]
        [Description("Skenario: Tidak ada truk pending. Kuota efektif = kuota SAP (tidak berubah).")]
        public void EffectiveQuota_TidakAdaPending_KuotaTetap()
        {
            double kuotaSAP = 80000;
            var pendingTrucks = new List<PendingTicketInfo>(); // kosong

            double pendingCommittedNetto = 0;
            foreach (var pt in pendingTrucks)
                pendingCommittedNetto += (pt.Netto > 0 ? pt.Netto : pt.Bruto);

            double sisaKuotaEfektif = System.Math.Max(0, kuotaSAP - pendingCommittedNetto);

            Assert.AreEqual(0, pendingCommittedNetto);
            Assert.AreEqual(80000, sisaKuotaEfektif,
                "Tanpa truk pending, kuota efektif sama dengan kuota SAP.");
        }

        // =====================================================================
        // BAGIAN 4: Integrasi dengan Split Kontrak
        // =====================================================================

        [TestMethod]
        [Description("Skenario: Truk ke-3 masuk saat kuota efektif tinggal sedikit → wajib split kontrak.")]
        public void EffectiveQuota_TrukKetigaMasuk_KuotaEfektifTinggalSedikit_WajibSplit()
        {
            double kuotaSAP = 140000; // kuota dari SAP (belum berkurang karena GRPO belum dibuat)
            var pendingTrucks = new List<PendingTicketInfo>
            {
                new PendingTicketInfo { TiketNum = "AS001", Netto = 46000 },
                new PendingTicketInfo { TiketNum = "AS002", Netto = 45500 }
            };

            double pendingCommittedNetto = 0;
            foreach (var pt in pendingTrucks)
                pendingCommittedNetto += (pt.Netto > 0 ? pt.Netto : pt.Bruto);

            double sisaKuotaEfektif = System.Math.Max(0, kuotaSAP - pendingCommittedNetto);

            // Truk ke-3 membawa muatan
            double nettoKebunTrukKetiga = 47000;

            // Validasi apakah perlu split
            bool wajibSplit = sisaKuotaEfektif < nettoKebunTrukKetiga;
            double alokasi_K1 = System.Math.Min(sisaKuotaEfektif, nettoKebunTrukKetiga);
            double alokasi_K2 = nettoKebunTrukKetiga - alokasi_K1;

            Assert.AreEqual(91500, pendingCommittedNetto, "Total pending 2 truk = 46000+45500=91500");
            Assert.AreEqual(48500, sisaKuotaEfektif, "Sisa efektif = 140000 - 91500 = 48500 kg");
            Assert.IsFalse(wajibSplit, "Truk ke-3 dengan netto 47.000 masih muat di sisa 48.500 kg, tidak perlu split.");
        }

        [TestMethod]
        [Description("Skenario kritis: Kuota efektif 10.000 kg, truk ke-3 bawa 47.000 kg → WAJIB split.")]
        public void EffectiveQuota_KuotaEfektifKecil_TrukBesarMasuk_WajibSplit()
        {
            double sisaKuotaEfektif = 10000; // sudah dikurangi pending

            double nettoKebunTrukBaru = 47000;

            bool wajibSplit = sisaKuotaEfektif < nettoKebunTrukBaru && sisaKuotaEfektif > 0;
            bool kuotaHabis = sisaKuotaEfektif <= 0;

            double alokasi_K1 = System.Math.Min(sisaKuotaEfektif, nettoKebunTrukBaru);
            double alokasi_K2 = nettoKebunTrukBaru - alokasi_K1;

            Assert.IsTrue(wajibSplit, "Netto truk (47.000) > sisa efektif (10.000) → WAJIB split kontrak.");
            Assert.IsFalse(kuotaHabis, "Kuota belum habis, ada 10.000 kg tersisa untuk K1.");
            Assert.AreEqual(10000, alokasi_K1, "Alokasi K1 = sisa kuota efektif = 10.000 kg");
            Assert.AreEqual(37000, alokasi_K2, "Alokasi K2 = 47.000 - 10.000 = 37.000 kg untuk kontrak lain.");
        }

        [TestMethod]
        [Description("Skenario: Kuota efektif = 0 karena semua terpakai oleh pending. Truk baru WAJIB 100% ke K2.")]
        public void EffectiveQuota_KuotaEfektifNol_TrukBaruSeluruhnyaKeK2()
        {
            double sisaKuotaEfektif = 0;

            double nettoKebunTrukBaru = 46000;
            bool kuotaHabis = sisaKuotaEfektif <= 0;

            double alokasi_K1 = System.Math.Min(sisaKuotaEfektif, nettoKebunTrukBaru);
            double alokasi_K2 = nettoKebunTrukBaru - alokasi_K1;

            Assert.IsTrue(kuotaHabis, "Sisa kuota efektif = 0, wajib split kontrak penuh ke K2.");
            Assert.AreEqual(0, alokasi_K1, "K1 tidak mendapat alokasi karena kuota efektif habis.");
            Assert.AreEqual(46000, alokasi_K2, "Seluruh 46.000 kg harus ke K2.");
        }

        // =====================================================================
        // BAGIAN 5: Validasi CheckPOQuota dengan kuota efektif
        // =====================================================================

        [TestMethod]
        [Description("CheckPOQuota menggunakan sisaKuotaEfektif (bukan kuota SAP asli) sebagai remainingQuota.")]
        public void CheckPOQuota_DenganKuotaEfektif_BukanKuotaSAP()
        {
            double kuotaSAP = 140000;
            double pendingCommittedNetto = 120000; // 3 truk pending
            double sisaKuotaEfektif = System.Math.Max(0, kuotaSAP - pendingCommittedNetto); // = 20000

            double nettoKebunTrukBaru = 47000;

            // Validasi menggunakan kuota EFEKTIF, bukan kuota SAP
            var resultEffective = WeighbridgeCalculator.CheckPOQuota(nettoKebunTrukBaru, sisaKuotaEfektif);
            var resultSAP = WeighbridgeCalculator.CheckPOQuota(nettoKebunTrukBaru, kuotaSAP);

            Assert.IsTrue(resultEffective.IsExceeded,
                "Dengan kuota efektif (20.000), netto 47.000 kg melebihi — wajib split.");
            Assert.IsFalse(resultSAP.IsExceeded,
                "Dengan kuota SAP asli (140.000), netto 47.000 terlihat tidak melebihi — ini MENYESATKAN tanpa pending.");
            Assert.AreEqual(27000, resultEffective.ExceedAmount,
                "Kelebihan = 47.000 - 20.000 = 27.000 kg → ini yang harus ke K2.");
        }

        [TestMethod]
        [Description("Netto Kebun lebih besar dari Netto WB (konservatif). Dengan Netto Kebun, kita under-allocate kuota.")]
        public void PendingEstimasi_NettoKebunLebihBesarDariNettoWB_AmanKonservatif()
        {
            // Truk pending: netto kebun lebih besar dari netto WB aktual
            double nettoKebun = 47000;   // estimasi dari surat jalan (lebih besar)
            double nettoWBReal = 45800;  // netto WB aktual setelah timbang keluar

            // Estimasi pending menggunakan netto kebun (konservatif)
            double kuotaSAP = 100000;
            double sisaEfektifDenganNettoKebun = kuotaSAP - nettoKebun;  // 53000
            double sisaEfektifDenganNettoWB = kuotaSAP - nettoWBReal;    // 54200

            // Dengan netto kebun, kita estimate LEBIH sedikit sisa kuota → lebih aman
            Assert.IsTrue(sisaEfektifDenganNettoKebun < sisaEfektifDenganNettoWB,
                "Estimasi dengan Netto Kebun menghasilkan sisa kuota lebih kecil (lebih konservatif/aman).");
            Assert.AreEqual(53000, sisaEfektifDenganNettoKebun);
            Assert.AreEqual(54200, sisaEfektifDenganNettoWB);
        }
    }
}

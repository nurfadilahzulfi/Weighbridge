using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class SplitContractAllocationTests
    {
        [TestMethod]
        public void AllocateSplitContract_NettoWithinSingleLine_AllocatesSingleLineOnly()
        {
            // Kasus: Muatan 15.000 Kg, Line 0 memiliki sisa 20.000 Kg
            double totalNetto = 15000;
            var lines = new List<POLineItem>
            {
                new POLineItem { LineNum = 0, ItemCode = "CPO", OpenQty = 20000 }
            };

            var result = WeighbridgeCalculator.AllocateSplitContract(totalNetto, lines);

            Assert.IsTrue(result.IsFullyCovered);
            Assert.IsFalse(result.IsMultiLineSplit, "Tidak perlu split multi-baris jika kuota baris pertama cukup.");
            Assert.AreEqual(1, result.Allocations.Count);
            Assert.AreEqual(15000, result.Allocations[0].AllocatedQty);
            Assert.AreEqual(5000, result.Allocations[0].RemainingOpenQty);
            Assert.AreEqual(0, result.UnallocatedNetto);
        }

        [TestMethod]
        public void AllocateSplitContract_NettoExceedsFirstLine_SplitsAcrossMultipleLines()
        {
            // Kasus: Muatan 28.000 Kg.
            // Line 0 sisa 10.000 Kg
            // Line 1 sisa 25.000 Kg (di bawah kontrak yang sama)
            double totalNetto = 28000;
            var lines = new List<POLineItem>
            {
                new POLineItem { LineNum = 0, ItemCode = "CPO", OpenQty = 10000 },
                new POLineItem { LineNum = 1, ItemCode = "CPO", OpenQty = 25000 }
            };

            var result = WeighbridgeCalculator.AllocateSplitContract(totalNetto, lines);

            Assert.IsTrue(result.IsFullyCovered, "Seluruh muatan harus ter-cover oleh gabungan line kontrak.");
            Assert.IsTrue(result.IsMultiLineSplit, "Harus split multi-baris otomatis.");
            Assert.AreEqual(2, result.Allocations.Count);

            // Line 0 dialokasikan penuh
            Assert.AreEqual(0, result.Allocations[0].LineNum);
            Assert.AreEqual(10000, result.Allocations[0].AllocatedQty);
            Assert.AreEqual(0, result.Allocations[0].RemainingOpenQty);

            // Line 1 dialokasikan sisa muatan (18.000 Kg)
            Assert.AreEqual(1, result.Allocations[1].LineNum);
            Assert.AreEqual(18000, result.Allocations[1].AllocatedQty);
            Assert.AreEqual(7000, result.Allocations[1].RemainingOpenQty);

            Assert.AreEqual(28000, result.TotalAllocated);
            Assert.AreEqual(0, result.UnallocatedNetto);
        }

        [TestMethod]
        public void AllocateSplitContract_NettoExceedsAllContractLines_AllocatesAllAndFlagsRemaining()
        {
            // Kasus ekstrem: Muatan 50.000 Kg, tetapi total kuota kontrak di seluruh line hanya 35.000 Kg
            double totalNetto = 50000;
            var lines = new List<POLineItem>
            {
                new POLineItem { LineNum = 0, ItemCode = "PK", OpenQty = 15000 },
                new POLineItem { LineNum = 1, ItemCode = "PK", OpenQty = 20000 }
            };

            var result = WeighbridgeCalculator.AllocateSplitContract(totalNetto, lines);

            Assert.IsFalse(result.IsFullyCovered, "Total kuota kontrak tidak cukup meng-cover muatan.");
            Assert.AreEqual(35000, result.TotalAllocated);
            Assert.AreEqual(15000, result.UnallocatedNetto);
            Assert.AreEqual(2, result.Allocations.Count);
        }

        [TestMethod]
        public void AllocateSplitContract_ZeroOrEmptyLines_HandlesGracefully()
        {
            var result = WeighbridgeCalculator.AllocateSplitContract(10000, new List<POLineItem>());

            Assert.IsFalse(result.IsFullyCovered);
            Assert.AreEqual(10000, result.UnallocatedNetto);
            Assert.AreEqual(0, result.Allocations.Count);
        }

        [TestMethod]
        public void AllocateSplitContract_ThreeLinesUnderSameContract_CascadesAcrossThreeLines()
        {
            // Muatan 45.000 Kg
            // Line 0: sisa 10.000 Kg
            // Line 1: sisa 20.000 Kg
            // Line 2: sisa 30.000 Kg
            double totalNetto = 45000;
            var lines = new List<POLineItem>
            {
                new POLineItem { LineNum = 0, ItemCode = "CPO", OpenQty = 10000 },
                new POLineItem { LineNum = 1, ItemCode = "CPO", OpenQty = 20000 },
                new POLineItem { LineNum = 2, ItemCode = "CPO", OpenQty = 30000 }
            };

            var result = WeighbridgeCalculator.AllocateSplitContract(totalNetto, lines);

            Assert.IsTrue(result.IsFullyCovered);
            Assert.IsTrue(result.IsMultiLineSplit);
            Assert.AreEqual(3, result.Allocations.Count);

            // Line 0: 10.000
            Assert.AreEqual(10000, result.Allocations[0].AllocatedQty);
            Assert.AreEqual(0, result.Allocations[0].RemainingOpenQty);

            // Line 1: 20.000
            Assert.AreEqual(20000, result.Allocations[1].AllocatedQty);
            Assert.AreEqual(0, result.Allocations[1].RemainingOpenQty);

            // Line 2: sisa 15.000 (dari 30.000)
            Assert.AreEqual(15000, result.Allocations[2].AllocatedQty);
            Assert.AreEqual(15000, result.Allocations[2].RemainingOpenQty);

            Assert.AreEqual(45000, result.TotalAllocated);
            Assert.AreEqual(0, result.UnallocatedNetto);
        }

        [TestMethod]
        public void AllocateSplitContract_NegativeOrZeroNetto_ReturnsZeroAllocated()
        {
            var lines = new List<POLineItem>
            {
                new POLineItem { LineNum = 0, ItemCode = "CPO", OpenQty = 10000 }
            };

            var resultZero = WeighbridgeCalculator.AllocateSplitContract(0, lines);
            Assert.IsTrue(resultZero.IsFullyCovered);
            Assert.AreEqual(0, resultZero.TotalAllocated);
            Assert.AreEqual(0, resultZero.Allocations.Count);

            var resultNeg = WeighbridgeCalculator.AllocateSplitContract(-500, lines);
            Assert.IsTrue(resultNeg.IsFullyCovered);
            Assert.AreEqual(0, resultNeg.TotalAllocated);
        }

        [TestMethod]
        public void AllocateSplitContract_NullLines_HandlesGracefullyWithoutException()
        {
            var result = WeighbridgeCalculator.AllocateSplitContract(15000, null);

            Assert.IsFalse(result.IsFullyCovered);
            Assert.AreEqual(15000, result.UnallocatedNetto);
            Assert.AreEqual(0, result.Allocations.Count);
        }

        [TestMethod]
        public void AllocateSplitContract_LinesWithZeroOpenQty_SkippedToNextLineWithOpenQty()
        {
            // Line 0 kuota sudah habis (0), Line 1 masih ada 25.000
            var lines = new List<POLineItem>
            {
                new POLineItem { LineNum = 0, ItemCode = "CPO", OpenQty = 0 },
                new POLineItem { LineNum = 1, ItemCode = "CPO", OpenQty = 25000 }
            };

            var result = WeighbridgeCalculator.AllocateSplitContract(12000, lines);

            Assert.IsTrue(result.IsFullyCovered);
            Assert.IsFalse(result.IsMultiLineSplit);
            Assert.AreEqual(1, result.Allocations.Count);
            Assert.AreEqual(1, result.Allocations[0].LineNum);
            Assert.AreEqual(12000, result.Allocations[0].AllocatedQty);
            Assert.AreEqual(13000, result.Allocations[0].RemainingOpenQty);
        }

        #region Split Contract 2 PO Weight and Quota Calculation Tests

        [TestMethod]
        public void CalculateSplitBySPB_SplitsCorrectlyAndCalculatesBruto()
        {
            // Netto SPB total 25.000, SPB K1 dari supplier 15.000, Tara Kebun 8.000
            var res = WeighbridgeCalculator.CalculateSplitBySPB(25000, 15000, 8000);

            Assert.IsTrue(res.NeedsSplit);
            Assert.AreEqual(15000, res.NettoK1, "Netto K1 = Netto SPB K1.");
            Assert.AreEqual(23000, res.BrutoK1, "Bruto K1 = Netto K1 + Tara Kebun.");
            Assert.AreEqual(10000, res.NettoK2, "Netto K2 = Netto SPB Total - Netto K1.");
            Assert.AreEqual(18000, res.BrutoK2, "Bruto K2 = Netto K2 + Tara Kebun.");
        }

        [TestMethod]
        public void CalculateSplitBySPB_K1SamaAtauLebihDariTotal_Ditolak()
        {
            var sama = WeighbridgeCalculator.CalculateSplitBySPB(12000, 12000, 8000);
            Assert.IsFalse(sama.NeedsSplit);
            StringAssert.Contains(sama.Error, "lebih kecil");

            var lebih = WeighbridgeCalculator.CalculateSplitBySPB(12000, 15000, 8000);
            Assert.IsFalse(lebih.NeedsSplit);
        }

        [TestMethod]
        public void CalculateSplitBySPB_ZeroOrNegativeNetto_ReturnsEmptyResult()
        {
            var resZero = WeighbridgeCalculator.CalculateSplitBySPB(0, 5000, 8000);
            Assert.IsFalse(resZero.NeedsSplit);
            Assert.AreEqual(0, resZero.NettoK1);

            var resNeg = WeighbridgeCalculator.CalculateSplitBySPB(-100, 5000, 8000);
            Assert.IsFalse(resNeg.NeedsSplit);
            Assert.AreEqual(0, resNeg.NettoK1);
        }

        [TestMethod]
        public void ValidateK2Quota_SufficientQuota_ReturnsValidAndCalculatesRemaining()
        {
            // Kekurangan dari K1 = 10.000 Kg
            // Sisa Kuota PO Kontrak 2 = 30.000 Kg
            // Kuota K2 mencukupi: Sisa kuota K2 akhir = 30.000 - 10.000 = 20.000 Kg
            double neededK2 = 10000;
            double availableK2 = 30000;

            var val = WeighbridgeCalculator.ValidateK2Quota(neededK2, availableK2);

            Assert.IsTrue(val.IsValid);
            Assert.IsTrue(val.IsSufficient);
            Assert.AreEqual(10000, val.RequiredQty);
            Assert.AreEqual(30000, val.AvailableQuota);
            Assert.AreEqual(20000, val.RemainingQuotaAfterDeduction, "Sisa kuota akhir K2 = Awal - Dibutuhkan");
            Assert.AreEqual(0, val.Shortage);
        }

        [TestMethod]
        public void ValidateK2Quota_ExactQuota_ReturnsValidAndZeroRemaining()
        {
            double neededK2 = 10000;
            double availableK2 = 10000;

            var val = WeighbridgeCalculator.ValidateK2Quota(neededK2, availableK2);

            Assert.IsTrue(val.IsValid);
            Assert.IsTrue(val.IsSufficient);
            Assert.AreEqual(0, val.RemainingQuotaAfterDeduction);
            Assert.AreEqual(0, val.Shortage);
        }

        [TestMethod]
        public void ValidateK2Quota_InsufficientQuota_FlagsShortageAndReturnsInvalid()
        {
            // Kasus kuota K2 tidak cukup:
            // Kekurangan dari K1 = 12.000 Kg
            // Sisa Kuota PO Kontrak 2 = 5.000 Kg (kurang 7.000 Kg)
            double neededK2 = 12000;
            double availableK2 = 5000;

            var val = WeighbridgeCalculator.ValidateK2Quota(neededK2, availableK2);

            Assert.IsFalse(val.IsValid, "Harus tidak valid jika kuota PO K2 tidak mencukupi kekurangan K1.");
            Assert.IsFalse(val.IsSufficient);
            Assert.AreEqual(7000, val.Shortage);
            Assert.AreEqual(-7000, val.RemainingQuotaAfterDeduction);
            StringAssert.Contains(val.Message, "TIDAK MENCUKUPI");
        }

        [TestMethod]
        public void ValidateK2Quota_ZeroNeeded_ReturnsValidAndUnchangedQuota()
        {
            double neededK2 = 0;
            double availableK2 = 25000;

            var val = WeighbridgeCalculator.ValidateK2Quota(neededK2, availableK2);

            Assert.IsTrue(val.IsValid);
            Assert.IsTrue(val.IsSufficient);
            Assert.AreEqual(25000, val.RemainingQuotaAfterDeduction);
        }

        [TestMethod]
        public void CalculateSplitBySPB_KasusAsamJawa_K1MengikutiSPB()
        {
            // Kasus nyata Asam Jawa:
            // Total SPB Kebun = 28.750 Kg, Tara = 11.630 Kg
            // Sisa Kuota PO 1 di SAP = 4.140 Kg (tetapi SPB fisik vendor untuk K1 adalah 3.750 Kg)
            // Sisa Kuota PO 2 di SAP = 25.000 Kg
            double totalNettoKebun = 28750;
            double sisaKuotaK1 = 4140;
            double taraTruck = 11630;
            double manualNettoK1 = 3750; // Input manual operator dari lembar SPB 1

            var result = WeighbridgeCalculator.CalculateSplitBySPB(totalNettoKebun, manualNettoK1, taraTruck);

            Assert.IsTrue(result.NeedsSplit);
            Assert.IsTrue(result.NettoK1 <= sisaKuotaK1, "SPB K1 masih dalam sisa kuota K1.");
            Assert.AreEqual(3750, result.NettoK1, "K1 harus mengikuti referensi SPB manual operator.");
            Assert.AreEqual(15380, result.BrutoK1, "Bruto K1 = 3.750 + 11.630 = 15.380");
            Assert.AreEqual(25000, result.NettoK2, "K2 harus menerima sisa muatan SPB: 28.750 - 3.750 = 25.000");
            Assert.AreEqual(36630, result.BrutoK2, "Bruto K2 = 25.000 + 11.630 = 36.630");

            var valK2 = WeighbridgeCalculator.ValidateK2Quota(result.NettoK2, 25000);
            Assert.IsTrue(valK2.IsValid, "Kuota K2 25.000 kg mencukupi alokasi SPB 25.000 kg.");
        }

        [TestMethod]
        public void CalculateSplitBySPB_SPBK1Kosong_DitolakTanpaDefaultKuota()
        {
            // SPB K1 wajib diisi dari SPB supplier; tidak ada lagi K1 otomatis = sisa kuota SAP
            var result = WeighbridgeCalculator.CalculateSplitBySPB(28750, 0, 11630);

            Assert.IsFalse(result.NeedsSplit);
            Assert.AreEqual(0, result.NettoK1);
            StringAssert.Contains(result.Error, "SPB K1 belum diisi");
        }

        [TestMethod]
        public void FlowSplitContract_WeighInManualSPB_WeighOutWithShrinkage_CalculatesAccurately()
        {
            // Alur Lengkap:
            // 1. Timbang Masuk: Total SPB Kebun = 28.750 Kg, Ref SPB K1 = 3.750 Kg, Ref SPB K2 = 25.000 Kg
            double spbNettoK1 = 3750;
            double spbNettoK2 = 25000;

            // 2. Timbang Keluar: Total Netto Real WB = 28.730 Kg (Terjadi Susut 20 Kg)
            double totalNettoWB = 28730;

            var splitKeluar = WeighbridgeCalculator.CalculateSplitGRPONetto(totalNettoWB, spbNettoK1, spbNettoK2);

            // K1 tetap mendapatkan 3.750 Kg sesuai SPB
            Assert.AreEqual(3750, splitKeluar.NettoWB_K1);

            // K2 menyerap susut 20 Kg: 25.000 - 20 = 24.980 Kg
            Assert.AreEqual(24980, splitKeluar.NettoWB_K2);

            // Total GRPO = 3.750 + 24.980 = 28.730 Kg (100% klop dengan timbangan fisik WB)
            Assert.AreEqual(28730, splitKeluar.TotalNettoWB);
            Assert.IsFalse(splitKeluar.K2ExceedsPlanned, "K2 tidak melebihi rencana karena mengalami susut.");
        }

        #endregion
    }
}

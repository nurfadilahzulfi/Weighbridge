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
    }
}

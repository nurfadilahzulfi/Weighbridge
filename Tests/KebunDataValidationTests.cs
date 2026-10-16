using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class KebunDataValidationTests
    {
        // Kasus AS2610030005: SPB asli Bruto 16910, Tara 4810, Netto 12100 diinput tertukar
        // menjadi Tara 12100 / Netto 4810. Tara WB (berat keluar) = 4870.
        [TestMethod]
        public void IsTaraNettoKebunSwapped_SwappedInput_Detected()
        {
            Assert.IsTrue(WeighbridgeCalculator.IsTaraNettoKebunSwapped(12100, 4810, 4870));
        }

        [TestMethod]
        public void IsTaraNettoKebunSwapped_CorrectInput_NotDetected()
        {
            Assert.IsFalse(WeighbridgeCalculator.IsTaraNettoKebunSwapped(4810, 12100, 4870));
        }

        [TestMethod]
        public void IsTaraNettoKebunSwapped_SmallLoadTaraGreaterThanNetto_NotDetected()
        {
            // Muatan kecil yang sah: tara memang > netto, tapi tara kebun dekat dengan tara WB
            Assert.IsFalse(WeighbridgeCalculator.IsTaraNettoKebunSwapped(11000, 5000, 11050));
        }

        [TestMethod]
        public void IsTaraNettoKebunSwapped_SplitRecordK1_NotDetected()
        {
            // Record K1 split yang benar: netto K1 kecil (alokasi kuota), tara = tara truk
            Assert.IsFalse(WeighbridgeCalculator.IsTaraNettoKebunSwapped(11630, 4140, 11680));
        }

        [TestMethod]
        public void IsTaraNettoKebunSwapped_SplitTotalSwapped_Detected()
        {
            // Kasus AS2610030001: tara kebun tersimpan 28750, total netto SPB K1+K2 = 4140 + 7490, tara WB 11680
            Assert.IsTrue(WeighbridgeCalculator.IsTaraNettoKebunSwapped(28750, 4140 + 7490, 11680));
        }

        [TestMethod]
        public void IsTaraNettoKebunSwapped_MissingValues_NotDetected()
        {
            Assert.IsFalse(WeighbridgeCalculator.IsTaraNettoKebunSwapped(0, 4810, 4870));
            Assert.IsFalse(WeighbridgeCalculator.IsTaraNettoKebunSwapped(12100, 0, 4870));
            Assert.IsFalse(WeighbridgeCalculator.IsTaraNettoKebunSwapped(12100, 4810, 0));
        }

        [TestMethod]
        public void RequiresSplitKontrak_NettoAboveRemainingQuota_True()
        {
            // PO 261000312 hanya tersisa 2.470 kg, netto SPB 12.100 kg -> wajib split
            Assert.IsTrue(WeighbridgeCalculator.RequiresSplitKontrak(12100, 2470));
        }

        [TestMethod]
        public void RequiresSplitKontrak_NettoWithinQuota_False()
        {
            Assert.IsFalse(WeighbridgeCalculator.RequiresSplitKontrak(12100, 12100));
            Assert.IsFalse(WeighbridgeCalculator.RequiresSplitKontrak(12100, 50000));
            Assert.IsFalse(WeighbridgeCalculator.RequiresSplitKontrak(0, 0));
        }

        [TestMethod]
        public void CheckPOQuota_OverReceiptBeyondTolerance_Exceeded()
        {
            // Kasus AS2610030001: GRPO 28.730 kg ke PO K2 dengan sisa 25.000 kg
            var res = WeighbridgeCalculator.CheckPOQuota(28730, 25000, WeighbridgeCalculator.GRPOOverReceiptTolerancePercent);
            Assert.IsTrue(res.IsExceeded);
        }

        [TestMethod]
        public void CheckPOQuota_SmallSurplusWithinTolerance_NotExceeded()
        {
            // Surplus 30 kg dari 12.100 kg masih dalam toleransi 1%
            var res = WeighbridgeCalculator.CheckPOQuota(12130, 12100, WeighbridgeCalculator.GRPOOverReceiptTolerancePercent);
            Assert.IsFalse(res.IsExceeded);
        }
    }
}

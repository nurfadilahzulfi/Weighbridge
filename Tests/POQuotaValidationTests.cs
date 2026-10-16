using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class POQuotaValidationTests
    {
        [TestMethod]
        public void CheckPOQuota_WeightWithinQuota_ReturnsValidAndNotExceeded()
        {
            double grossWeight = 25000;
            double remainingQuota = 30000;

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.IsExceeded);
            Assert.AreEqual(0, result.ExceedAmount);
            Assert.AreEqual(0, result.ExceedPercentage);
        }

        [TestMethod]
        public void CheckPOQuota_ZeroRemainingQuota_ReturnsInvalidAndExceeded()
        {
            double grossWeight = 20000;
            double remainingQuota = 0; // Kuota PO sudah habis

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota);

            Assert.IsFalse(result.IsValid, "Jika kuota habis (0), status validasi harus false.");
            Assert.IsTrue(result.IsExceeded);
            Assert.IsTrue(result.Message.Contains("habis") || result.Message.Contains("0"));
        }

        [TestMethod]
        public void CheckPOQuota_WeightExceedsQuota_CalculatesExceedAmountAndPercentage()
        {
            double grossWeight = 32000;
            double remainingQuota = 25000;

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota);

            Assert.IsTrue(result.IsExceeded);
            Assert.AreEqual(7000, result.ExceedAmount);
            Assert.AreEqual(28.0, result.ExceedPercentage);
            Assert.IsTrue(result.Message.Contains("melebihi"));
        }

        [TestMethod]
        public void CheckPOQuota_WithTolerance_WithinToleranceDoesNotFlagExceeded()
        {
            double grossWeight = 10200;
            double remainingQuota = 10000;
            double tolerancePercent = 5.0; // toleransi 5% = max 10,500

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota, tolerancePercent);

            Assert.IsFalse(result.IsExceeded);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void CheckPOQuota_WithTolerance_ExceedingToleranceFlagsExceeded()
        {
            double grossWeight = 10800;
            double remainingQuota = 10000;
            double tolerancePercent = 5.0; // toleransi 5% = max 10,500 -> 10,800 is exceeded

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota, tolerancePercent);

            Assert.IsTrue(result.IsExceeded);
            Assert.AreEqual(800, result.ExceedAmount);
        }

        [TestMethod]
        public void CheckPOQuota_WeightExactlyEqualsQuota_ReturnsValidAndNotExceeded()
        {
            double grossWeight = 20000;
            double remainingQuota = 20000;

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota);

            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.IsExceeded);
            Assert.AreEqual(0, result.ExceedAmount);
            Assert.AreEqual(0, result.ExceedPercentage);
        }

        [TestMethod]
        public void CheckPOQuota_NegativeRemainingQuota_ReturnsInvalidAndExceeded()
        {
            double grossWeight = 5000;
            double remainingQuota = -100; // Kuota negatif (overdrawn)

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.IsExceeded);
            Assert.AreEqual(grossWeight, result.ExceedAmount);
        }

        [TestMethod]
        public void CheckPOQuota_SmallRemainingQuota_FlagsExceededAndInvalid()
        {
            double grossWeight = 40000; // 40 ton
            double remainingQuota = 420; // 420 kg

            var result = WeighbridgeCalculator.CheckPOQuota(grossWeight, remainingQuota);

            Assert.IsFalse(result.IsValid, "Berat melebihi sisa kuota kecil harus invalid.");
            Assert.IsTrue(result.IsExceeded);
            Assert.AreEqual(39580, result.ExceedAmount);
            Assert.IsTrue(result.Message.Contains("40,000 kg") || result.Message.Contains("40.000 kg"));
            Assert.IsTrue(result.Message.Contains("420 kg"));
        }

        [TestMethod]
        public void POQuotaThreshold_Below55000Kg_IsFlaggedAsInsufficient()
        {
            double remainingQuota = 54999;
            bool isInsufficient = remainingQuota < DocumentLookupFilter.MinimumPOQuotaKg;

            Assert.IsTrue(isInsufficient, "Sisa kuota 54.999 kg harus dianggap tidak mencukupi batas minimum 55.000 kg.");
        }

        [TestMethod]
        public void POQuotaThreshold_AtOrAbove55000Kg_IsSufficient()
        {
            double remainingQuotaExact = 55000;
            double remainingQuotaAbove = 65000;

            Assert.IsFalse(remainingQuotaExact < DocumentLookupFilter.MinimumPOQuotaKg, "Sisa kuota 55.000 kg memenuhi batas minimum.");
            Assert.IsFalse(remainingQuotaAbove < DocumentLookupFilter.MinimumPOQuotaKg, "Sisa kuota 65.000 kg memenuhi batas minimum.");
        }
    }
}

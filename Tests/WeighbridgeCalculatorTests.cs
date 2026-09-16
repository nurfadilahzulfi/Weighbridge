using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class WeighbridgeCalculatorTests
    {
        [TestMethod]
        public void ParseWeight_StandardIndicatorString_ExtractsCorrectWeight()
        {
            // Indicator standard: "+0025400 kg" or "ST,GS,+0025400kg"
            string raw = "ST,GS,+0025400kg";
            string result = WeighbridgeCalculator.ParseWeight(raw);
            Assert.AreEqual("25400", result);
        }

        [TestMethod]
        public void ParseWeight_WithDecimal_ExtractsDecimalCorrectly()
        {
            string raw = "WW,000150.50kg";
            string result = WeighbridgeCalculator.ParseWeight(raw);
            Assert.AreEqual("150.5", result);
        }

        [TestMethod]
        public void ParseWeight_EmptyOrWhitespace_ReturnsZero()
        {
            Assert.AreEqual("0", WeighbridgeCalculator.ParseWeight(""));
            Assert.AreEqual("0", WeighbridgeCalculator.ParseWeight("   "));
            Assert.AreEqual("0", WeighbridgeCalculator.ParseWeight(null));
        }

        [TestMethod]
        public void ParseWeight_NoDigitsFound_ReturnsZero()
        {
            string raw = "ERROR_NO_DATA";
            string result = WeighbridgeCalculator.ParseWeight(raw);
            Assert.AreEqual("0", result);
        }

        [TestMethod]
        public void CalculateNetto_MasukGreaterThanKeluar_ReturnsPositiveDifference()
        {
            double beratMasuk = 32500; // Truk berisi muatan
            double beratKeluar = 12000; // Truk kosong (tara)

            double netto = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            Assert.AreEqual(20500, netto);
        }

        [TestMethod]
        public void CalculateNetto_KeluarGreaterThanMasuk_ReturnsPositiveDifference()
        {
            double beratMasuk = 11500; // Truk kosong masuk pabrik
            double beratKeluar = 31500; // Truk keluar bermuatan

            double netto = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            Assert.AreEqual(20000, netto);
        }

        [TestMethod]
        public void CalculateNetto_EqualWeights_ReturnsZero()
        {
            double beratMasuk = 15000;
            double beratKeluar = 15000;

            double netto = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            Assert.AreEqual(0, netto);
        }

        [TestMethod]
        public void CalculateNetto_WithDecimals_RoundsToTwoDecimalPlaces()
        {
            double beratMasuk = 25430.856;
            double beratKeluar = 10200.222;

            double netto = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            // 25430.856 - 10200.222 = 15230.634 -> rounded to 2 decimals = 15230.63
            Assert.AreEqual(15230.63, netto);
        }
    }
}

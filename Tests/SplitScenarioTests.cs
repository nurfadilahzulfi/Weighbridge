using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    /// <summary>
    /// Contoh kasus split kontrak: Netto SPB 12.500 kg, Tara 4.950 kg, Netto real WB 12.450 kg (susut 50 kg).
    /// </summary>
    [TestClass]
    public class SplitScenarioTests
    {
        private const double NettoSPB = 12500;
        private const double Tara = 4950;
        private const double NettoWB = 12450;

        [TestMethod]
        public void Skenario1_K1DariSPBSupplier()
        {
            // Timbang masuk: operator isi "Netto SPB K1" = 2.500 (sisa kuota K1 cukup, mis. 3.000)
            var masuk = WeighbridgeCalculator.CalculateSplitBySPB(NettoSPB, 2500, Tara);
            Assert.AreEqual(2500, masuk.NettoK1);
            Assert.AreEqual(7450, masuk.BrutoK1);
            Assert.AreEqual(10000, masuk.NettoK2);
            Assert.AreEqual(14950, masuk.BrutoK2);

            // Timbang keluar: K1 dikunci, susut diserap K2
            var keluar = WeighbridgeCalculator.CalculateSplitGRPONetto(NettoWB, masuk.NettoK1, masuk.NettoK2);
            Assert.AreEqual(2500, keluar.NettoWB_K1);
            Assert.AreEqual(9950, keluar.NettoWB_K2);
            Assert.AreEqual(NettoWB, keluar.NettoWB_K1 + keluar.NettoWB_K2);
        }
    }
}

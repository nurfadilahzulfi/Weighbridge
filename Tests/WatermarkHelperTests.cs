using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Helpers;

namespace Weighbridge.Tests
{
    [TestClass]
    public class WatermarkHelperTests
    {
        [TestMethod]
        public void FitInside_LogoLebar_MenyesuaikanLebar()
        {
            Assert.AreEqual(new Size(400, 200), WatermarkHelper.FitInside(new Size(800, 400), 400));
        }

        [TestMethod]
        public void FitInside_LogoTinggi_MenyesuaikanTinggi()
        {
            Assert.AreEqual(new Size(150, 300), WatermarkHelper.FitInside(new Size(500, 1000), 300));
        }

        [TestMethod]
        public void FitInside_UkuranTidakValid_Kosong()
        {
            Assert.AreEqual(Size.Empty, WatermarkHelper.FitInside(new Size(0, 100), 300));
            Assert.AreEqual(Size.Empty, WatermarkHelper.FitInside(new Size(100, 100), 0));
        }
    }
}

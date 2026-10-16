using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Helpers;

namespace Weighbridge.Tests
{
    [TestClass]
    public class ResponsiveLayoutHelperTests
    {
        [TestMethod]
        public void ComputeScale_SameSize_ReturnsOne()
        {
            float font;
            SizeF s = ResponsiveLayoutHelper.ComputeScale(new Size(767, 668), new Size(767, 668), out font);
            Assert.AreEqual(1f, s.Width, 0.0001f);
            Assert.AreEqual(1f, s.Height, 0.0001f);
            Assert.AreEqual(1f, font, 0.0001f);
        }

        [TestMethod]
        public void ComputeScale_Maximized_ScalesEachAxisAndFontUsesSmallerAxis()
        {
            float font;
            SizeF s = ResponsiveLayoutHelper.ComputeScale(new Size(800, 600), new Size(1600, 900), out font);
            Assert.AreEqual(2f, s.Width, 0.0001f);
            Assert.AreEqual(1.5f, s.Height, 0.0001f);
            Assert.AreEqual(1.5f, font, 0.0001f);
        }

        [TestMethod]
        public void ComputeScale_SmallerThanDesign_NeverShrinks()
        {
            float font;
            SizeF s = ResponsiveLayoutHelper.ComputeScale(new Size(800, 600), new Size(400, 300), out font);
            Assert.AreEqual(1f, s.Width, 0.0001f);
            Assert.AreEqual(1f, s.Height, 0.0001f);
            Assert.AreEqual(1f, font, 0.0001f);
        }

        [TestMethod]
        public void ComputeScale_FontScaleRoundedToFiveHundredths()
        {
            float font;
            ResponsiveLayoutHelper.ComputeScale(new Size(1000, 1000), new Size(1234, 1234), out font);
            Assert.AreEqual(1.25f, font, 0.0001f);
        }

        [TestMethod]
        public void ScaleBounds_ScalesPositionAndSize()
        {
            Rectangle r = ResponsiveLayoutHelper.ScaleBounds(new Rectangle(544, 36, 212, 20), new SizeF(2f, 1.5f));
            Assert.AreEqual(new Rectangle(1088, 54, 424, 30), r);
        }

        [TestMethod]
        public void ScaleBounds_AdjacentControlsStayAdjacentAfterRounding()
        {
            var scale = new SizeF(1.37f, 1.21f);
            Rectangle a = ResponsiveLayoutHelper.ScaleBounds(new Rectangle(19, 346, 736, 285), scale);
            Rectangle b = ResponsiveLayoutHelper.ScaleBounds(new Rectangle(755, 631, 10, 10), scale);
            Assert.AreEqual(a.Right, b.Left);
            Assert.AreEqual(a.Bottom, b.Top);
        }
    }
}

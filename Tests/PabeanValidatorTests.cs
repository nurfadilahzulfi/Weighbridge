using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class PabeanValidatorTests
    {
        [TestMethod]
        public void Validate_AllFieldsProvided_ReturnsValid()
        {
            // Arrange
            string kode = "BC40";
            string nomor = "001234/BC/2026";
            bool hasTanggal = true;
            string customFormat = "yyyy-MM-dd";

            // Act
            var result = PabeanValidator.Validate(kode, nomor, hasTanggal, customFormat);

            // Assert
            Assert.IsTrue(result.IsValid, "Pabean data yang lengkap harus dianggap valid.");
            Assert.AreEqual(string.Empty, result.ErrorMessage);
        }

        [TestMethod]
        public void Validate_EmptyKodeDokumen_ReturnsInvalid()
        {
            // Arrange
            string kode = "";
            string nomor = "001234/BC/2026";
            bool hasTanggal = true;

            // Act
            var result = PabeanValidator.Validate(kode, nomor, hasTanggal);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Kode Dokumen"));
        }

        [TestMethod]
        public void Validate_WhitespaceKodeDokumen_ReturnsInvalid()
        {
            // Arrange
            string kode = "   ";
            string nomor = "001234/BC/2026";
            bool hasTanggal = true;

            // Act
            var result = PabeanValidator.Validate(kode, nomor, hasTanggal);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Kode Dokumen"));
        }

        [TestMethod]
        public void Validate_EmptyNomorDokumen_ReturnsInvalid()
        {
            // Arrange
            string kode = "BC20";
            string nomor = "";
            bool hasTanggal = true;

            // Act
            var result = PabeanValidator.Validate(kode, nomor, hasTanggal);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Nomor Dokumen"));
        }

        [TestMethod]
        public void Validate_UncheckedTanggal_ReturnsInvalid()
        {
            // Arrange
            string kode = "BC20";
            string nomor = "001234/BC/2026";
            bool hasTanggal = false;

            // Act
            var result = PabeanValidator.Validate(kode, nomor, hasTanggal);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Tanggal Dokumen"));
        }

        [TestMethod]
        public void Validate_CustomFormatBlankSpace_ReturnsInvalid()
        {
            // In WinForms, DateTimePicker with CustomFormat = " " signifies empty/cleared date
            string kode = "BC20";
            string nomor = "001234/BC/2026";
            bool hasTanggal = true;
            string customFormat = " ";

            // Act
            var result = PabeanValidator.Validate(kode, nomor, hasTanggal, customFormat);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Tanggal Dokumen"));
        }

        [TestMethod]
        public void Validate_NullKodeDokumen_ReturnsInvalid()
        {
            var result = PabeanValidator.Validate(null, "001234/BC/2026", true);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Kode Dokumen"));
        }

        [TestMethod]
        public void Validate_NullNomorDokumen_ReturnsInvalid()
        {
            var result = PabeanValidator.Validate("BC20", null, true);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Nomor Dokumen"));
        }
    }
}

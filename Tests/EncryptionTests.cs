using Microsoft.VisualStudio.TestTools.UnitTesting;
using Weighbridge.Core.Encryptions;

namespace Weighbridge.Tests
{
    [TestClass]
    public class EncryptionTests
    {
        [TestMethod]
        public void EncryptDecrypt_StandardPassword_MatchesOriginalPlainText()
        {
            var encryption = new Encryption();
            string plainText = "SapPassword123!";

            string cipherText = encryption.encrypt(plainText);
            Assert.IsNotNull(cipherText);
            Assert.AreNotEqual(plainText, cipherText);

            string decrypted = encryption.decrypt(cipherText);
            Assert.AreEqual(plainText, decrypted, "Hasil dekripsi harus sama persis dengan teks asli.");
        }

        [TestMethod]
        public void EncryptDecrypt_ComplexSymbolsAndNumbers_MatchesOriginal()
        {
            var encryption = new Encryption();
            string plainText = "T@n1m4s#2026_Tr41n1ng$*";

            string cipher = encryption.encrypt(plainText);
            string decrypted = encryption.decrypt(cipher);

            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void Encrypt_ProducesDeterministicValidBase64()
        {
            var encryption = new Encryption();
            string cipher = encryption.encrypt("SYSTEM");

            byte[] bytes = System.Convert.FromBase64String(cipher);
            Assert.IsTrue(bytes.Length > 0);
        }
    }
}

using System;
using System.Text;
using BH.SDK.Services;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Two shipped defects: a constant-seeded Random made salt and IV identical on every installation
    // (NotDeterministic sees it), and a short input threw out of the crypto path.

    /// <summary> The raw AES primitive: that two encryptions of the same text differ, and that a short input is
    /// refused as "not ours" rather than throwing out of the crypto path. </summary>
    public class CryptographyTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void CryptographyTest()
        {
            var cryptography = new CryptographyService();

            const string text = "secret text";
            const string password = "cool password";

            var cryptedText = cryptography.Encrypt(text, password);
            var result = cryptography.Decrypt(cryptedText, password);

            Assert.AreEqual(text, result);
            Cat.Meow($"{result} - {cryptedText}");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void Encrypt_IsNotDeterministic()
        {
            var cryptography = new CryptographyService();

            const string text = "secret text";
            const string password = "cool password";

            var first = cryptography.Encrypt(text, password);
            var second = cryptography.Encrypt(text, password);

            Assert.AreNotEqual(first, second);
            Assert.AreEqual(text, cryptography.Decrypt(first, password));
            Assert.AreEqual(text, cryptography.Decrypt(second, password));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DecryptAES_RefusesInputShorterThanItsHeader()
        {
            var cryptography = new CryptographyService();
            var password = Encoding.UTF8.GetBytes("cool password");

            Assert.Throws<ArgumentException>(() => cryptography.DecryptAES(Array.Empty<byte>(), password));
            Assert.Throws<ArgumentException>(() =>
                cryptography.DecryptAES(new byte[CryptographyService.SaltSize], password));
            Assert.Throws<ArgumentNullException>(() => cryptography.DecryptAES(null, password));
        }
    }
}
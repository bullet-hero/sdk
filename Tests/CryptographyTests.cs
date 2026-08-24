using System;
using System.Text;
using BH.SDK.Services;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Two of these pin defects that were live in the shipped class rather than behaviour anyone
    // asked for, and both were invisible from the round trip alone:
    //
    //   The salt and the IV came out of a constant-seeded System.Random, so every installation drew
    //   the same ones in the same order. Encrypting the same text twice produced identical bytes -
    //   which is what NotDeterministic below refuses, and it is the only assert that can see it.
    //
    //   A short input computed a negative array length and threw out of the crypto path, so "this
    //   file is not ours" arrived as "the cipher broke".
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
        [Category(Metadata.Category.Hard)]
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
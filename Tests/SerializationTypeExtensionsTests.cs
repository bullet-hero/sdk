using BH.SDK.Serialization.Serializers;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    public class SerializationTypeExtensionsTests
    {
        [TestCase(SerializationType.Json, ".json")]
        [TestCase(SerializationType.Blob, ".blob")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ToFileExtension_ReturnsExpectedExtension(SerializationType type, string expected)
        {
            Assert.AreEqual(expected, type.ToFileExtension());
        }

        [TestCase(".json", SerializationType.Json)]
        [TestCase(".JSON", SerializationType.Json)]
        [TestCase(".blob", SerializationType.Blob)]
        [TestCase(".BLOB", SerializationType.Blob)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TryFromFileExtension_ParsesKnownExtensions(string extension, SerializationType expected)
        {
            var result = SerializationTypeExtensions.TryFromFileExtension(extension, out var type);
            Assert.IsTrue(result);
            Assert.AreEqual(expected, type);
        }

        [TestCase(".txt")]
        [TestCase("")]
        [TestCase(".jsonx")]
        // The retired one. Its number is never reissued and its extension resolves to nothing.
        [TestCase(".bson")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TryFromFileExtension_RejectsUnknownExtensions(string extension)
        {
            var result = SerializationTypeExtensions.TryFromFileExtension(extension, out _);
            Assert.IsFalse(result);
        }

        // Two members are retired and neither number is ever reissued: 1 was Bson, 2 was JsonPretty.
        // A settings file in the wild still holds one of them, and what it must NOT do is quietly
        // mean whatever took the slot - so the numbers stay vacant and the values stay undefined.
        [TestCase(1)]
        [TestCase(2)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ARetiredNumber_IsNotReissued(byte number)
        {
            Assert.IsFalse(System.Enum.IsDefined(typeof(SerializationType), (SerializationType)number));
        }
    }
}
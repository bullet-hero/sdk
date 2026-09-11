using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Text;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using BH.SDK.Utils;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // EVERY CASE HERE IS A DEFECT THAT WAS MEASURED, not a shape imagined for completeness. An int
    // override never applied at all, and an enum and an id stopped applying once they had been
    // through the serializer, because Modification.Value widens an int to a long on assignment and
    // the old apply path guarded its write with IsAssignableFrom. The values below are taken the
    // way production takes them - out of a Modification that has been serialized and read back -
    // rather than hand-built, so a case cannot pass for a shape the format never actually produces.

    /// <summary> ModificationValues.TryConvert against the shapes an override arrives in after the serializer. </summary>
    public class ModificationValuesTests
    {
        private static readonly ObjectId Id = new(1);

        /// <summary> A value as the apply path meets it: written and read back by the format's own serializer. </summary>
        private static object Serialized(object value)
        {
            var serializer = new SerializationService().Serializer;
            var source = new Modification(new ModificationKey(Id, ModificationFields.Layer), value);

            using var text = new StringWriter(CultureInfo.InvariantCulture);
            serializer.Serialize(text, source);

            using var reader = new JsonTextReader(new StringReader(text.ToString()));
            return serializer.Deserialize<Modification>(reader).Value;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // The case that needed no file at all: the setter widens the 7 at construction, so the
        // override was unusable before anything was written anywhere.
        public void ALong_BecomesTheIntItWas()
        {
            var value = new Modification(new ModificationKey(Id, ModificationFields.Layer), 7).Value;
            Assert.IsInstanceOf<long>(value, "Modification.Value stopped widening - this test is moot.");

            Assert.IsTrue(ModificationValues.TryConvert<int>(value, out var converted));
            Assert.AreEqual(7, converted);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ANumber_BecomesTheEnumItWas()
        {
            var value = Serialized(TextObjectHorizontalAlignment.Right);

            Assert.IsTrue(ModificationValues.TryConvert<TextObjectHorizontalAlignment>(value, out var converted));
            Assert.AreEqual(TextObjectHorizontalAlignment.Right, converted);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ASerializedId_BecomesTheIdStructItWas()
        {
            var shape = new ShapeId(Guid.NewGuid());
            var value = Serialized(shape);

            Assert.IsTrue(ModificationValues.TryConvert<ShapeId>(value, out var converted));
            Assert.AreEqual(shape, converted);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AToken_BecomesTheModelItWas()
        {
            var key = new PosKey { Frame = 3 };
            var value = Serialized(key);

            Assert.IsTrue(ModificationValues.TryConvert<PosKey>(value, out var converted));
            Assert.AreEqual(3, converted.Frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        // The whole-track override - the shape that made Value untyped in the first place.
        public void AnArray_BecomesTheListItWas()
        {
            var track = new List<PosKey> { new() { Frame = 1 }, new() { Frame = 5 } };
            var value = Serialized(track);

            Assert.IsTrue(ModificationValues.TryConvert<List<PosKey>>(value, out var converted));
            Assert.AreEqual(2, converted.Count);
            Assert.AreEqual(5, converted[1].Frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // A value already of the right type takes the fast path, which is every override applied in
        // the session that recorded it - the case worth not paying the serializer for.
        public void AValueOfTheRightType_PassesStraightThrough()
        {
            var track = new List<PosKey> { new() { Frame = 2 } };

            Assert.IsTrue(ModificationValues.TryConvert<List<PosKey>>(track, out var converted));
            Assert.AreSame(track, converted);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // It answers false rather than throwing: a failed conversion IS the caller's answer, and an
        // override that does not land is not an exceptional condition at that layer.
        public void AWrongShape_AnswersFalseWithoutThrowing()
        {
            Assert.IsFalse(ModificationValues.TryConvert<int>("not a number", out var number));
            Assert.AreEqual(0, number);

            Assert.IsFalse(ModificationValues.TryConvert<PosKey>(7L, out var key));
            Assert.IsNull(key);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // Null is a corrupt override rather than a legal state - Modification.Value carries
        // [RuleNotNull] - so it is refused rather than written through as a default.
        public void Null_AnswersFalse()
        {
            Assert.IsFalse(ModificationValues.TryConvert<int>(null, out _));
            Assert.IsFalse(ModificationValues.TryConvert<PosKey>(null, out _));
        }
    }
}

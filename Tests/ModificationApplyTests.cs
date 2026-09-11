using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Text;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Utils;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // WHETHER AN OVERRIDE ACTUALLY LANDS, ASKED PER KIND OF FIELD. Everything that existed tested
    // one half or the other: BlobCodecTests and ModelUtilsTests build a Modification and assert it
    // ROUND TRIPS, never that it applies. Nothing drove a scalar through Apply at all.
    //
    // WHAT IT FOUND, AND WHAT FIXED IT. Seven kinds landed and four did not. An int never landed at
    // all - Modification.Value's setter widens every integral to long so an override still equals
    // itself after a round trip, and the old Apply guarded its write with
    // PropertyType.IsAssignableFrom(value type), which is false for int from long. An enum and an
    // id stopped landing once they had been through the serializer, where both arrive as a number.
    // ModificationValues.TryConvert is the fix: it converts back through the very serializer that
    // gave the value its shape, instead of refusing anything that is not already the right type.
    //
    // The four cases that read "DoesNotApply" while that was true are "Applies" below, and the
    // three kinds this file used to leave uncovered after a round trip - a string, a FrameSpan and
    // a keyframe list - are covered now. They were a deliberate gap while the question was "what
    // does the serializer change", since nothing changes theirs; with a conversion in the path the
    // question became "does the conversion preserve what it need not change", which is worth asking.

    /// <summary> ModificationUtils.Apply against one field of each kind - scalar, enum, id, struct,
    /// reference and whole list - in memory and after a serializer round trip. </summary>
    public class ModificationApplyTests
    {
        private static readonly ObjectId Id = new(1);

        private static void Apply(RectObject obj, int field, object value)
            => obj.Apply(new Modification(new ModificationKey(obj.ObjectId, field), value));

        private static void ApplyRoundTripped(RectObject obj, int field, object value)
            => obj.Apply(RoundTrip(new Modification(new ModificationKey(obj.ObjectId, field), value)));

        // What deserialization does to a value, without a file: the same serializer the format runs
        // on, writing and reading the override back. An enum, an id and a struct all arrive as
        // something else on the far side, which is the half Modification.Value's setter cannot show.
        private static Modification RoundTrip(Modification source)
        {
            var serializer = new SerializationService().Serializer;

            using var text = new StringWriter(CultureInfo.InvariantCulture);
            serializer.Serialize(text, source);

            using var reader = new JsonTextReader(new StringReader(text.ToString()));
            return serializer.Deserialize<Modification>(reader);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AStringField_Applies()
        {
            var obj = new RectObject { ObjectId = Id, Name = "template" };
            Apply(obj, ModificationFields.Name, "overridden");

            Assert.AreEqual("overridden", obj.Name);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ABoolField_Applies()
        {
            var obj = new RectObject { ObjectId = Id, Active = true };
            Apply(obj, ModificationFields.Active, false);

            Assert.IsFalse(obj.Active);
        }

        // THE CASE THE WHOLE ITEM TURNED ON, and the one that needed no file to show: Layer is an
        // int and Modification.Value's setter makes the 7 a long before anything is written
        // anywhere. It applies because the conversion asks the serializer, not the type system.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnIntField_Applies()
        {
            var obj = new RectObject { ObjectId = Id, Layer = 1 };
            Apply(obj, ModificationFields.Layer, 7);

            Assert.AreEqual(7, obj.Layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AStructField_Applies()
        {
            var obj = new RectObject { ObjectId = Id, Span = new FrameSpan(1, 10) };
            Apply(obj, ModificationFields.Span, new FrameSpan(5, 20));

            Assert.AreEqual(new FrameSpan(5, 20), obj.Span);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnIdField_Applies()
        {
            var obj = new ShapeObject { ObjectId = Id };
            var replacement = new ShapeId(Guid.NewGuid());
            Apply(obj, ModificationFields.ShapeId, replacement);

            Assert.AreEqual(replacement, obj.ShapeId);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnEnumField_Applies()
        {
            var obj = new TextObject { ObjectId = Id };
            Apply(obj, ModificationFields.HorizontalAlignment, TextObjectHorizontalAlignment.Right);

            Assert.AreEqual(TextObjectHorizontalAlignment.Right, obj.HorizontalAlignment);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AWholeTrack_Applies()
        {
            var obj = new RectObject { ObjectId = Id };
            obj.Positions.Add(new PosKey(new Vector2Value(0f, 0f), 1));

            var replacement = new List<PosKey> { new(new Vector2Value(3f, 4f), 1) };
            Apply(obj, ModificationFields.Positions, replacement);

            Assert.AreEqual(1, obj.Positions.Count);
            Assert.AreEqual(replacement[0], obj.Positions[0]);
        }

        // ==================== addressing one element ====================

        // The capability the index half of the key buys, and the reason it exists at all rather than
        // being added later: overriding keyframe 0 of a track instead of replacing the whole track.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OneKeyframe_Applies()
        {
            var obj = new RectObject { ObjectId = Id };
            obj.Positions.Add(new PosKey(new Vector2Value(0f, 0f), 1));
            obj.Positions.Add(new PosKey(new Vector2Value(1f, 1f), 5));

            var replacement = new PosKey(new Vector2Value(9f, 9f), 1);
            obj.Apply(new Modification(new ModificationKey(Id, ModificationFields.Positions, 0), replacement));

            Assert.AreEqual(2, obj.Positions.Count, "An element override must not resize the track.");
            Assert.AreEqual(replacement, obj.Positions[0]);
            Assert.AreEqual(5, obj.Positions[1].Frame);
        }

        // An index past the end is refused rather than grown into: the override was written against
        // a template that HAD that element, so applying it anyway would invent content.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnIndexPastTheEnd_IsRefused()
        {
            var obj = new RectObject { ObjectId = Id };
            obj.Positions.Add(new PosKey(new Vector2Value(0f, 0f), 1));

            var applied = obj.Apply(new Modification(
                new ModificationKey(Id, ModificationFields.Positions, 4),
                new PosKey(new Vector2Value(9f, 9f), 1)));

            Assert.IsFalse(applied);
            Assert.AreEqual(1, obj.Positions.Count);
        }

        // An index on a scalar has nothing to address, and the generated case refuses it before the
        // conversion rather than writing the field whole.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnIndexOnAScalar_IsRefused()
        {
            var obj = new RectObject { ObjectId = Id, Layer = 1 };

            Assert.IsFalse(obj.Apply(new Modification(
                new ModificationKey(Id, ModificationFields.Layer, 0), 7)));
            Assert.AreEqual(1, obj.Layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnUnregisteredField_IsRefused()
        {
            var obj = new RectObject { ObjectId = Id, Layer = 1 };

            Assert.IsFalse(obj.Apply(new Modification(new ModificationKey(Id, 0x7F01), 7)));
            Assert.AreEqual(1, obj.Layer);
        }

        // A field of a type this object is not: the case is in the table, and the type test in it is
        // what stops a ShapeObject override reaching a plain RectObject.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFieldOfAnotherObjectType_IsRefused()
        {
            var obj = new RectObject { ObjectId = Id };

            Assert.IsFalse(obj.Apply(new Modification(
                new ModificationKey(Id, ModificationFields.ShapeId), new ShapeId(Guid.NewGuid()))));
        }

        // ==================== after a round trip ====================

        // The cases above pass in memory because the value is still the type the property wants.
        // These ask the same question about a value that has been through the serializer, which is
        // the only state an override is ever in when a level is loaded.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnIntField_AppliesAfterARoundTrip()
        {
            var obj = new RectObject { ObjectId = Id, Layer = 1 };
            ApplyRoundTripped(obj, ModificationFields.Layer, 7);

            Assert.AreEqual(7, obj.Layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnEnumField_AppliesAfterARoundTrip()
        {
            var obj = new TextObject { ObjectId = Id };
            ApplyRoundTripped(obj, ModificationFields.HorizontalAlignment, TextObjectHorizontalAlignment.Right);

            Assert.AreEqual(TextObjectHorizontalAlignment.Right, obj.HorizontalAlignment);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnIdField_AppliesAfterARoundTrip()
        {
            var obj = new ShapeObject { ObjectId = Id };
            var replacement = new ShapeId(Guid.NewGuid());
            ApplyRoundTripped(obj, ModificationFields.ShapeId, replacement);

            Assert.AreEqual(replacement, obj.ShapeId);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ABoolField_AppliesAfterARoundTrip()
        {
            var obj = new RectObject { ObjectId = Id, Active = true };
            ApplyRoundTripped(obj, ModificationFields.Active, false);

            Assert.IsFalse(obj.Active);
        }

        // The three kinds this file used to leave uncovered: nothing on their way back changes
        // their type, so they could not fail for the reason under investigation - and they can fail
        // for a conversion that mangles what it did not need to touch.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AStringField_AppliesAfterARoundTrip()
        {
            var obj = new RectObject { ObjectId = Id, Name = "template" };
            ApplyRoundTripped(obj, ModificationFields.Name, "overridden");

            Assert.AreEqual("overridden", obj.Name);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AStructField_AppliesAfterARoundTrip()
        {
            var obj = new RectObject { ObjectId = Id, Span = new FrameSpan(1, 10) };
            ApplyRoundTripped(obj, ModificationFields.Span, new FrameSpan(5, 20));

            Assert.AreEqual(new FrameSpan(5, 20), obj.Span);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AWholeTrack_AppliesAfterARoundTrip()
        {
            var obj = new RectObject { ObjectId = Id };
            obj.Positions.Add(new PosKey(new Vector2Value(0f, 0f), 1));

            var replacement = new List<PosKey> { new(new Vector2Value(3f, 4f), 1) };
            ApplyRoundTripped(obj, ModificationFields.Positions, replacement);

            Assert.AreEqual(1, obj.Positions.Count);
            Assert.AreEqual(replacement[0], obj.Positions[0]);
        }

        // The end-to-end case ModificationRegistryTests used to carry, rewritten onto the mechanism
        // that replaced it: a polymorphic keyframe, addressed as one element of its own track.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OneKeyframeOfAPolymorphicTrack_AppliesAfterARoundTrip()
        {
            var obj = new TextObject { ObjectId = Id };
            obj.FontSizes.Add(new FontSizeKey(new FloatValue(10f), 1));

            var replacement = new FontSizeKey(new FloatValue(42f), 1);
            obj.Apply(RoundTrip(new Modification(
                new ModificationKey(Id, ModificationFields.FontSizes, 0), replacement)));

            Assert.AreEqual(1, obj.FontSizes.Count);
            Assert.AreEqual(replacement, obj.FontSizes[0]);
        }

        // THE ONE SHAPE THE CASES ABOVE LEAVE OUT: a scalar whose DECLARED type is an interface.
        // Every other polymorphic case here is an element of a list, and the two reach TryConvert
        // differently - a list arrives as a JArray and is converted whole, while this arrives as
        // whatever the IString converter wrote and has to find its concrete type from the tag alone.
        // TextObject.Text is the only overridable member of this shape, and it is one an author
        // overrides constantly.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void APolymorphicScalar_AppliesAfterARoundTrip()
        {
            var obj = new TextObject { ObjectId = Id, Text = new StringValue("template") };

            var replacement = new StringValue("overridden");
            obj.Apply(RoundTrip(new Modification(new ModificationKey(Id, ModificationFields.Text), replacement)));

            var written = obj.Text as StringValue;
            Assert.IsNotNull(written, "The override did not land, or landed as another IString.");
            Assert.AreEqual("overridden", written.Value);
        }
    }
}

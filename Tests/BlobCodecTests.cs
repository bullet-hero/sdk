using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Blob;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE SHAPES THESE TAKE ARE THE LEVEL CACHE'S, deliberately: its own header called them "the
    // shape the .blob format's own tests should take", and they were the right ones - a round trip,
    // a truncation sweep, a wrong generation, garbage. What is added is what a FORMAT has to answer
    // for and a cache did not: a payload damaged in the middle while its header still looks right,
    // and a length the file chose that the reader must refuse before allocating anything.
    //
    // THE ROUND TRIP IS THE ONLY ONE THAT PROVES CORRECTNESS, and it proves it through Level.Equals
    // rather than through byte comparison: a codec that writes and reads its own mistakes
    // consistently passes every byte comparison ever written.

    [TestFixture]
    public class BlobCodecTests
    {
        private static readonly SerializationService Service = new();

        private static IDataSerializer Blob => Service.GetDataSerializer(SerializationType.Blob);

        private static byte[] Write(Level level)
            => Blob.SerializeEnvelope(DataDomains.Level, new EnvelopeData(new Version(1, 0), level));

        private static Level Read(byte[] bytes)
            => Blob.DeserializeEnvelope(bytes, typeof(Level)).GetPayload<Level>();

        #region Round trip

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ATestLevel_ReadsBackAsTheSameLevel()
        {
            var level = MockData.CreateTestLevel();

            var read = Read(Write(level));

            Assert.IsTrue(level.Equals(read), "the round trip did not preserve the level");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ALevelOfEveryShape_ReadsBackAsTheSameLevel()
        {
            var level = CreateBigLevel();

            var read = Read(Write(level));

            Assert.AreEqual(level.Game.Objects.Count, read.Game.Objects.Count);
            Assert.AreEqual(level.Resources.Prefabs.Count, read.Resources.Prefabs.Count);
            Assert.IsTrue(level.Equals(read), "the round trip did not preserve the level");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnEmptyCollectionAndAMissingOne_StayDifferent()
        {
            // An object's keyframe tracks are legitimately empty, and null means something else
            // everywhere in this format. A length prefix of -1 is what keeps them apart, and a
            // codec that collapsed them would still round-trip every ordinary level.
            var level = MockData.CreateTestLevel();
            var empty = new ShapeObject { ObjectId = new ObjectId(9001) };
            var missing = new ShapeObject { ObjectId = new ObjectId(9002), UVs = null };
            level.Game.Objects[empty.ObjectId] = empty;
            level.Game.Objects[missing.ObjectId] = missing;

            var read = Read(Write(level));

            Assert.IsNotNull(((ShapeObject)read.Game.Objects[empty.ObjectId]).UVs);
            Assert.IsEmpty(((ShapeObject)read.Game.Objects[empty.ObjectId]).UVs);
            Assert.IsNull(((ShapeObject)read.Game.Objects[missing.ObjectId]).UVs);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheSubclassHalfOfEveryObjectType_Survives()
        {
            // A polymorphic value is written tag-first, and the tag is the model's OWN
            // GetModelType(). Reading one back as its base would compile and lose everything the
            // subtype adds - which is the failure the tag exists to prevent.
            var level = MockData.CreateTestLevel();
            level.Game.Objects.Clear();
            RectObject[] objects =
            {
                new RectObject { ObjectId = new ObjectId(1), Layer = 5 },
                new ShapeObject { ObjectId = new ObjectId(2), ShapeId = ShapeId.Circle.Fill },
                new TextObject { ObjectId = new ObjectId(3), AppearingMask = "Q" },
                new EffectObject { ObjectId = new ObjectId(4) },
                new PrefabObject { ObjectId = new ObjectId(5) },
            };
            foreach (var obj in objects) level.Game.Objects[obj.ObjectId] = obj;

            var read = Read(Write(level));

            foreach (var obj in objects)
            {
                var back = read.Game.Objects[obj.ObjectId];
                Assert.AreEqual(obj.GetType(), back.GetType(), "the concrete type must survive");
                Assert.IsTrue(obj.Equals(back), $"{obj.GetType().Name} did not round trip");
            }
        }

        #endregion

        #region Refusals

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Garbage_IsRefused()
        {
            var random = new Random(20260902);
            var noise = new byte[4096];
            random.NextBytes(noise);

            foreach (var bytes in new[] { null, Array.Empty<byte>(), new byte[64], noise })
                Assert.Throws<BlobFormatException>(() => Read(bytes),
                    $"a payload of {(bytes?.Length.ToString() ?? "null")} bytes was accepted");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ATruncatedPayload_IsRefused()
        {
            var bytes = Write(MockData.CreateTestLevel());

            for (var length = 0; length < bytes.Length; length += Math.Max(1, bytes.Length / 40))
            {
                var truncated = new byte[length];
                Buffer.BlockCopy(bytes, 0, truncated, 0, length);
                Assert.Throws<BlobFormatException>(() => Read(truncated),
                    $"a payload truncated to {length} bytes was accepted");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnotherCodecGeneration_IsRefused()
        {
            var bytes = Write(MockData.CreateTestLevel());
            bytes[4]++; // the generation, immediately after the four magic bytes

            Assert.Throws<BlobFormatException>(() => Read(bytes));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OneFlippedBitInThePayload_IsRefusedByTheHash()
        {
            // The check the header exists for. A single byte changed in the middle leaves every
            // structural claim intact - the magic, the generation, the declared length - and only
            // the hash notices. Without it the level would open, subtly wrong, and stay that way.
            var bytes = Write(MockData.CreateTestLevel());
            bytes[BlobFormat.HeaderLength + 16] ^= 0x40;

            var error = Assert.Throws<BlobFormatException>(() => Read(bytes));
            Assert.That(error.Message, Does.Contain("damaged"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AHostileCount_DoesNotAllocate()
        {
            // The one attack a format has that a cache did not: `new List<T>(count)` on a length
            // the FILE chose. The reader compares a count against the bytes actually left before it
            // believes it, so this costs an exception rather than a gigabyte.
            // A ref struct cannot be captured by a lambda, so the call is wrapped rather than the
            // reader - which is also closer to how a generated body reaches it.
            static void ReadHostileCount()
            {
                var reader = new BlobReader(BitConverter.GetBytes(int.MaxValue));
                reader.ReadCount(1);
            }

            Assert.Throws<BlobFormatException>(ReadHostileCount);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TrailingBytes_AreRefused()
        {
            var bytes = Write(MockData.CreateTestLevel());
            var longer = new byte[bytes.Length + 8];
            Buffer.BlockCopy(bytes, 0, longer, 0, bytes.Length);

            Assert.Throws<BlobFormatException>(() => Read(longer));
        }

        #endregion

        #region The hash itself

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void XxHash64_MatchesTheReferenceVectors()
        {
            // The published vectors for the empty input and for "abc" at seed 0. Written out
            // because "our hash agrees with itself" is worth nothing: the point of choosing a
            // NAMED algorithm is that a second implementation, one day, in another language, reads
            // the same file.
            Assert.AreEqual(0xEF46DB3751D8E999UL, XxHash64.Compute(Array.Empty<byte>()));
            Assert.AreEqual(0x44BC2CF5AD770999UL, XxHash64.Compute(new byte[] { 0x61, 0x62, 0x63 }));
        }

        #endregion

        #region Fixture

        private static Level CreateBigLevel()
        {
            var level = MockData.CreateTestLevel();

            for (var i = 0; i < 500; i++)
            {
                var shape = new ShapeObject
                {
                    ObjectId = new ObjectId(1000 + i),
                    Name = "shape " + i,
                    Layer = i % 100,
                    Span = new FrameSpan(i, i + 30),
                    ShaderType = i % 2 == 0 ? ShaderType.Opaque : ShaderType.Transparent,
                };
                level.Game.Objects[shape.ObjectId] = shape;
            }

            for (var i = 0; i < 8; i++)
            {
                var prefab = new Prefab { Name = "prefab " + i };
                for (var j = 0; j < 6; j++)
                {
                    var inner = new TextObject { ObjectId = new ObjectId(j + 1), Layer = j };
                    prefab.Objects[inner.ObjectId] = inner;
                }
                level.Resources.Prefabs[new PrefabId(Guid.NewGuid())] = prefab;
            }

            var placement = new PrefabObject { ObjectId = new ObjectId(90000) };
            placement.ObjectIds[new ObjectId(1)] = new ObjectId(90001);
            placement.Modifications[new ModificationKey(new ObjectId(1), "layer")] =
                new Modification(new ObjectId(1), "layer", 7);
            level.Game.Objects[placement.ObjectId] = placement;

            return level;
        }

        #endregion
    }
}

using BH.SDK.Models.Enums;
using BH.SDK.Models.Events;
using BH.SDK.Models.Game;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Checkpoint grew a respawn position and a space to read it in. Two things are worth pinning:
    // that a file written before those existed still reads as what it always meant (World at the
    // origin, hence no migration), and that the hand-written IModel<T> boilerplate actually accounts
    // for the new fields - that is the exact place in this codebase where a copy-paste omission
    // compiles and then silently makes two different checkpoints compare equal.
    public class CheckpointTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Default_IsWorldSpaceAtTheOrigin()
        {
            var checkpoint = new Checkpoint();

            Assert.AreEqual(CheckpointSpace.World, checkpoint.Space);
            Assert.IsInstanceOf<Vector2Value>(checkpoint.Position);
            var position = (Vector2Value)checkpoint.Position;
            Assert.AreEqual(0f, position.X);
            Assert.AreEqual(0f, position.Y);
        }

        // The whole reason GameEvents never bumped its DataVersion: absent has to mean the same
        // thing the field's default means. The document is hand-spliced rather than produced by
        // serializing a current GameEvents, because a current one would carry the new keys - the
        // same reason Tests/MockData.cs splices its own historical fragments by hand.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Deserialize_DocumentWithoutTheNewKeys_ReadsAsWorldAtOrigin()
        {
            const string json =
                "{\"version\":\"1.0\",\"value\":{\"checkpoints\":[{\"f\":42,\"name\":\"Old\",\"a\":true}]}}";

            var events = new SerializationService().DeserializeData<GameEvents>(json);

            Assert.AreEqual(1, events.Checkpoints.Count);
            var checkpoint = events.Checkpoints[0];

            Assert.AreEqual(42, checkpoint.Frame);
            Assert.AreEqual(CheckpointSpace.World, checkpoint.Space);
            Assert.IsNotNull(checkpoint.Position);
            Assert.AreEqual(0f, ((Vector2Value)checkpoint.Position).X);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Copy_CarriesPositionAndSpace()
        {
            var source = new Checkpoint("Mid", true, Color4Value.white, 10,
                new Vector2Value(3f, -4f), CheckpointSpace.CameraPosition);

            var copy = source.Copy();

            Assert.AreEqual(CheckpointSpace.CameraPosition, copy.Space);
            Assert.AreEqual(source.Position, copy.Position);
            Assert.AreEqual(source, copy);
            Assert.AreNotSame(source.Position, copy.Position, "a copy must not share the source's value");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equals_NoticesADifferentSpace()
        {
            var a = new Checkpoint("Mid", true, Color4Value.white, 10,
                new Vector2Value(1f, 1f), CheckpointSpace.World);
            var b = new Checkpoint("Mid", true, Color4Value.white, 10,
                new Vector2Value(1f, 1f), CheckpointSpace.Camera);

            Assert.AreNotEqual(a, b);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equals_NoticesADifferentPosition()
        {
            var a = new Checkpoint("Mid", true, Color4Value.white, 10,
                new Vector2Value(1f, 1f), CheckpointSpace.World);
            var b = new Checkpoint("Mid", true, Color4Value.white, 10,
                new Vector2Value(2f, 1f), CheckpointSpace.World);

            Assert.AreNotEqual(a, b);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reset_ReturnsBothNewFieldsToTheirDefaults()
        {
            var checkpoint = new Checkpoint("Mid", false, Color4Value.white, 10,
                new Vector2Value(5f, 5f), CheckpointSpace.Camera);

            checkpoint.Reset();

            Assert.AreEqual(CheckpointSpace.World, checkpoint.Space);
            Assert.AreEqual(0f, ((Vector2Value)checkpoint.Position).X);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ShortConstructor_StillMeansWorldAtOrigin()
        {
            var checkpoint = new Checkpoint("Mid", true, Color4Value.white, 10);

            Assert.AreEqual(CheckpointSpace.World, checkpoint.Space);
            Assert.AreEqual(0f, ((Vector2Value)checkpoint.Position).X);
        }
    }
}

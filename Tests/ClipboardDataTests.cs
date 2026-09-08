using BH.SDK.Models.Audio;
using BH.SDK.Models.Clipboard;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // ClipboardData is a serialization root like Level or Prefab, and it is the only one whose whole
    // point is leaving the process as text (the editor puts it on the system clipboard), so a
    // round trip is not a nicety here - it is the feature.

    /// <summary> ClipboardData's round trip, which for this root is the feature rather than a nicety - its whole
    /// purpose is leaving the process as text. </summary>
    public class ClipboardDataTests
    {
        private static ClipboardData CreateTestClipboard()
        {
            var data = new ClipboardData { Content = ClipboardContent.Objects | ClipboardContent.ObjectKeys };

            var shape = new ShapeObject { ObjectId = new ObjectId(1), Span = new FrameSpan(4, 12), Layer = 3 };
            shape.Positions.Add(new PosKey());
            data.Objects.Add(shape.ObjectId, shape);

            var text = new TextObject { ObjectId = new ObjectId(2), ParentObjectId = new ObjectId(1) };
            data.Objects.Add(text.ObjectId, text);

            // A carrier: an object stripped down to the copied keyframes alone.
            var carrier = new RectObject { ObjectId = new ObjectId(5) };
            carrier.Rotations.Add(new AngleKey());
            data.KeyObjects.Add(carrier.ObjectId, carrier);

            return data;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestClipboardSerialization()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var clipboard = CreateTestClipboard();

            var json = serializationService.SerializeData(clipboard);
            Cat.Meow($"Clipboard - <color=green>{json}</color>");

            var clipboard2 = serializationService.DeserializeData<ClipboardData>(json);
            Assert.IsTrue(clipboard.Equals(clipboard2));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestEmptyClipboardSerialization()
        {
            // The ordinary state of most sections most of the time - an empty section has to survive
            // as an empty one rather than as null, or the consumer's own buffers come back broken.
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var clipboard = new ClipboardData();

            var json = serializationService.SerializeData(clipboard);
            var clipboard2 = serializationService.DeserializeData<ClipboardData>(json);

            Assert.IsTrue(clipboard.Equals(clipboard2));
            Assert.AreEqual(ClipboardContent.None, clipboard2.Content);
            Assert.IsNotNull(clipboard2.Objects);
            Assert.IsNotNull(clipboard2.GameKeys);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestClipboardCopyIsDeep()
        {
            var clipboard = CreateTestClipboard();
            var copy = clipboard.Copy();

            Assert.IsTrue(clipboard.Equals(copy));
            Assert.AreNotSame(clipboard.Objects[new ObjectId(1)], copy.Objects[new ObjectId(1)]);

            copy.Objects[new ObjectId(1)].Layer = 999;
            Assert.AreEqual(3, clipboard.Objects[new ObjectId(1)].Layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestClipboardReset()
        {
            var clipboard = CreateTestClipboard();
            clipboard.AudioTracks.Add(new AudioId(1), new LevelTrack { AudioId = new AudioId(1) });

            clipboard.Reset();

            Assert.AreEqual(ClipboardContent.None, clipboard.Content);
            Assert.AreEqual(0, clipboard.Objects.Count);
            Assert.AreEqual(0, clipboard.KeyObjects.Count);
            Assert.AreEqual(0, clipboard.AudioTracks.Count);
            Assert.IsTrue(clipboard.Equals(new ClipboardData()));
        }
    }
}

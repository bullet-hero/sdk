using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Events;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    [TestFixture]
    public class BeatSegmentTests
    {
        private static BeatSegment Sample() =>
            new(new FrameSpan(120, 480), 128f, 3.5f, 3, "drop", new Color4Value(1f, 0.5f, 0f, 1f));

        // Anchors say "this edge follows the parent's" and a beat segment has no parent, so they are
        // stripped by the setter rather than validated afterwards - FrameSpan keeps them in its sign
        // bits, and an anchored one would serialize as a negative number nothing resolves.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Span_DropsAnchors()
        {
            var segment = new BeatSegment
            {
                Span = new FrameSpan(10, 20, FrameAnchor.Start | FrameAnchor.End),
            };

            Assert.AreEqual(FrameAnchor.None, segment.Span.Anchors);
            Assert.AreEqual(10, segment.Span.StartFrame);
            Assert.AreEqual(20, segment.Span.FrameDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Copy_EqualsSource_AndIsIndependent()
        {
            var source = Sample();
            var copy = source.Copy();

            Assert.IsTrue(source.Equals(copy));
            Assert.IsTrue(source.Equals((object)copy));

            copy.BPM = 90f;
            Assert.IsFalse(source.Equals(copy));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_ReturnsDefaults()
        {
            var segment = Sample();
            segment.Reset();

            Assert.AreEqual(LevelRules.DefaultBpm, segment.BPM);
            Assert.AreEqual(LevelRules.DefaultBeatsPerBar, segment.BeatsPerBar);
            Assert.AreEqual(0f, segment.Offset);
            Assert.AreEqual(string.Empty, segment.Name);
        }

        // The whole level round trip is what actually matters here: Beats rides inside GameEvents,
        // which is its own [DataVersion] envelope, and the segment has no converter of its own.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Level_RoundTrip_KeepsBeats()
        {
            var service = new SerializationService(new SerializationSettings());

            var level = new Level();
            level.Game.Events.Beats.Add(Sample());
            level.Game.Events.Beats.Add(new BeatSegment(new FrameSpan(600, 300), 174f, 0f, 4,
                "chorus", Color4Value.green));

            var json = service.SerializeData(level);
            var restored = service.DeserializeData<Level>(json);

            Assert.AreEqual(2, restored.Game.Events.Beats.Count);
            Assert.IsTrue(level.Game.Events.Beats[0].Equals(restored.Game.Events.Beats[0]));
            Assert.IsTrue(level.Game.Events.Beats[1].Equals(restored.Game.Events.Beats[1]));
        }

        // A level written before this field existed has no "beats" key at all, and must come back as
        // an empty list rather than null - the same no-migration shape LevelSettings.Seed and
        // LevelHints.FontCharacters were added in.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Level_WithoutBeatsKey_DeserializesToEmptyList()
        {
            var service = new SerializationService(new SerializationSettings());

            var json = service.SerializeData(new Level());
            var stripped = json.Replace($"\"{Names.Beats}\":[]", "\"unused_beats\":[]");

            var restored = service.DeserializeData<Level>(stripped);

            Assert.IsNotNull(restored.Game.Events.Beats);
            Assert.AreEqual(0, restored.Game.Events.Beats.Count);
        }
    }
}

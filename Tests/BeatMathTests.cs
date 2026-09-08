using System.Collections.Generic;
using BH.SDK.Models.Events;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> Where beats actually fall: rounding from a segment's own start rather than accumulating, and
    /// what a subdivision does to the answer. </summary>
    [TestFixture]
    public class BeatMathTests
    {
        private static BeatSegment Segment(int start, int duration, float bpm,
            float offset = 0f, int beatsPerBar = 4) =>
            new(new FrameSpan(start, duration), bpm, offset, beatsPerBar, "test", new Color4Value());

        private static List<int> Collect(BeatSegment segment, int framerate, int division = 1)
        {
            var result = new List<int>();
            BeatMath.CollectSegment(segment, framerate, division, 0, FrameRules.MaxFrameDuration, result);
            return result;
        }

        #region FramesPerBeat

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FramesPerBeat_120Bpm60Fps_IsHalfASecond()
        {
            Assert.AreEqual(30f, BeatMath.FramesPerBeat(120f, 60), 1e-4f);
        }

        // The same tempo is a different number of frames per framerate, and that is correct rather
        // than a rounding artifact: a frame is a different length of time in each.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FramesPerBeat_SameBpm_DiffersWithFramerate()
        {
            Assert.AreEqual(15f, BeatMath.FramesPerBeat(120f, 30), 1e-4f);
            Assert.AreEqual(30f, BeatMath.FramesPerBeat(120f, 60), 1e-4f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FramesPerBeat_NonPositiveInput_IsZero()
        {
            Assert.AreEqual(0f, BeatMath.FramesPerBeat(0f, 60));
            Assert.AreEqual(0f, BeatMath.FramesPerBeat(120f, 0));
            Assert.AreEqual(0f, BeatMath.FramesPerBeat(-120f, 60));
        }

        #endregion

        #region CollectSegment

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectSegment_WholeBeats_StartsAtSpanStart()
        {
            // 120 BPM at 60 fps = one beat every 30 frames, span [0, 120) holds four of them.
            var beats = Collect(Segment(0, 120, 120f), 60);
            CollectionAssert.AreEqual(new[] { 0, 30, 60, 90 }, beats);
        }

        // The span is half-open, so a beat landing exactly on EndFrame belongs to whatever comes
        // next, not to this segment.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectSegment_BeatOnEndFrame_IsExcluded()
        {
            var beats = Collect(Segment(0, 121, 120f), 60);
            CollectionAssert.AreEqual(new[] { 0, 30, 60, 90, 120 }, beats);

            var exact = Collect(Segment(0, 120, 120f), 60);
            CollectionAssert.DoesNotContain(exact, 120);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectSegment_Offset_ShiftsWholeGrid()
        {
            var beats = Collect(Segment(0, 120, 120f, 7f), 60);
            CollectionAssert.AreEqual(new[] { 7, 37, 67, 97 }, beats);
        }

        // A negative phase means the first beat sits BEFORE the segment - it simply isn't part of it,
        // and collection starts at the first one that is.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectSegment_NegativeOffset_SkipsBeatsBeforeSpan()
        {
            // Phase -10 puts the grid on 90, 120, 150, ... - the first of those is outside [100, 220)
            // and is simply not part of this segment.
            var beats = Collect(Segment(100, 120, 120f, -10f), 60);
            CollectionAssert.AreEqual(new[] { 120, 150, 180, 210 }, beats);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectSegment_Division_SubdividesEachBeat()
        {
            var beats = Collect(Segment(0, 60, 120f), 60, division: 2);
            CollectionAssert.AreEqual(new[] { 0, 15, 30, 45 }, beats);
        }

        // Every frame is rounded from the segment's own start, never accumulated - so a tempo whose
        // beat is not a whole number of frames stays within half a frame of the true time forever.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CollectSegment_FractionalBeat_DoesNotDrift()
        {
            const float bpm = 140f;
            const int framerate = 60;
            var framesPerBeat = BeatMath.FramesPerBeat(bpm, framerate); // 25.714...

            var beats = Collect(Segment(0, 6000, bpm), framerate);

            for (var i = 0; i < beats.Count; i++)
            {
                var exact = i * framesPerBeat;
                Assert.LessOrEqual(System.Math.Abs(beats[i] - exact), 0.5f,
                    $"beat {i} drifted: {beats[i]} vs {exact}");
            }
            Assert.Greater(beats.Count, 200);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectSegment_ViewportRange_ReturnsOnlyWhatIsInside()
        {
            var result = new List<int>();
            BeatMath.CollectSegment(Segment(0, 1200, 120f), 60, 1, 300, 400, result);
            CollectionAssert.AreEqual(new[] { 300, 330, 360, 390 }, result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectSegment_UnusableSegment_ProducesNothing()
        {
            Assert.AreEqual(0, Collect(Segment(0, 120, 0f), 60).Count);
            Assert.AreEqual(0, Collect(Segment(0, 120, 120f), 0).Count);
            Assert.AreEqual(0, Collect(null, 60).Count);
        }

        // The limit is what keeps a fast tempo over a long span from producing millions of points.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CollectSegment_Limit_CutsCollectionOff()
        {
            var result = new List<int>();
            var appended = BeatMath.CollectSegment(Segment(0, 100_000, 240f), 60, 1,
                0, FrameRules.MaxFrameDuration, result, limit: 10);

            Assert.AreEqual(10, appended);
            Assert.AreEqual(10, result.Count);
        }

        #endregion

        #region CollectBeats

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectBeats_SeveralSegments_CoversEachAtItsOwnTempo()
        {
            var segments = new List<BeatSegment>
            {
                Segment(0, 60, 120f),   // every 30 frames
                Segment(60, 60, 240f),  // every 15 frames
            };

            var result = new List<int>();
            BeatMath.CollectBeats(segments, 60, result);

            CollectionAssert.AreEqual(new[] { 0, 30, 60, 75, 90, 105 }, result);
        }

        // A hole between two segments is the whole reason this is a span list and not a keyframe
        // track: nothing is emitted where no segment says there is a beat.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectBeats_GapBetweenSegments_EmitsNothingInIt()
        {
            var segments = new List<BeatSegment>
            {
                Segment(0, 60, 120f),
                Segment(300, 60, 120f),
            };

            var result = new List<int>();
            BeatMath.CollectBeats(segments, 60, result);

            CollectionAssert.AreEqual(new[] { 0, 30, 300, 330 }, result);
        }

        #endregion

        #region TryGetSegment / IsDownbeat / NormalizeOffset

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TryGetSegment_FrameInsideAndOutside()
        {
            var segments = new List<BeatSegment> { Segment(10, 20, 120f) };

            Assert.IsTrue(BeatMath.TryGetSegment(segments, 10, out _));
            Assert.IsTrue(BeatMath.TryGetSegment(segments, 29, out _));
            Assert.IsFalse(BeatMath.TryGetSegment(segments, 30, out _)); // EndFrame is exclusive
            Assert.IsFalse(BeatMath.TryGetSegment(segments, 9, out _));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IsDownbeat_EveryBeatsPerBar()
        {
            var segment = Segment(0, 120, 120f, 0f, beatsPerBar: 3);

            Assert.IsTrue(BeatMath.IsDownbeat(segment, 0));
            Assert.IsFalse(BeatMath.IsDownbeat(segment, 1));
            Assert.IsFalse(BeatMath.IsDownbeat(segment, 2));
            Assert.IsTrue(BeatMath.IsDownbeat(segment, 3));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NormalizeOffset_BringsPhaseIntoOneBeat()
        {
            Assert.AreEqual(5f, BeatMath.NormalizeOffset(35f, 30f), 1e-4f);
            Assert.AreEqual(25f, BeatMath.NormalizeOffset(-5f, 30f), 1e-4f);
            Assert.AreEqual(0f, BeatMath.NormalizeOffset(60f, 30f), 1e-4f);
        }

        #endregion
    }
}

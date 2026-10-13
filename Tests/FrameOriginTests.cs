using BH.SDK.Models.Enums;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Serialization.Converters.CustomTypes;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The one fixture that pins where the timeline starts (spans four types); every other fixture speaks
    // in OFFSETS, so moving the origin is a handful of failures here.

    /// <summary> That the timeline counts frames from one, and that every type carrying a piece of that
    /// convention agrees on it. </summary>
    public class FrameOriginTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheTimelineCountsFromOne()
        {
            Assert.AreEqual(1, FrameRules.MinFrame);
            Assert.AreEqual(0, FrameRules.NoFrame);
            Assert.Less(FrameRules.NoFrame, FrameRules.MinFrame,
                "NoFrame only works as a sentinel while it sits below every real frame");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NoFrameIsNotAFrame()
        {
            Assert.IsFalse(FrameRules.IsValidFrame(FrameRules.NoFrame));
            Assert.IsFalse(FrameRules.IsValidFrame(-1));
            Assert.IsTrue(FrameRules.IsValidFrame(FrameRules.MinFrame));
        }

        // A timeline of N frames holds frames 1..N, so the count that fits a given last frame is
        // that frame's own number and the exclusive boundary is one past it. Everything in the
        // project that used to spell `FrameDuration - 1` by hand goes through these three.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void LengthAndLastFrameAreTheSameNumber()
        {
            Assert.AreEqual(600, FrameRules.LastFrameOf(600));
            Assert.AreEqual(600, FrameRules.CountOf(600));
            Assert.AreEqual(601, FrameRules.EndBoundaryOf(600));
            Assert.AreEqual(FrameRules.MaxFrameDuration, FrameRules.MaxFrame,
                "the longest legal timeline's last frame IS its length");
        }

        // The all-zero bit pattern is what default(T), Reset(), an unassigned struct field and a
        // converter degrading a corrupt file all produce, so it has to come back as a span the rest
        // of the code can use rather than as one the constructor would have refused.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DefaultSpanIsTheSmallestLegalOne()
        {
            var span = default(FrameSpan);

            Assert.AreEqual(FrameRules.MinFrame, span.StartFrame);
            Assert.AreEqual(FrameRules.MinFrameDuration, span.FrameDuration);
            Assert.AreEqual(FrameRules.MinFrame, span.LastFrame);
            Assert.AreEqual(FrameAnchor.None, span.Anchors);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AKeyframeDefaultsToTheFirstFrame()
        {
            Assert.AreEqual(FrameRules.MinFrame, Keyframe.DefaultFrame);
            Assert.AreEqual(FrameRules.MinFrame, new Keyframe().Frame);
        }

        // The span's own first frame is local MinFrame, not a zero-based offset - the keyframe
        // inspector shows exactly this number, and it is the reason the whole convention moved.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ALocalFrameIsAFrame()
        {
            var span = new FrameSpan(100, 40);

            Assert.AreEqual(FrameRules.MinFrame, span.ToLocalFrame(span.StartFrame));
            Assert.AreEqual(FrameRules.LastFrameOf(span.FrameDuration), span.ToLocalFrame(span.LastFrame));
            Assert.AreEqual(span.StartFrame, span.ToGlobalFrame(FrameRules.MinFrame));
            Assert.AreEqual(span.LastFrame, span.ToGlobalFrame(FrameRules.LastFrameOf(span.FrameDuration)));
        }

        // A span whose start is illegal must lose the illegal END, never gain length: computing the
        // duration before the clamp is what used to turn a request for [0, 100) into [1, 101).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void FromBoundsClampsTheStartWithoutMovingTheEnd()
        {
            var span = FrameSpan.FromBounds(FrameRules.NoFrame, 100);

            Assert.AreEqual(FrameRules.MinFrame, span.StartFrame);
            Assert.AreEqual(100, span.EndFrame);
        }

        // The anchored-edge encoding is a plain negation now, with no offset anywhere, and that is
        // only sound while no legal number is zero: -0 exists in neither JSON nor BSON.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnAnchoredEdgeIsWrittenAsAPlainNegation()
        {
            var converter = new FrameSpanConverter();
            var span = new FrameSpan(FrameRules.MinFrame, FrameRules.MinFrameDuration, FrameAnchor.Both);

            Assert.AreEqual($"[-{FrameRules.MinFrame},-{FrameRules.MinFrameDuration}]",
                JsonConvert.SerializeObject(span, converter));
            Assert.AreEqual(span, JsonConvert.DeserializeObject<FrameSpan>(
                JsonConvert.SerializeObject(span, converter), converter));
        }

        // PROGRESS RUNS BETWEEN THE TWO REACHABLE ENDS, and the last frame is one of them. A playhead
        // is clamped to LastFrameOf and never stands on the end boundary, so dividing by the level's
        // own LENGTH left a finished run reporting (N-1)/N - the 99.9% a cleared level used to show.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ProgressSpansTheFirstFrameToTheLast()
        {
            const int frameCount = 600;

            Assert.AreEqual(0f, FrameRules.ProgressOf(FrameRules.MinFrame, frameCount), 1e-4f);
            Assert.AreEqual(1f, FrameRules.ProgressOf(FrameRules.LastFrameOf(frameCount), frameCount), 1e-4f);
            Assert.AreEqual(0.5f, FrameRules.ProgressOf(FrameRules.MinFrame + 300, 601), 1e-4f);

            // Off both ends: nothing reached, and content playing past the level.
            Assert.AreEqual(0f, FrameRules.ProgressOf(FrameRules.NoFrame, frameCount), 1e-4f);
            Assert.AreEqual(1f, FrameRules.ProgressOf(FrameRules.EndBoundaryOf(frameCount), frameCount), 1e-4f);

            // A one-frame timeline is both ends at once, and a zero-length one has no scale at all.
            Assert.AreEqual(1f, FrameRules.ProgressOf(FrameRules.MinFrame, 1), 1e-4f);
            Assert.AreEqual(0f, FrameRules.ProgressOf(FrameRules.MinFrame, 0), 1e-4f);
        }

        // A seek bar at its right end must stand on the last frame, never on the end boundary.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void FrameAtProgressInvertsProgressOf()
        {
            const int frameCount = 480;

            Assert.AreEqual(FrameRules.MinFrame, FrameRules.FrameAtProgress(0f, frameCount));
            Assert.AreEqual(FrameRules.LastFrameOf(frameCount), FrameRules.FrameAtProgress(1f, frameCount));
            Assert.AreEqual(FrameRules.MinFrame + 300, FrameRules.FrameAtProgress(0.5f, 601));

            for (var frame = FrameRules.MinFrame; frame <= FrameRules.LastFrameOf(frameCount); frame++)
                Assert.AreEqual(frame,
                    FrameRules.FrameAtProgress(FrameRules.ProgressOf(frame, frameCount), frameCount));

            // Off both ends, NaN, and timelines with no span to divide.
            Assert.AreEqual(FrameRules.MinFrame, FrameRules.FrameAtProgress(-1f, frameCount));
            Assert.AreEqual(FrameRules.LastFrameOf(frameCount), FrameRules.FrameAtProgress(2f, frameCount));
            Assert.AreEqual(FrameRules.MinFrame, FrameRules.FrameAtProgress(float.NaN, frameCount));
            Assert.AreEqual(FrameRules.MinFrame, FrameRules.FrameAtProgress(0.5f, 1));
            Assert.AreEqual(FrameRules.MinFrame, FrameRules.FrameAtProgress(0.5f, 0));
        }

        // Everything relative is invariant under a uniform shift of the origin, and that invariance
        // is what made moving it affordable. If one of these ever starts depending on where the
        // timeline begins, something has read a frame number as an offset.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RelativeSpanMathIsInvariantUnderAShift()
        {
            const int shift = 137;

            var first = new FrameSpan(10, 20);
            var second = new FrameSpan(25, 40);
            var shiftedFirst = first.Shifted(shift);
            var shiftedSecond = second.Shifted(shift);

            Assert.AreEqual(first.Overlaps(second), shiftedFirst.Overlaps(shiftedSecond));
            Assert.AreEqual(first.Contains(second), shiftedFirst.Contains(shiftedSecond));
            Assert.AreEqual(first.FrameDuration, shiftedFirst.FrameDuration);
            Assert.AreEqual(first.CompareTo(second), shiftedFirst.CompareTo(shiftedSecond));
            Assert.AreEqual(first.ToLocalFrame(15), shiftedFirst.ToLocalFrame(15 + shift));
        }
    }
}
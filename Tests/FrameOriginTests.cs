using BH.SDK.Interop.AfterBeat;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Serialization.Converters.CustomTypes;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // ONE FIXTURE FOR THE WHOLE CONVENTION, because the convention is spread across four types that
    // have no reason to know about each other - FrameRules holds the constants, FrameSpan holds the
    // packing, Keyframe holds the default and FrameSpanConverter holds the wire form - and a change
    // to any one of them can leave the other three describing a timeline that no longer exists.
    //
    // Every other fixture in this repo speaks in OFFSETS from the first frame on purpose, so that a
    // test says what a generator or an importer DOES rather than where the timeline happens to
    // start. This is the one place that pins where it starts, and it is deliberately the only one:
    // moving the origin again should be a handful of failures here, not a thousand everywhere else.

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

        // The rule that keeps audio in sync: the level's first frame begins at zero, so nothing has
        // to compensate for the origin anywhere downstream of this one subtraction.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheFirstFrameBeginsAtZeroSeconds()
        {
            Assert.AreEqual(0f, (FrameRules.MinFrame - FrameRules.MinFrame) / 60f, 1e-6f);
            Assert.AreEqual(FrameRules.MinFrame, ABTimeMapFirstFrame(60));
        }

        // ABTimeMap is the only other implementation of the frame/time crossing in this repo (the
        // Unity project owns FrameUtils, which cannot be reached from here), so its zero has to
        // agree with the one above or an imported level starts a frame out.
        private static int ABTimeMapFirstFrame(int framerate)
            => ABTimeMap.ToFrame(0f, framerate);

        // A count is NOT a frame, and this is the pair that stops the two being spelled with one
        // function: a length of N frames is N frames long wherever it starts.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ACountCarriesNoOrigin()
        {
            Assert.AreEqual(60, ABTimeMap.ToFrameCount(1f, 60));
            Assert.AreEqual(1f, ABTimeMap.ToSecondsCount(60, 60), 1e-4f);
            Assert.AreEqual(0f, ABTimeMap.ToSeconds(FrameRules.MinFrame, 60), 1e-4f);
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

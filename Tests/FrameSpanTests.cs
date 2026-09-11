using BH.SDK.Models.Enums;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Serialization.Converters.CustomTypes;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // FrameSpan is the type that replaced the old StartFrame + EndFrame pair, and the whole point of
    // it is that the two invariants (Start >= MinFrame, Duration >= 1) and the half-open convention
    // cannot be violated by any representable value. Most of what follows tests exactly that: the
    // packing of the anchor flags into the sign bits and the bias-by-their-floor fields are internal
    // details, so they are checked through the public surface only.


    /// <summary> That no representable FrameSpan can violate its two invariants or the half-open convention.
    /// The sign-bit anchors and the biased duration are internal, so they are only reached through the surface. </summary>
    public class FrameSpanTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Default_IsSingleFrameOnTheFirstFrame_WithoutAnchors()
        {
            var span = new FrameSpan();

            Assert.AreEqual(FrameRules.MinFrame, span.StartFrame);
            Assert.AreEqual(1, span.FrameDuration);
            Assert.AreEqual(FrameRules.MinFrame + 1, span.EndFrame);
            Assert.AreEqual(FrameRules.MinFrame, span.LastFrame);
            Assert.AreEqual(FrameAnchor.None, span.Anchors);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Constructor_ClampsIllegalValuesIntoInvariants()
        {
            var negativeStart = new FrameSpan(-50, 10);
            var zeroDuration = new FrameSpan(5, 0);
            var negativeDuration = new FrameSpan(5, -10);

            Assert.AreEqual(FrameRules.MinFrame, negativeStart.StartFrame);
            Assert.AreEqual(10, negativeStart.FrameDuration);
            Assert.AreEqual(FrameRules.MinFrameDuration, zeroDuration.FrameDuration);
            Assert.AreEqual(FrameRules.MinFrameDuration, negativeDuration.FrameDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void EndFrame_IsExclusive_LastFrameIsInclusive()
        {
            var span = new FrameSpan(10, 15);

            Assert.AreEqual(25, span.EndFrame);
            Assert.AreEqual(24, span.LastFrame);
            Assert.IsTrue(span.Contains(24));
            Assert.IsFalse(span.Contains(25));
        }

        // The regression this whole type exists for: two objects authored back to back used to both
        // render on the frame they shared, because playback fed an inclusive interval tree.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AdjacentSpans_ShareNoFrame()
        {
            var first = new FrameSpan(10, 15);
            var second = new FrameSpan(25, 15);

            Assert.IsFalse(first.Overlaps(second));
            Assert.IsFalse(second.Overlaps(first));
            Assert.IsFalse(first.Contains(25));
            Assert.IsTrue(second.Contains(25));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Contains_Span_IsTheContainmentInvariant()
        {
            var parent = new FrameSpan(10, 30);

            Assert.IsTrue(parent.Contains(new FrameSpan(10, 30)));
            Assert.IsTrue(parent.Contains(new FrameSpan(15, 5)));
            Assert.IsFalse(parent.Contains(new FrameSpan(5, 10)));
            Assert.IsFalse(parent.Contains(new FrameSpan(35, 10)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ClampedInto_CutsBothEdges_AndKeepsMinimumDuration()
        {
            var parent = new FrameSpan(10, 30);

            var overflowing = new FrameSpan(5, 100).ClampedInto(parent);
            Assert.AreEqual(10, overflowing.StartFrame);
            Assert.AreEqual(40, overflowing.EndFrame);

            var fullyBefore = new FrameSpan(0, 5).ClampedInto(parent);
            Assert.AreEqual(10, fullyBefore.StartFrame);
            Assert.AreEqual(FrameRules.MinFrameDuration, fullyBefore.FrameDuration);

            var fullyAfter = new FrameSpan(100, 5).ClampedInto(parent);
            Assert.AreEqual(39, fullyAfter.StartFrame);
            Assert.AreEqual(40, fullyAfter.EndFrame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Anchors_SurviveThePacking_WithoutDisturbingValues()
        {
            var both = new FrameSpan(1234, 567, FrameAnchor.Both);
            var startOnly = new FrameSpan(1234, 567, FrameAnchor.Start);
            var endOnly = new FrameSpan(1234, 567, FrameAnchor.End);

            Assert.AreEqual(FrameAnchor.Both, both.Anchors);
            Assert.IsTrue(both.IsAnchoredStart);
            Assert.IsTrue(both.IsAnchoredEnd);

            Assert.IsTrue(startOnly.IsAnchoredStart);
            Assert.IsFalse(startOnly.IsAnchoredEnd);
            Assert.IsFalse(endOnly.IsAnchoredStart);
            Assert.IsTrue(endOnly.IsAnchoredEnd);

            foreach (var span in new[] { both, startOnly, endOnly })
            {
                Assert.AreEqual(1234, span.StartFrame);
                Assert.AreEqual(567, span.FrameDuration);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Withers_KeepAnchors_AndShiftedClampsAtTheFirstFrame()
        {
            var span = new FrameSpan(10, 20, FrameAnchor.Both);

            Assert.AreEqual(FrameAnchor.Both, span.WithStart(50).Anchors);
            Assert.AreEqual(FrameAnchor.Both, span.WithDuration(5).Anchors);
            Assert.AreEqual(FrameAnchor.Both, span.WithEnd(40).Anchors);
            Assert.AreEqual(40, span.WithEnd(40).EndFrame);
            Assert.AreEqual(FrameAnchor.End, span.WithAnchors(FrameAnchor.End).Anchors);

            Assert.AreEqual(15, span.Shifted(5).StartFrame);
            Assert.AreEqual(FrameRules.MinFrame, span.Shifted(-100).StartFrame);
            Assert.AreEqual(20, span.Shifted(-100).FrameDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void LocalGlobalFrame_RoundTrips()
        {
            var span = new FrameSpan(100, 40);

            // A local frame is a frame, so the span's own first frame is local MinFrame and its
            // last is its FrameDuration - never a zero-based offset.
            Assert.AreEqual(FrameRules.MinFrame, span.ToLocalFrame(100));
            Assert.AreEqual(40, span.ToLocalFrame(139));
            Assert.AreEqual(139, span.ToGlobalFrame(40));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equality_AndCompare_AccountForAnchors()
        {
            var plain = new FrameSpan(10, 20);
            var same = new FrameSpan(10, 20);
            var anchored = new FrameSpan(10, 20, FrameAnchor.End);

            Assert.AreEqual(plain, same);
            Assert.AreEqual(plain.GetHashCode(), same.GetHashCode());
            Assert.AreNotEqual(plain, anchored);

            Assert.Less(new FrameSpan(5, 20).CompareTo(plain), 0);
            Assert.Less(plain.CompareTo(new FrameSpan(10, 30)), 0);
            Assert.AreEqual(0, plain.CompareTo(same));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void MaxBounds_NeverProduceAnEndPastTheFormatLimit()
        {
            var atLimit = new FrameSpan(FrameRules.MaxFrame, 1000);

            Assert.AreEqual(FrameRules.MaxFrame, atLimit.StartFrame);
            Assert.AreEqual(FrameRules.MaxFrame + 1, atLimit.EndFrame);
            Assert.AreEqual(FrameRules.MinFrameDuration, atLimit.FrameDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reset_ReturnsToDefault()
        {
            var span = new FrameSpan(10, 20, FrameAnchor.Both);
            span.Reset();

            Assert.AreEqual(new FrameSpan(), span);
        }

        // The in-memory packing (bias-by-one duration, flags in the sign bits) must never reach the
        // file - a start frame surfacing as -2147483548 would be unreadable to third party tools.
        // The wire form carries its own, separate sign convention: the two logical numbers, negated
        // per anchored edge, so a span is always exactly two numbers and an unanchored one reads as
        // its own plain frame count.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Json_WritesTwoNumbers_AndRoundTrips()
        {
            var converter = new FrameSpanConverter();

            var plain = new FrameSpan(45, 15);
            var plainJson = JsonConvert.SerializeObject(plain, converter);
            Assert.AreEqual("[45,15]", plainJson);
            Assert.AreEqual(plain, JsonConvert.DeserializeObject<FrameSpan>(plainJson, converter));

            var anchoredEnd = new FrameSpan(45, 15, FrameAnchor.End);
            var anchoredEndJson = JsonConvert.SerializeObject(anchoredEnd, converter);
            Assert.AreEqual("[45,-15]", anchoredEndJson);
            Assert.AreEqual(anchoredEnd, JsonConvert.DeserializeObject<FrameSpan>(anchoredEndJson, converter));

            var anchored = new FrameSpan(45, 15, FrameAnchor.Both);
            var anchoredJson = JsonConvert.SerializeObject(anchored, converter);
            Assert.AreEqual("[-45,-15]", anchoredJson);
            Assert.AreEqual(anchored, JsonConvert.DeserializeObject<FrameSpan>(anchoredJson, converter));
        }

        // A child starting together with its parent at the very beginning of the level is the case
        // the sign convention used to be unable to express, because -0 exists in neither JSON nor
        // BSON and the start could legitimately BE zero. It cannot any more - the timeline counts
        // from FrameRules.MinFrame - so the negated number stands on its own with no offset, and
        // this pins that the simplification did not cost the case its round trip.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Json_AnchoredStartOnTheFirstFrame_SurvivesRoundTrip()
        {
            var converter = new FrameSpanConverter();

            var span = new FrameSpan(FrameRules.MinFrame, 1, FrameAnchor.Start);
            var json = JsonConvert.SerializeObject(span, converter);

            Assert.AreEqual("[-1,1]", json);

            var restored = JsonConvert.DeserializeObject<FrameSpan>(json, converter);
            Assert.AreEqual(FrameRules.MinFrame, restored.StartFrame);
            Assert.AreEqual(1, restored.FrameDuration);
            Assert.AreEqual(FrameAnchor.Start, restored.Anchors);
        }
    }
}

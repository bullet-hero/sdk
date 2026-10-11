using System.Collections.Generic;
using BH.SDK.Agent;
using BH.SDK.Models.Events;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> Bar and beat numbering across beat segments: a tempo change, a metre change and a gap.
    /// </summary>
    [TestFixture]
    public class MusicGridTests
    {
        private const int Fps = 60;

        // verse: frames [1, 481), 120 BPM = 30 frames a beat, 4/4 -> 16 beats, bars 1-4, beats 1-16.
        // gap [481, 601). chorus: [601, 1201), 90 BPM = 40 frames, offset 10, 3/4 -> 15 beats, bars 5-9.
        internal static List<BeatSegment> TwoSegments() => new()
        {
            Segment(601, 600, 90f, 10f, 3, "chorus"),
            Segment(1, 480, 120f, 0f, 4, "verse"),
        };

        internal static BeatSegment Segment(int start, int duration, float bpm, float offset, int beatsPerBar,
            string name) => new(new FrameSpan(start, duration), bpm, offset, beatsPerBar, name, new Color4Value());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Numbering_ContinuesAcrossSegmentsInStartOrder_WithNoGapInBars()
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.AreEqual(9, grid.BarCount);
            Assert.AreEqual(31, grid.BeatCount);
            Assert.AreEqual("verse", grid.Segments[0].Name);
            Assert.AreEqual(1, grid.Segments[0].FirstBar);
            Assert.AreEqual(4, grid.Segments[0].LastBar);
            Assert.AreEqual(5, grid.Segments[1].FirstBar);
            Assert.AreEqual(9, grid.Segments[1].LastBar);
            Assert.AreEqual(17, grid.Segments[1].FirstBeat);
            Assert.False(grid.TryUniform(out _, out _));
        }

        [TestCase(1, 1, 1)]
        [TestCase(1, 2, 31)]
        [TestCase(4, 4, 451)]
        [TestCase(5, 1, 611)]
        [TestCase(5, 2, 651)]
        [TestCase(6, 1, 731)]
        [TestCase(9, 3, 1171)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Bar_AndBeat_LandOnTheFrameTheEditorDraws(int bar, int beat, int frame)
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.True(grid.TryFrameOfBar(bar, beat, out var actual, out var error), error);
            Assert.AreEqual(frame, actual);
        }

        [TestCase(1.0, 1)]
        [TestCase(1.5, 16)]
        [TestCase(16.0, 451)]
        [TestCase(17.0, 611)]
        [TestCase(17.5, 631)]
        [TestCase(31.0, 1171)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GlobalBeat_CountsEverySegmentsBeatsInOrder(double beat, int frame)
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.True(grid.TryFrameOfBeat(beat, out var actual, out var error), error);
            Assert.AreEqual(frame, actual);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryBeatMathFrame_RoundTripsThroughBarAndBeat()
        {
            var segments = TwoSegments();
            var grid = new MusicGrid(segments, Fps);
            var frames = new List<int>();
            BeatMath.CollectBeats(segments, Fps, frames);
            frames.Sort();

            Assert.AreEqual(grid.BeatCount, frames.Count);
            for (var i = 0; i < frames.Count; i++)
            {
                Assert.True(grid.TryPositionOf(frames[i], out var position, out var error), error);
                Assert.AreEqual(0, position.Fraction, $"frame {frames[i]}");
                Assert.AreEqual(i + 1, position.GlobalBeat, 1e-9, $"frame {frames[i]}");
                Assert.True(grid.TryFrameOfBar(position.Bar, position.Beat, out var back, out error), error);
                Assert.AreEqual(frames[i], back, position.ToString());
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Position_BetweenBeats_CarriesTheFraction()
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.True(grid.TryPositionOf(46, out var position, out var error), error);

            Assert.AreEqual(1, position.Bar);
            Assert.AreEqual(2, position.Beat);
            Assert.AreEqual(0.5, position.Fraction, 1e-9);
            Assert.AreEqual(2.5, position.GlobalBeat, 1e-9);
            Assert.AreEqual("1.2+0.5", position.ToString());
        }

        [TestCase(500, "\"verse\"")]
        [TestCase(590, "\"chorus\"")]
        [TestCase(5000, "\"chorus\"")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Position_OutsideEverySegment_NamesTheNearest(int frame, string nearest)
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.False(grid.TryPositionOf(frame, out _, out var error));
            StringAssert.Contains("outside every beat segment", error);
            StringAssert.Contains(nearest, error);
        }

        [TestCase(10, 1, "\"chorus\"")]
        [TestCase(0, 1, "\"verse\"")]
        [TestCase(4, 5, "beats 1-4")]
        [TestCase(5, 4, "beats 1-3")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Bar_ThatDoesNotExist_FailsSayingWhy(int bar, int beat, string expected)
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.False(grid.TryFrameOfBar(bar, beat, out _, out var error));
            StringAssert.Contains(expected, error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Beat_LandingPastItsSegment_FailsNamingIt()
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.True(grid.TryFrameOfBeat(16.9, out var inside, out _));
            Assert.AreEqual(478, inside);
            Assert.False(grid.TryFrameOfBeat(16.99, out _, out var error));
            StringAssert.Contains("\"verse\"", error);
            Assert.False(grid.TryFrameOfBeat(32, out _, out error));
            StringAssert.Contains("\"chorus\"", error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Shifts_MoveByBarsKeepingTheBeat_AndByBeatsAcrossTheGap()
        {
            var grid = new MusicGrid(TwoSegments(), Fps);

            Assert.True(grid.TryShiftBars(31, 4, out var bars, out var error), error);
            Assert.AreEqual(651, bars, "bar 1.2 + 4 bars = bar 5.2");
            Assert.True(grid.TryShiftBeats(451, 1, out var beats, out error), error);
            Assert.AreEqual(611, beats, "the beat after the verse's last is the chorus's first");
            Assert.True(grid.TryShiftBars(731, -5, out var back, out error), error);
            Assert.AreEqual(1, back);
            Assert.False(grid.TryShiftBars(1, -1, out _, out error));
            StringAssert.Contains("bar 0", error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NegativeOffset_MakesAPickupBar_CountedAsOne()
        {
            // index 0 sits before the span, so the first beat is beat 2 of bar 1.
            var grid = new MusicGrid(new[] { Segment(1, 240, 120f, -30f, 4, "pickup") }, Fps);

            Assert.AreEqual(3, grid.BarCount);
            Assert.AreEqual(8, grid.BeatCount);
            Assert.True(grid.TryFrameOfBar(1, out var first, out _));
            Assert.AreEqual(1, first);
            Assert.False(grid.TryFrameOfBar(1, 1, out _, out _));
            Assert.True(grid.TryFrameOfBar(2, 1, out var downbeat, out _));
            Assert.AreEqual(91, downbeat);
            Assert.True(grid.TryPositionOf(1, out var position, out _));
            Assert.AreEqual("1.2", position.ToString());
            Assert.True(grid.TryUniform(out var framesPerBeat, out var beatsPerBar));
            Assert.AreEqual(30f, framesPerBeat);
            Assert.AreEqual(4, beatsPerBar);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NoSegments_IsAnEmptyGrid_ThatAnswersNothing()
        {
            var grid = new MusicGrid(null, Fps);

            Assert.True(grid.IsEmpty);
            Assert.False(grid.TryFrameOfBar(1, 1, out _, out var error));
            StringAssert.Contains("no beat grid", error);
            Assert.False(grid.TryPositionOf(1, out _, out error));
            StringAssert.Contains("no beat grid", error);
            Assert.False(grid.TryFrameOfBeat(1, out _, out error));
            StringAssert.Contains("no beat grid", error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Grid_CopiesTheSegments_SoALaterEditDoesNotReachIt()
        {
            var segments = TwoSegments();
            var grid = new MusicGrid(segments, Fps);

            segments[1].BPM = 60f;

            Assert.True(grid.TryFrameOfBar(1, 2, out var frame, out _));
            Assert.AreEqual(31, frame);
        }
    }
}

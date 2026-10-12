using System.Collections.Generic;
using BH.SDK.Agent;
using BH.SDK.Rules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The musical spellings of a time and a duration, resolved through a
    /// <see cref="TimeAddressContext"/> over <see cref="MusicGridTests.TwoSegments"/>. </summary>
    [TestFixture]
    public class TimeAddressMusicTests
    {
        private const int Fps = 60;

        private static TimeAddressContext Context() => new(new MusicGrid(MusicGridTests.TwoSegments(), Fps),
            new Dictionary<string, int> { ["drop"] = 611, ["pre-drop"] = 31, ["void"] = 540 });

        [TestCase("bar:1", 1)]
        [TestCase("bar:5.2", 651)]
        [TestCase(" BAR:9.3 ", 1171)]
        [TestCase("beat:17.5", 631)]
        [TestCase("beat:16", 451)]
        [TestCase("marker:drop", 611)]
        [TestCase("marker:drop+2bars", 851)]
        [TestCase("marker:drop - 1beat", 451)]
        [TestCase("marker:drop+1.5beats", 671)]
        [TestCase("marker:pre-drop", 31)]
        [TestCase("marker:pre-drop+1bar", 151)]
        [TestCase("marker:void", 540)]
        [TestCase("f:120", 120)]
        [TestCase("s:2", 121)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Time_MusicalForms_ResolveThroughTheGridAndMarkers(string input, int expected)
        {
            Assert.True(TimeAddress.TryParseFrame(input, Fps, Context(), out var frame, out var error), error);
            Assert.AreEqual(expected, frame);
        }

        [TestCase("bar:10", "\"chorus\"")]
        [TestCase("bar:4.5", "beats 1-4")]
        [TestCase("bar:x", "whole number")]
        [TestCase("beat:abc", "a beat is a number")]
        [TestCase("marker:nowhere", "no marker named \"nowhere\"")]
        [TestCase("marker:drop+10bars", "bar 15")]
        [TestCase("marker:void+1bar", "outside every beat segment")]
        [TestCase("marker:drop+1.5bars", "bars are whole")]
        [TestCase("marker:", "needs a name")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Time_MusicalForm_ThatCannotResolve_FailsNamingInputAndReason(string input, string reason)
        {
            Assert.False(TimeAddress.TryParseFrame(input, Fps, Context(), out var frame, out var error));
            Assert.AreEqual(FrameRules.NoFrame, frame);
            StringAssert.Contains("\"" + input.Trim() + "\"", error);
            StringAssert.Contains(reason, error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Time_MusicalForm_WithNoGrid_SaysTheLevelHasNone()
        {
            Assert.False(TimeAddress.TryParseFrame("bar:17", Fps, out _, out var error));
            StringAssert.Contains("no beat grid", error);
            Assert.False(TimeAddress.TryParseFrame("beat:3", Fps, null, out _, out error));
            StringAssert.Contains("no beat grid", error);
            var markersOnly = new TimeAddressContext(null, new Dictionary<string, int> { ["drop"] = 611 });
            Assert.True(TimeAddress.TryParseFrame("marker:drop", Fps, markersOnly, out var frame, out error), error);
            Assert.AreEqual(611, frame);
            Assert.False(TimeAddress.TryParseFrame("marker:drop+1bar", Fps, markersOnly, out _, out error));
            StringAssert.Contains("no beat grid", error);
            Assert.False(TimeAddress.TryParseFrame("marker:drop", Fps, new TimeAddressContext(null), out _,
                out error));
            StringAssert.Contains("no markers", error);
        }

        [TestCase("2bars", 611, 240)]
        [TestCase("1bar", 361, 250)]
        [TestCase("3beats", 1, 90)]
        [TestCase("1beat", 451, 160)]
        [TestCase("0.5beats", 611, 20)]
        [TestCase("s:2", 611, 120)]
        [TestCase("f:7", 611, 7)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Duration_InBarsOrBeats_IsMeasuredFromItsStart(string input, int from, int expected)
        {
            Assert.True(TimeAddress.TryParseDuration(input, Fps, Context(), from, out var frames, out var error),
                error);
            Assert.AreEqual(expected, frames);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Duration_WithNoStart_NeedsOneTempo()
        {
            Assert.False(TimeAddress.TryParseDuration("2bars", Fps, Context(), FrameRules.NoFrame, out _,
                out var error));
            StringAssert.Contains("depends on where it starts", error);

            var uniform = new TimeAddressContext(new MusicGrid(new[]
            {
                MusicGridTests.Segment(1, 6000, 120f, 0f, 4,
                    "all")
            }, Fps));
            Assert.AreEqual(240, TimeAddress.ParseDuration(new JValue("2bars"), Fps, uniform, FrameRules.NoFrame));
            Assert.AreEqual(45, TimeAddress.ParseDuration(new JValue("1.5beats"), Fps, uniform, FrameRules.NoFrame));
        }

        [TestCase("2bars", "no beat grid")]
        [TestCase("1.5bars", "bars are whole")]
        [TestCase("bar:3", "\"2bars\", \"3beats\"")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Duration_ThatCannotResolve_FailsNamingInputAndReason(string input, string reason)
        {
            var context = input == "2bars" ? null : Context();

            Assert.False(TimeAddress.TryParseDuration(input, Fps, context, 611, out _, out var error));
            StringAssert.Contains("\"" + input + "\"", error);
            StringAssert.Contains(reason, error);
        }

        [TestCase("2bars+1f", 611, 241)]
        [TestCase("1bar + 1beat", 361, 290)]
        [TestCase("s:1+10", 611, 70)]
        [TestCase("60+f:5", 1, 65)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Duration_Sum_AddsItsTerms(string input, int from, int expected)
        {
            Assert.True(TimeAddress.TryParseDuration(input, Fps, Context(), from, out var frames, out var error),
                error);
            Assert.AreEqual(expected, frames);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Duration_Sum_WithABadTerm_NamesTheTerm()
        {
            Assert.False(TimeAddress.TryParseDuration("2bars+x", Fps, Context(), 611, out _, out var error));
            StringAssert.Contains("\"x\"", error);
        }

        [TestCase("bar:5.2-1f", 650)]
        [TestCase("marker:drop+3f", 614)]
        [TestCase("marker:drop+2bars-1f", 850)]
        [TestCase("s:2+1f", 122)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Time_ShiftedByFrames_MovesTheBase(string input, int expected)
        {
            Assert.True(TimeAddress.TryParseFrame(input, Fps, Context(), out var frame, out var error), error);
            Assert.AreEqual(expected, frame);
        }

        [TestCase("bar:5+1beat", "bar:5.2")]
        [TestCase("bar:5.3 - 2beats", "bar:5")]
        [TestCase("beat:16+1beat", "beat:17")]
        [TestCase("bar:1+4bars", "bar:5")]
        [TestCase("f:651-1beat", "bar:5")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Time_ShiftedAlongTheGrid_LandsWhereTheEquivalentAddressDoes(string input, string same)
        {
            Assert.True(TimeAddress.TryParseFrame(input, Fps, Context(), out var frame, out var error), error);
            Assert.AreEqual(TimeAddress.ParseFrame(new JValue(same), Fps, Context()), frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Time_BeatAfterTheDot_IsExplainedWhenOutOfRange()
        {
            Assert.False(TimeAddress.TryParseFrame("bar:4.25", Fps, Context(), out _, out var error));
            StringAssert.Contains("beat number, not a fraction", error);
            Assert.False(TimeAddress.TryParseFrame("bar:2+1beat", Fps, null, out _, out error));
            StringAssert.Contains("no beat grid", error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Unavailable_RefusesEveryMusicalForm_WithItsReason()
        {
            var template = TimeAddressContext.Unavailable("no music in a template");

            Assert.False(TimeAddress.TryParseFrame("bar:2", Fps, template, out _, out var error));
            StringAssert.Contains("no music in a template", error);
            Assert.False(TimeAddress.TryParseFrame("marker:drop", Fps, template, out _, out error));
            StringAssert.Contains("no music in a template", error);
            Assert.False(TimeAddress.TryParseDuration("2bars", Fps, template, 1, out _, out error));
            StringAssert.Contains("no music in a template", error);
            Assert.True(TimeAddress.TryParseFrame("f:30", Fps, template, out var frame, out error), error);
            Assert.AreEqual(30, frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void WithMarkers_AddsNames_AndKeepsTheEarlierOfTwo()
        {
            var context = Context().WithMarkers(new Dictionary<string, int> { ["outro"] = 900, ["drop"] = 700 });

            Assert.AreEqual(900, TimeAddress.ParseFrame(new JValue("marker:outro"), Fps, context));
            Assert.AreEqual(611, TimeAddress.ParseFrame(new JValue("marker:drop"), Fps, context));
            Assert.AreEqual(31, TimeAddress.ParseFrame(new JValue("marker:pre-drop"), Fps, context));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ParseFrame_WithContext_ThrowsNamingTheInput()
        {
            var ex = Assert.Throws<System.FormatException>(() =>
                TimeAddress.ParseFrame(new JValue("bar:99"), Fps, Context()));
            StringAssert.Contains("bar:99", ex.Message);
            Assert.AreEqual(651, TimeAddress.ParseFrame(new JValue("bar:5.2"), Fps, Context()));
        }
    }
}
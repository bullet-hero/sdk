using BH.SDK.Agent;
using BH.SDK.Rules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The AI's time spellings: a frame, or seconds, in and out of the frame the model holds.
    /// </summary>
    [TestFixture]
    public class TimeAddressTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Frame_NumberAndPrefixedText_AreTheSameFrame()
        {
            Assert.AreEqual(120, TimeAddress.ParseFrame(new JValue(120), 60));
            Assert.AreEqual(120, TimeAddress.ParseFrame(new JValue("f:120"), 60));
            Assert.AreEqual(120, TimeAddress.ParseFrame(new JValue("120"), 60));
            Assert.AreEqual(120, TimeAddress.ParseFrame(new JValue(" F:120 "), 60));
        }

        [TestCase("s:0", 60, 1)]
        [TestCase("s:2", 60, 121)]
        [TestCase("s:12.5", 60, 751)]
        [TestCase("s:12.5", 24, 301)]
        [TestCase("s:0.0166", 60, 1)]
        [TestCase("s:0.0167", 60, 2)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Frame_Seconds_BeginsTheFrameContainingThem(string input, int framerate, int expected)
        {
            Assert.AreEqual(expected, TimeAddress.ParseFrame(new JValue(input), framerate));
        }

        [TestCase("s:2", 60, 120)]
        [TestCase("s:0.5", 60, 30)]
        [TestCase("f:60", 60, 60)]
        [TestCase("60", 30, 60)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Duration_IsACountWithNoOrigin(string input, int framerate, int expected)
        {
            Assert.AreEqual(expected, TimeAddress.ParseDuration(new JValue(input), framerate));
        }

        [TestCase("s:-1")]
        [TestCase("f:0")]
        [TestCase("0")]
        [TestCase("-3")]
        [TestCase("s:abc")]
        [TestCase("f:1.5")]
        [TestCase("bar:17")]
        [TestCase("")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Frame_Invalid_FailsNamingTheInput(string input)
        {
            Assert.False(TimeAddress.TryParseFrame(input, 60, out var frame, out var error));
            Assert.AreEqual(FrameRules.NoFrame, frame);
            StringAssert.Contains("\"" + input.Trim() + "\"", error);
        }

        [TestCase("s:0")]
        [TestCase("f:0")]
        [TestCase("s:0.001")]
        [TestCase("s:-2")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Duration_BelowOneFrame_FailsNamingTheInput(string input)
        {
            Assert.False(TimeAddress.TryParseDuration(input, 60, out _, out var error));
            StringAssert.Contains(input, error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NonTimeTokens_Fail()
        {
            Assert.False(TimeAddress.TryParseFrame(null, 60, out _, out _));
            Assert.False(TimeAddress.TryParseFrame(JValue.CreateNull(), 60, out _, out _));
            Assert.False(TimeAddress.TryParseFrame(new JValue(true), 60, out _, out _));
            Assert.False(TimeAddress.TryParseFrame(new JValue(1.5), 60, out _, out var error));
            StringAssert.Contains("1.5", error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void OutOfRange_AndBadFramerate_Fail()
        {
            Assert.False(TimeAddress.TryParseFrame("f:" + (FrameRules.MaxFrame + 1), 60, out _, out _));
            Assert.False(TimeAddress.TryParseFrame("s:1e30", 60, out _, out _));
            Assert.False(TimeAddress.TryParseDuration("f:" + (FrameRules.MaxFrameDuration + 1), 60, out _, out _));
            Assert.False(TimeAddress.TryParseFrame("f:1", 0, out _, out var error));
            StringAssert.Contains("framerate", error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Parse_ThrowsFormatExceptionNamingTheInput()
        {
            var ex = Assert.Throws<System.FormatException>(() => TimeAddress.ParseFrame(new JValue("s:-1"), 60));
            StringAssert.Contains("s:-1", ex.Message);
        }
    }
}

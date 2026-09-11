using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary>
    /// SurrogateUtils: the one place that knows an astral character is two code units, so nothing
    /// that cuts a string by length can end inside one.
    /// </summary>
    [TestFixture]
    public class SurrogateUtilsTests
    {
        /// <summary> U+1F44D THUMBS UP - one character, two code units. Escaped so the fixture never
        /// depends on this file's own encoding. </summary>
        private const string Thumb = "👍";

        /// <summary> Fails on any surrogate not sitting beside its own other half. </summary>
        private static void AssertNoLoneSurrogate(string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsHighSurrogate(character))
                {
                    Assert.IsTrue(index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]),
                        $"A high surrogate at {index} of \"{value}\" has no low half");
                    index++;
                    continue;
                }

                Assert.IsFalse(char.IsLowSurrogate(character),
                    $"A low surrogate at {index} of \"{value}\" has no high half");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IsLead_CoversExactlyTheHighSurrogateRange()
        {
            Assert.IsFalse(SurrogateUtils.IsLead('퟿'));
            Assert.IsTrue(SurrogateUtils.IsLead('\uD800'));
            Assert.IsTrue(SurrogateUtils.IsLead('\uDBFF'));
            Assert.IsFalse(SurrogateUtils.IsLead('\uDC00'));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IsTrail_CoversExactlyTheLowSurrogateRange()
        {
            Assert.IsFalse(SurrogateUtils.IsTrail('\uDBFF'));
            Assert.IsTrue(SurrogateUtils.IsTrail('\uDC00'));
            Assert.IsTrue(SurrogateUtils.IsTrail('\uDFFF'));
            Assert.IsFalse(SurrogateUtils.IsTrail(''));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ClampLength_StringThatFits_IsUnchanged()
        {
            Assert.AreEqual(5, SurrogateUtils.ClampLength("abcde", 10));
            Assert.AreEqual(5, SurrogateUtils.ClampLength("abcde", 5));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ClampLength_CutLandingInsideAPair_DropsTheWholePair()
        {
            // "a" + pair: a cut at 2 would keep the high half alone.
            Assert.AreEqual(1, SurrogateUtils.ClampLength("a" + Thumb, 2));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ClampLength_CutLandingAfterAPair_KeepsIt()
        {
            Assert.AreEqual(3, SurrogateUtils.ClampLength("a" + Thumb + "b", 3));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ClampLength_NullOrEmpty_IsZero()
        {
            Assert.AreEqual(0, SurrogateUtils.ClampLength(null, 8));
            Assert.AreEqual(0, SurrogateUtils.ClampLength(string.Empty, 8));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ClampLength_NegativeCeiling_IsZero()
        {
            Assert.AreEqual(0, SurrogateUtils.ClampLength("abc", -1));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Truncate_StringThatFits_ReturnsTheSameInstance()
        {
            const string value = "abcde";

            Assert.AreSame(value, SurrogateUtils.Truncate(value, 5));
            Assert.AreSame(value, SurrogateUtils.Truncate(value, 99));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Truncate_CutInsideAPair_KeepsWhatIsWhole()
        {
            Assert.AreEqual("a", SurrogateUtils.Truncate("a" + Thumb + "b", 2));
            Assert.AreEqual("a" + Thumb, SurrogateUtils.Truncate("a" + Thumb + "b", 3));
        }

        // The ceiling this guards is ValueRules.MaxGameString / TextRules.MaxAppearingMask, and both
        // are constants somebody will raise or lower one day. Enumerating every cut is what keeps the
        // guarantee independent of the number: whatever the ceiling becomes, no cut can produce a
        // lone half.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Truncate_EveryCeilingOverAMixedString_NeverLeavesALoneSurrogate()
        {
            var value = "a" + Thumb + "bc" + Thumb + Thumb + "d";

            for (var ceiling = 0; ceiling <= value.Length + 2; ceiling++)
                AssertNoLoneSurrogate(SurrogateUtils.Truncate(value, ceiling));
        }

        // Input arriving already broken - a hand-edited file, a foreign tool, a level cut by an older
        // build - must not make this throw or invent a character. It cuts where it was asked and
        // leaves the damage it did not cause.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Truncate_SourceAlreadyCarryingALoneSurrogate_DoesNotThrow()
        {
            var broken = "a\uD83Db"; // a high surrogate with no low half, in the middle

            Assert.DoesNotThrow(() => SurrogateUtils.Truncate(broken, 2));
            Assert.AreEqual(3, SurrogateUtils.ClampLength(broken, 3));
        }
    }
}

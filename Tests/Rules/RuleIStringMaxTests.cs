using System;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIStringMax over both IString variants: a plain StringValue and a StringLocalized holding
    /// one entry per language. The localized case caps every language separately, not their total.
    /// </summary>
    public class RuleIStringMaxTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIStringMax(5)] public IString Value { get; set; } = new StringValue("abc");
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIStringMax(5)] public string Value { get; set; } = string.Empty;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new StringValue("abc") });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBoundary()
        {
            AssertValid(new Model { Value = new StringValue("abcde") });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueJustOver()
        {
            AssertInvalid<RuleIStringMaxAttribute>(new Model { Value = new StringValue("abcdef") });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueTruncates()
        {
            var model = new Model { Value = new StringValue("abcdefghij") };
            AssertFixedTo(model, () => ((StringValue)model.Value).Value, "abcde");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestLocalizedValid()
        {
            AssertValid(new Model
            {
                Value = new StringLocalized(new StringLanguage("en", "abc"),
                    new StringLanguage("ru", "abcde")),
            });
        }

        // Each language is capped on its own - two languages of five characters each are fine even
        // though their combined length is over the cap.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestLocalizedCapsEachLanguageSeparately()
        {
            AssertInvalid<RuleIStringMaxAttribute>(new Model
            {
                Value = new StringLocalized(new StringLanguage("en", "abc"),
                    new StringLanguage("ru", "abcdef")),
            });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixLocalizedTruncatesOnlyOverlongEntries()
        {
            var localized = new StringLocalized(new StringLanguage("en", "ab"),
                new StringLanguage("ru", "abcdefghij"));
            var model = new Model { Value = localized };
            AssertFixed(model);

            Assert.AreEqual("ab", localized.Strings[0].Value);
            Assert.AreEqual("abcde", localized.Strings[1].Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyLocalizedIsValid()
        {
            AssertValid(new Model { Value = new StringLocalized() });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIStringMaxAttribute>(new Model { Value = null });
        }

        // A StringValue holding a null string blows up inside the rule instead of being reported:
        // the length check dereferences Value without a guard. Nothing in the SDK prevents building
        // one (the constructor assigns straight through), and deserializing a JSON null lands here
        // too. Pinned as a throw so the guard that fixes it has to update this test deliberately.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullInnerStringThrows()
        {
            var model = new Model { Value = new StringValue(null) };
            Assert.Throws<NullReferenceException>(() => Analyze(model));
        }

        // Same cut, same hazard as RuleStringMax's: the ceiling counts code units, an astral
        // character is two of them, and a cut between the halves leaves one that is not a character
        // at all. Both IString variants go through the same clamp.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixNeverLeavesALoneSurrogate()
        {
            const string thumb = "👍"; // U+1F44D, one character, two code units

            var model = new Model { Value = new StringValue("ab" + thumb + thumb) };
            AssertFixedTo(model, () => ((StringValue)model.Value).Value, "ab" + thumb);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestLocalizedFixNeverLeavesALoneSurrogate()
        {
            const string thumb = "👍";

            var model = new Model
            {
                Value = new StringLocalized(new[] { new StringLanguage("en", "ab" + thumb + thumb) }),
            };

            Fix(model);

            var localized = (StringLocalized)model.Value;
            Assert.AreEqual("ab" + thumb, localized.Strings[0].Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWrongType()
        {
            AssertWrongType(new WrongTypeModel());
        }
    }
}

using BH.SDK.Models.Enums.Controls;
using BH.SDK.Rules;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The grammar is the contract between three parties that never see each other: a person editing
    // settings.json, the rule that validates what they wrote, and the engine-side resolver that turns
    // it into a key. Everything worth pinning here is a case where two of them could disagree -
    // capitalization, modifier order, a stray separator - because each of those makes a binding that
    // resolves correctly and still fails to compare equal to itself.

    /// <summary> ShortcutSyntax: what a binding string may say and what it canonicalizes to. </summary>
    public class ShortcutSyntaxTests
    {
        private static string Normalize(string value, bool allowModifierOnly = false)
        {
            Assert.IsTrue(ShortcutSyntax.TryNormalize(value, allowModifierOnly, out var normalized),
                $"Expected '{value}' to be a legal binding");
            return normalized;
        }

        private static void AssertRejected(string value, bool allowModifierOnly = false)
        {
            Assert.IsFalse(ShortcutSyntax.TryNormalize(value, allowModifierOnly, out var normalized),
                $"Expected '{value}' to be rejected, got '{normalized}'");
            Assert.AreEqual(ShortcutSyntax.Unbound, normalized);
        }

        [TestCase("c", "c")]
        [TestCase("ctrl+c", "ctrl+c")]
        [TestCase("CTRL+C", "ctrl+c")]
        [TestCase("Shift+Ctrl+P", "ctrl+shift+p")]
        [TestCase("alt+shift+ctrl+f4", "ctrl+shift+alt+f4")]
        [TestCase("  ctrl + c  ", "ctrl+c")]
        [TestCase("mouse.middle", "mouse.middle")]
        [TestCase("digit1", "digit1")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestCanonicalForm(string value, string expected)
        {
            Assert.AreEqual(expected, Normalize(value));
        }

        // The point of canonicalizing at all: two spellings of one binding must not be able to
        // coexist, because everything downstream compares these as strings.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNormalizeIsIdempotent()
        {
            var once = Normalize("Shift+Ctrl+P");
            Assert.AreEqual(once, Normalize(once));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEmptyIsUnboundNotInvalid()
        {
            Assert.AreEqual(ShortcutSyntax.Unbound, Normalize(""));
            Assert.AreEqual(ShortcutSyntax.Unbound, Normalize("   "));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNullIsRejected()
        {
            AssertRejected(null);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestAlternates()
        {
            Assert.AreEqual("ctrl+y|ctrl+shift+z", Normalize("Ctrl+Y|Shift+Ctrl+Z"));
        }

        // A duplicate alternate is not an error - it is the same key twice, which means once.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestDuplicateAlternateCollapses()
        {
            Assert.AreEqual("ctrl+c", Normalize("ctrl+c|CTRL+C"));
        }

        // A trailing separator is a typo rather than a second, unbound key: there is no such thing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEmptyAlternateIsDropped()
        {
            Assert.AreEqual("ctrl+c", Normalize("ctrl+c|"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestTooManyAlternatesRejected()
        {
            AssertRejected("a|b|c");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestTooLongRejected()
        {
            AssertRejected(new string('a', KeybindingsRules.MaxBindingLength + 1));
        }

        [TestCase("ctrl+")]
        [TestCase("+c")]
        [TestCase("ctrl++c")]
        [TestCase("a+ctrl")]
        [TestCase("a-b")]
        [TestCase("ctrl+shift")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestMalformedRejected(string value)
        {
            AssertRejected(value);
        }

        // A held shortcut - a wheel modifier - IS its modifier, and nothing else. Everywhere else a
        // bare modifier is a half-typed binding.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestModifierOnlyNeedsPermission()
        {
            AssertRejected("ctrl");
            Assert.AreEqual("ctrl", Normalize("Ctrl", allowModifierOnly: true));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestSplitReportsModifiersAndKey()
        {
            Assert.IsTrue(ShortcutSyntax.TrySplit("ctrl+shift+p", out var modifiers, out var key));
            Assert.AreEqual(ShortcutModifiers.Ctrl | ShortcutModifiers.Shift, modifiers);
            Assert.AreEqual("p", key);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestSplitOfModifierOnlyHasNoKey()
        {
            Assert.IsTrue(ShortcutSyntax.TrySplit("ctrl", out var modifiers, out var key));
            Assert.AreEqual(ShortcutModifiers.Ctrl, modifiers);
            Assert.IsEmpty(key);
        }

        // Compose is what the capture UI writes with, so it has to produce exactly what TrySplit
        // reads back - the round trip is the contract, not either half on its own.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestComposeRoundTrips()
        {
            var composed = ShortcutSyntax.Compose(
                ShortcutModifiers.Alt | ShortcutModifiers.Ctrl, "f4");
            Assert.AreEqual("ctrl+alt+f4", composed);

            Assert.IsTrue(ShortcutSyntax.TrySplit(composed, out var modifiers, out var key));
            Assert.AreEqual(ShortcutModifiers.Ctrl | ShortcutModifiers.Alt, modifiers);
            Assert.AreEqual("f4", key);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestSplitAlternates()
        {
            CollectionAssert.AreEqual(new[] { "ctrl+y", "ctrl+shift+z" },
                ShortcutSyntax.SplitAlternates("ctrl+y|ctrl+shift+z"));
            CollectionAssert.IsEmpty(ShortcutSyntax.SplitAlternates(ShortcutSyntax.Unbound));
        }
    }
}

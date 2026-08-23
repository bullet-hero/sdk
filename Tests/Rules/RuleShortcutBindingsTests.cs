using System.Collections.Generic;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleShortcutBindings: a keybindings map must hold canonical binding strings under non-empty
    /// ids, and a fix drops whatever does not so the shortcut falls back to its shipped default.
    /// </summary>
    public class RuleShortcutBindingsTests : BaseRuleTests
    {
        [RuleContainer]
        private class MapModel
        {
            [RuleShortcutBindings]
            public Dictionary<string, string> Value { get; set; } = new();
        }

        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleShortcutBindings]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyMapIsValid()
        {
            AssertValid(new MapModel());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCanonicalEntriesAreValid()
        {
            AssertValid(new MapModel
            {
                Value =
                {
                    ["editor.copy"] = "ctrl+c",
                    ["editor.redo"] = "ctrl+y|ctrl+shift+z",
                    ["timeline.pan_modifier"] = "ctrl",
                    ["editor.beat_tap"] = "",
                },
            });
        }

        // Not merely "does it parse". A value that resolves correctly and is spelled differently
        // still fails to match the canonical string every conflict check compares it against.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNonCanonicalSpellingIsInvalid()
        {
            AssertInvalid<RuleShortcutBindingsAttribute>(new MapModel
            {
                Value = { ["editor.search_commands"] = "Shift+Ctrl+P" },
            });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMalformedValueIsInvalid()
        {
            AssertInvalid<RuleShortcutBindingsAttribute>(new MapModel
            {
                Value = { ["editor.copy"] = "ctrl+" },
            });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyIdIsInvalid()
        {
            AssertInvalid<RuleShortcutBindingsAttribute>(new MapModel
            {
                Value = { [""] = "ctrl+c" },
            });
        }

        // Dropping is the safe direction: the shortcut falls back to what the game ships with.
        // Rewriting a garbled value into some nearby legal one would hand the player a binding they
        // never chose.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestFixDropsOnlyTheOffendingEntry()
        {
            var model = new MapModel
            {
                Value =
                {
                    ["editor.copy"] = "ctrl+c",
                    ["editor.paste"] = "Ctrl+V",
                    ["editor.duplicate"] = "ctrl++d",
                },
            };

            AssertFixed(model);

            Assert.AreEqual(1, model.Value.Count);
            Assert.AreEqual("ctrl+c", model.Value["editor.copy"]);
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

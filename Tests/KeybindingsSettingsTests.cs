using BH.SDK.Models;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Serialization;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Two things here are worth a test rather than a comment. First, the group shipped WITHOUT a
    // UserSettings version bump, so a settings.json written before it existed has no "keys" key at
    // all and must come back holding an empty map rather than null - the same additive shape
    // InterfaceSettings and AntiAliasingGraphicsSettings were added in.
    //
    // Second, and specific to this group: an EMPTY value is a real answer. "The player cleared this
    // shortcut" and "the player never touched it" are different states that must survive a round
    // trip separately, because the first has to beat the shipped default and the second has to lose
    // to it. A map that dropped empty values on save would silently re-arm every shortcut anyone
    // deliberately turned off.

    /// <summary> KeybindingsSettings: defaults, the override API, boilerplate, and what an older
    /// settings file deserializes to. </summary>
    public class KeybindingsSettingsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreAnEmptyMap()
        {
            var settings = new KeybindingsSettings();

            Assert.IsNotNull(settings.Overrides);
            Assert.IsEmpty(settings.Overrides);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_ClearsEveryOverride()
        {
            var settings = new KeybindingsSettings();
            settings.SetOverride("editor.copy", "ctrl+shift+c");

            settings.Reset();

            Assert.IsEmpty(settings.Overrides);
            Assert.IsTrue(new KeybindingsSettings().Equals(settings));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SetOverride_StoresTheCanonicalForm()
        {
            var settings = new KeybindingsSettings();

            Assert.IsTrue(settings.SetOverride("editor.search_commands", "Shift+Ctrl+P"));
            Assert.AreEqual("ctrl+shift+p", settings.GetOverride("editor.search_commands"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SetOverride_RefusesGarbageAndWritesNothing()
        {
            var settings = new KeybindingsSettings();

            Assert.IsFalse(settings.SetOverride("editor.copy", "ctrl+"));
            Assert.IsFalse(settings.SetOverride("", "ctrl+c"));
            Assert.IsFalse(settings.SetOverride("editor.multi_select", "ctrl"));

            Assert.IsEmpty(settings.Overrides);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SetOverride_AcceptsAModifierOnlyBindingWhenAsked()
        {
            var settings = new KeybindingsSettings();

            Assert.IsTrue(settings.SetOverride("timeline.zoom_modifier", "Alt", allowModifierOnly: true));
            Assert.AreEqual("alt", settings.GetOverride("timeline.zoom_modifier"));
        }

        // The distinction the whole sparse design rests on: absent falls back to the shipped default,
        // empty does not.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetOverride_SeparatesClearedFromNeverSet()
        {
            var settings = new KeybindingsSettings();
            settings.SetOverride("editor.beat_tap", "");

            Assert.AreEqual(string.Empty, settings.GetOverride("editor.beat_tap"));
            Assert.IsNull(settings.GetOverride("editor.copy"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ClearOverride_RestoresTheFallback()
        {
            var settings = new KeybindingsSettings();
            settings.SetOverride("editor.copy", "ctrl+shift+c");

            Assert.IsTrue(settings.ClearOverride("editor.copy"));
            Assert.IsNull(settings.GetOverride("editor.copy"));
            Assert.IsFalse(settings.ClearOverride("editor.copy"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CopyAndPull_CarryEveryEntry()
        {
            var source = MockData.CreateValidTestSettings().Keybindings;

            var copy = source.Copy();
            Assert.IsTrue(source.Equals(copy));
            Assert.AreEqual(source.GetHashCode(), copy.GetHashCode());

            var pulled = new KeybindingsSettings();
            pulled.Pull(source);
            Assert.IsTrue(source.Equals(pulled));
        }

        // A shallow dictionary copy is the right one here only because both halves are immutable
        // strings - but the DICTIONARY itself must not be shared, or editing a copy would edit the
        // settings it was taken from.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Copy_DoesNotShareTheMap()
        {
            var source = new KeybindingsSettings();
            source.SetOverride("editor.copy", "ctrl+shift+c");

            var copy = source.Copy();
            copy.SetOverride("editor.paste", "ctrl+shift+v");

            Assert.IsNull(source.GetOverride("editor.paste"));
            Assert.IsFalse(source.Equals(copy));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equals_SeesBothTheKeyAndTheValue()
        {
            var a = new KeybindingsSettings();
            a.SetOverride("editor.copy", "ctrl+shift+c");

            var differentValue = a.Copy();
            differentValue.SetOverride("editor.copy", "ctrl+alt+c");
            Assert.IsFalse(a.Equals(differentValue));

            var differentKey = new KeybindingsSettings();
            differentKey.SetOverride("editor.paste", "ctrl+shift+c");
            Assert.IsFalse(a.Equals(differentKey));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void UserSettings_RoundTrip_KeepsEveryBinding()
        {
            var service = new SerializationService(new SerializationSettings());
            var settings = MockData.CreateValidTestSettings();

            var json = service.SerializeData(settings);
            var restored = service.DeserializeData<UserSettings>(json);

            Assert.IsTrue(settings.Keybindings.Equals(restored.Keybindings));
            Assert.AreEqual(string.Empty, restored.Keybindings.GetOverride("editor.beat_tap"));
        }

        // What a settings.json written before this group existed deserializes to. The key is simply
        // absent there, so the constructor's own empty map has to survive - not null, which every
        // reader would then have to guard.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void UserSettings_WithoutKeybindingsKey_DeserializesToAnEmptyMap()
        {
            var service = new SerializationService(new SerializationSettings());

            var json = service.SerializeData(MockData.CreateValidTestSettings());
            var stripped = json.Replace($"\"{Names.Keys}\":", "\"unused_keys\":");
            Assert.AreNotEqual(json, stripped, "Test fixture did not strip the keybindings key");

            var restored = service.DeserializeData<UserSettings>(stripped);

            Assert.IsNotNull(restored.Keybindings);
            Assert.IsEmpty(restored.Keybindings.Overrides);
        }
    }
}
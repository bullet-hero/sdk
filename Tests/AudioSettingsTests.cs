using BH.SDK.Models.SettingGroups;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The four channels do not all start at 1: UI and EditorUI start at half on purpose and are separate
    // channels (the shell sounds every press, the editor only outcomes). EditorUI's key is absent from
    // every older file, so the constructor must answer. BH.SDK/Docs/TESTING.md.

    /// <summary> The volume mix's defaults and its round trip. </summary>
    public class AudioSettingsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_PutTheInterfaceUnderTheLevel()
        {
            var settings = new AudioSettings();

            Assert.AreEqual(1f, settings.Volume);
            Assert.AreEqual(1f, settings.Game);
            Assert.AreEqual(0.5f, settings.UI);
            Assert.AreEqual(0.5f, settings.EditorUI);
            Assert.AreEqual(1f, settings.EditorGame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresTheDefaults()
        {
            var settings = new AudioSettings(0.2f, 0.3f, 0.05f, 0.4f, 0.1f);

            settings.Reset();

            Assert.AreEqual(new AudioSettings(), settings);
            Assert.AreEqual(0.5f, settings.UI);
            Assert.AreEqual(0.5f, settings.EditorUI);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsEveryChannel()
        {
            var source = new AudioSettings(0.9f, 0.8f, 0.5f, 0.7f, 0.6f);

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<AudioSettings>(json);

            Assert.AreEqual(source, restored);
        }

        // A file written before the default moved carries the key, so it keeps the level it had -
        // which is what makes the change safe for anyone who ever opened the audio tab.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnExplicitValue_OutranksTheNewDefault()
        {
            var settings = JsonConvert.DeserializeObject<AudioSettings>(
                "{\"vlm\":1.0,\"game\":1.0,\"ui\":1.0}");

            Assert.AreEqual(1f, settings.UI);
        }

        // The whole of what an additive field promises: no key, no migrator, and the value the
        // constructor supplies is what the player gets. If this ever reads 0, the field stopped
        // being additive and every existing settings.json silenced the editor.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnAbsentEditorUI_TakesTheConstructorsValue()
        {
            var settings = JsonConvert.DeserializeObject<AudioSettings>(
                "{\"vlm\":1.0,\"game\":1.0,\"ui\":1.0}");

            Assert.AreEqual(0.5f, settings.EditorUI);
        }

        // The editor's own fader is the second additive channel and takes the opposite
        // default - FULL, because it replaces Game while the editor is open, and a zero here
        // would open every existing settings.json onto a silent editor.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnAbsentEditorGame_TakesTheConstructorsValue()
        {
            var settings = JsonConvert.DeserializeObject<AudioSettings>(
                "{\"vlm\":1.0,\"game\":1.0,\"ui\":1.0}");

            Assert.AreEqual(1f, settings.EditorGame);
        }
    }
}
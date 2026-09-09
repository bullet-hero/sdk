using BH.SDK.Models.SettingGroups;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE MIX A PLAYER IS FIRST HANDED, and the only thing about it worth pinning is that the four
    // channels do not all start at 1. UI sits at half on purpose - interface sounds are feedback on
    // a press the player already made, while the music is what they came for - and a value that
    // reads as a typo is exactly the kind that gets "corrected" back.
    //
    // EditorUI starts at that same half and is a separate channel anyway, which is the part a
    // reader is likeliest to mistake for a duplicate: the two differ in what they SOUND, not in how
    // loud they are - the shell sounds every press, the editor only its outcomes - so an author
    // silencing one must not lose the other.
    //
    // Nothing here needs an absent-key case for UI: 0.5 is not the zero value, so a settings.json
    // written before that change carries an explicit "ui" of 1 and keeps it. That is the intended
    // cost and Rule 11's whole position - the author's own file is re-saved, and no migrator
    // exists. EditorUI is the opposite case and is covered below: its key is genuinely absent from
    // every file written so far, and the constructor is what has to answer.

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
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresTheDefaults()
        {
            var settings = new AudioSettings(0.2f, 0.3f, 0.4f, 0.1f);

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
            var source = new AudioSettings(0.9f, 0.8f, 0.7f, 0.6f);

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
    }
}
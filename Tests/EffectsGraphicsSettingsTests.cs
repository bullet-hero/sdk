using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups.Graphics;
using BH.SDK.Rules;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The two step budgets are the effect pool's phone lever, so what these pin is that they behave
    // like every other settings field: they survive a round trip, they take part in equality (a
    // settings screen decides whether to re-apply by comparing), and an older settings.json without
    // them falls back to the shipped defaults rather than to zero - a zero budget would stall every
    // effect replay instead of merely making it coarse.


    /// <summary> The two effect step budgets: round trip, equality, and an older settings file falling back to
    /// the shipped defaults rather than to a zero that would stall every replay. </summary>
    public class EffectsGraphicsSettingsTests
    {
        // A settings group carries no [ModelGeneration] - only the GameSettings root does - so it is
        // serialized as a plain nested object, and that is what these read and write directly.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreTheShippedBudgets()
        {
            var settings = new EffectsGraphicsSettings();

            Assert.AreEqual(EffectRules.ReplayStepBudget_Default, settings.ReplayStepBudget);
            Assert.AreEqual(EffectRules.FrameStepBudget_Default, settings.FrameStepBudget);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresTheBudgets()
        {
            var settings = new EffectsGraphicsSettings { ReplayStepBudget = 8, FrameStepBudget = 64 };

            settings.Reset();

            Assert.AreEqual(EffectRules.ReplayStepBudget_Default, settings.ReplayStepBudget);
            Assert.AreEqual(EffectRules.FrameStepBudget_Default, settings.FrameStepBudget);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyAndPull_CarryTheBudgets()
        {
            var source = new EffectsGraphicsSettings(true, FramerateTarget.Fixed, 50, 0.5f, 12, 128);

            var copy = source.Copy();
            var pulled = new EffectsGraphicsSettings();
            pulled.Pull(source);

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, pulled);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equals_SeparatesEachBudget()
        {
            var settings = new EffectsGraphicsSettings();

            Assert.AreNotEqual(settings, new EffectsGraphicsSettings { ReplayStepBudget = 16 });
            Assert.AreNotEqual(settings, new EffectsGraphicsSettings { FrameStepBudget = 512 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Serialization_RoundTripsTheBudgets()
        {
            var settings = new EffectsGraphicsSettings(true, FramerateTarget.Fixed, 50, 0.5f, 12, 128);

            var json = JsonConvert.SerializeObject(settings);
            var restored = JsonConvert.DeserializeObject<EffectsGraphicsSettings>(json);

            StringAssert.Contains(Models.Names.ReplayStepBudget, json);
            StringAssert.Contains(Models.Names.FrameStepBudget, json);
            Assert.AreEqual(settings, restored);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Deserialization_OlderFileWithoutBudgets_KeepsDefaults()
        {
            var json = $"{{\"{Models.Names.Render}\":true,\"{Models.Names.FpsTarget}\":2," +
                       $"\"{Models.Names.FpsFixed}\":50,\"{Models.Names.MaxScrubTime}\":0.5}}";

            var restored = JsonConvert.DeserializeObject<EffectsGraphicsSettings>(json);

            Assert.AreEqual(EffectRules.ReplayStepBudget_Default, restored.ReplayStepBudget);
            Assert.AreEqual(EffectRules.FrameStepBudget_Default, restored.FrameStepBudget);
        }
    }
}

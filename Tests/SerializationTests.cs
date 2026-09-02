using System.Linq;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Validations;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    public class SerializationTests
    {
        // THERE IS ONLY ONE SHAPE OF JSON HERE. A second mode used to write the same document
        // indented, and this test pinned that the two differed in whitespace alone; what it pins now
        // is that the survivor is the compact one, on every call, with nothing to leak between them.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void TestLevelSerializationIsAlwaysCompact()
        {
            var serializationService = new SerializationService(new SerializationSettings());
            var level = MockData.CreateTestLevel();

            var first = serializationService.SerializeData(level);
            var second = serializationService.SerializeData(level);

            Assert.IsFalse(first.Contains("\n"), "Compact output carries line breaks");
            Assert.AreEqual(first, second);

            var restored = serializationService.DeserializeData<Level>(first);
            Assert.IsTrue(level.Equals(restored));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestEffectSerialization()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var effect = MockData.CreateTestEffectData();

            var json = serializationService.SerializeData(effect);
            Cat.Meow($"Effect - <color=green>{json}</color>");

            var effect2 = serializationService.DeserializeData<EffectData>(json);
            Assert.IsTrue(effect.Equals(effect2));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void TestLevelSerialization()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var level = MockData.CreateTestLevel();
            var json = serializationService.SerializeData(level);
            Cat.Meow($"Level - <color=green>{json}</color>");

            var level2 = serializationService.DeserializeData<Level>(json);
            Assert.IsTrue(level.Equals(level2));
        }

        // Seed is the one LevelSettings field whose DEFAULT is the interesting case: a level that
        // never pinned a seed writes LevelRules.NullSeed, and a level written before the field
        // existed deserializes to the same thing. Both have to survive a round trip as zero, or an
        // unpinned level would silently become pinned to whatever the reader invented.
        [TestCase(0)]
        [TestCase(12345)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestLevelSeedRoundTrip(int seed)
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var level = MockData.CreateTestLevel();
            level.Settings.Seed = seed;

            var json = serializationService.SerializeData(level);
            var level2 = serializationService.DeserializeData<Level>(json);

            Assert.AreEqual(seed, level2.Settings.Seed);
            Assert.IsTrue(level.Equals(level2));
        }

        // IDataSerializer (VERSION-UPDATE.md, "Format-agnosticism") is generic per [DataVersion]
        // domain, not per concrete type - exercised here against two unrelated domains (Level and
        // Theme) to prove it isn't hardcoded to either one. Parametrized over SerializationType so
        // both implementations run through the same assertions - and unlike the JSON/BSON pair this
        // replaced, these two share NOTHING below the interface: one binds members by reflection
        // through Newtonsoft, the other is generated code on the models themselves. That is what
        // makes running the same assertions twice worth the seconds it costs.
        [TestCase(SerializationType.Json)]
        [TestCase(SerializationType.Blob)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestDataSerializerRoundTrip(SerializationType type)
        {
            var serializationService = new SerializationService(new SerializationSettings());
            var dataSerializer = serializationService.GetDataSerializer(type);
            Assert.AreEqual(type, dataSerializer.Type);

            var level = MockData.CreateTestLevel();
            var levelAttribute = level.GetType().GetCustomAttribute<DataVersionAttribute>();
            var levelBytes = dataSerializer.SerializeEnvelope(levelAttribute.Domain,
                new EnvelopeData(levelAttribute.Version, level));
            var levelEnvelope = dataSerializer.DeserializeEnvelope(levelBytes, typeof(Level));
            Assert.AreEqual(levelAttribute.Version, levelEnvelope.Version);
            Assert.IsTrue(level.Equals(levelEnvelope.GetPayload<Level>()));

            var theme = MockData.CreateTestTheme();
            var themeAttribute = theme.GetType().GetCustomAttribute<DataVersionAttribute>();
            var themeBytes = dataSerializer.SerializeEnvelope(themeAttribute.Domain,
                new EnvelopeData(themeAttribute.Version, theme));
            var themeEnvelope = dataSerializer.DeserializeEnvelope(themeBytes, typeof(ThemeData));
            Assert.AreEqual(themeAttribute.Version, themeEnvelope.Version);
            Assert.IsTrue(theme.Equals(themeEnvelope.GetPayload<ThemeData>()));

            var shape = MockData.CreateTestCompositeShape();
            var shapeAttribute = shape.GetType().GetCustomAttribute<DataVersionAttribute>();
            var shapeBytes = dataSerializer.SerializeEnvelope(shapeAttribute.Domain,
                new EnvelopeData(shapeAttribute.Version, shape));
            var shapeEnvelope = dataSerializer.DeserializeEnvelope(shapeBytes, typeof(CompositeShape));
            Assert.AreEqual(shapeAttribute.Version, shapeEnvelope.Version);
            Assert.IsTrue(shape.Equals(shapeEnvelope.GetPayload<CompositeShape>()));
        }

        // Exercises the full recursive migration chain against a v0.0 fixture (Versions/V0_0) -
        // Level -> LevelSettings/GameLevel/LevelResources (each independently
        // versioned, auto-upgraded by VersionedEnvelopeConverter) -> GameEvents (nested one level
        // deeper inside GameLevel) -> Audio (intentionally NOT independently versioned at v0.0,
        // migrated by hand inside LevelV0_0ToV1_0 instead). See VERSION-UPDATE.md. The fixture JSON
        // itself comes from MockDataSource.CreateTestLevelV0_0Json, built from the real VX_Y snapshot
        // classes rather than a hand-typed literal - see that method's own comment for why.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void TestLevelV0_0Migration()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var json = MockData.CreateTestLevelV0_0Json(serializationService);

            var level = serializationService.DeserializeData<Level>(json);

            Assert.AreEqual(61, level.Settings.Framerate);
            Assert.AreEqual(610, level.Settings.FrameDuration);
            Assert.IsNotNull(level.Game);
            Assert.IsNotNull(level.Game.Events);
            Assert.IsNotNull(level.Game.CameraEvents);
            Assert.IsNotNull(level.Game.PostProcessingEvents);
            Assert.IsNotNull(level.Game.PlayerEvents);
            Assert.AreEqual(1, level.Game.Objects.Count);
            Assert.IsNotNull(level.Audio);
            Assert.IsNotNull(level.Resources);

            // Migration-correctness oracle (see VERSION-UPDATE.md, Rule system section): a migrator's
            // output must never violate a RuleGroup.Error rule against the current-shape model, even
            // though Warning/Advice issues are allowed (e.g. a sparse fixture missing recommended data).
            var validator = new RuleAnalyzer();
            var issues = validator.Analyze(level, new RuleAnalyzerSettings());
            var errors = issues.Where(issue => issue.Rule.Group == RuleGroup.Error).ToList();
            Assert.IsEmpty(errors, string.Join("\n", errors.Select(issue => issue.ToString())));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestLevelMetaSerialization()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var levelMeta = MockData.CreateTestLevelMeta();
            var json = serializationService.SerializeData(levelMeta);
            Cat.Meow($"LevelMeta - <color=green>{json}</color>");

            var levelMeta2 = serializationService.DeserializeData<LevelMeta>(json);
            Assert.IsTrue(levelMeta.Equals(levelMeta2));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestPrefabSerialization()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var prefab = MockData.CreateTestPrefab();

            var json = serializationService.SerializeData(prefab);
            Cat.Meow($"Prefab - <color=green>{json}</color>");

            var prefab2 = serializationService.DeserializeData<Prefab>(json);
            Assert.IsTrue(prefab.Equals(prefab2));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestThemeSerialization()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var theme = MockData.CreateTestTheme();

            var json = serializationService.SerializeData(theme);
            Cat.Meow($"Theme - <color=green>{json}</color>");

            var theme2 = serializationService.DeserializeData<ThemeData>(json);
            Assert.IsTrue(theme.Equals(theme2));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestPlayerSettingsSerialization()
        {
            var settings = new SerializationSettings();
            var serializationService = new SerializationService(settings);

            var testSettings = MockData.CreateValidTestSettings();

            var json = serializationService.SerializeData(testSettings);
            Cat.Meow($"Settings - <color=green>{json}</color>");

            var testSettings2 = serializationService.DeserializeData<UserSettings>(json);
            Assert.IsTrue(testSettings.Equals(testSettings2));
        }
    }
}
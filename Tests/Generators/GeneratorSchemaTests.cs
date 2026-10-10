using System.Collections.Generic;
using System.Linq;
using BH.SDK.Agent;
using BH.SDK.Generators;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    /// <summary> The generator schema: every shipped generator describes itself in form order, and a caller's
    /// values read back through the same hints a form applies. </summary>
    public class GeneratorSchemaTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryRegisteredGenerator_ProducesASchema()
        {
            Assert.IsNotEmpty(GeneratorRegistry.All);
            foreach (var generator in GeneratorRegistry.All)
            {
                var schema = GeneratorSchema.Build(generator);
                Assert.AreEqual(generator.NameKey, (string)schema["id"], generator.NameKey);
                Assert.AreEqual(generator.Kind.ToString(), (string)schema["kind"], generator.NameKey);
                Assert.IsInstanceOf<JObject>(schema["properties"], generator.NameKey);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void FieldOrder_EqualsHintsOrder()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                var schema = GeneratorSchema.Build(generator);
                var order = ((JArray)schema[GeneratorSchema.OrderKey]).Select(t => (string)t).ToList();
                var properties = ((JObject)schema["properties"]).Properties().Select(p => p.Name).ToList();

                CollectionAssert.AreEqual(generator.Hints.Order, order, generator.NameKey);
                CollectionAssert.AreEqual(order, properties, generator.NameKey);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AField_MissingFromOrder_FollowsTheListedOnes()
        {
            var generator = new SpawnTestGenerator();
            var order = GeneratorSchema.OrderedFields(generator).Select(f => f.Name).ToList();
            CollectionAssert.AreEqual(new[] { "Count" }, order);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TheDefaults_ReadBackIntoAFreshInstance_ForEveryGenerator()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                var schema = GeneratorSchema.Build(generator);
                var values = new JObject();
                foreach (var property in ((JObject)schema["properties"]).Properties())
                    if (property.Value["default"] != null && !(bool)property.Value["x-readOnly"])
                        values[property.Name] = property.Value["default"];

                var parameters = generator.CreateDefaultParameters();
                var warnings = new List<string>();
                Assert.DoesNotThrow(() => GeneratorSchema.Apply(generator, parameters, values, warnings),
                    generator.NameKey);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ARange_IsInTheSchema_AndAnOutOfRangeValueIsClampedWithAWarning()
        {
            var generator = new SpawnTestGenerator();
            var schema = GeneratorSchema.Build(generator);
            Assert.AreEqual(1024f, (float)schema["properties"]["Count"]["maximum"]);

            var parameters = generator.CreateDefaultParameters();
            var warnings = new List<string>();
            GeneratorSchema.Apply(generator, parameters, JObject.Parse("{\"Count\": 5000}"), warnings);

            Assert.AreEqual(1024, ((SpawnTestGenerator.Parameters)parameters).Count);
            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("clamped", warnings[0]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnUnknownParameter_NamesTheOnesTheGeneratorHas()
        {
            var generator = new SpawnTestGenerator();
            var exception = Assert.Throws<AgentValueException>(() =>
                GeneratorSchema.Apply(generator, generator.CreateDefaultParameters(),
                    JObject.Parse("{\"Cnt\": 1}"), new List<string>()));
            StringAssert.Contains("Count", exception.Message);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AHostFilledField_IsMarkedInTheSchema_AndCannotBeSet()
        {
            var generator = new CameraFlashTestGenerator();
            var schema = GeneratorSchema.Build(generator);
            Assert.AreEqual("Int32[]", (string)schema["properties"]["Frames"]["x-hostFilled"]);

            Assert.Throws<AgentValueException>(() =>
                GeneratorSchema.Apply(generator, generator.CreateDefaultParameters(),
                    JObject.Parse("{\"Frames\": [1, 2]}"), new List<string>()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Visibility_IsStatedForTheDefaults_AndReadAgainstTheGivenValues()
        {
            var generator = new CameraFlashTestGenerator();
            var schema = GeneratorSchema.Build(generator);
            Assert.IsFalse((bool)schema["properties"]["Frames"]["x-visible"]);
            Assert.IsTrue((bool)schema["properties"]["Zoom"]["x-visible"]);

            CollectionAssert.AreEqual(new[] { "Frames" },
                GeneratorSchema.HiddenFields(generator, generator.CreateDefaultParameters()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ReadOnlyFields_AreIgnoredWithAWarning()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                var readOnly = GeneratorSchema.OrderedFields(generator)
                    .FirstOrDefault(f => generator.Hints.IsReadOnly(f.Name));
                if (readOnly == null) continue;

                var parameters = generator.CreateDefaultParameters();
                var before = readOnly.GetValue(parameters);
                var values = new JObject { [readOnly.Name] = AgentValueCodec.Encode(before) };
                var warnings = new List<string>();
                GeneratorSchema.Apply(generator, parameters, values, warnings);

                Assert.AreEqual(before, readOnly.GetValue(parameters), generator.NameKey);
                Assert.IsTrue(warnings.Any(w => w.Contains("read-only")), generator.NameKey);
                return;
            }

            Assert.Inconclusive("No shipped generator declares a read-only field.");
        }
    }
}

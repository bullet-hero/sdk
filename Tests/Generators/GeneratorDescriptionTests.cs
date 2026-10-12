using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Generators;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    /// <summary> Field descriptions: a client reading generator_schema cannot see the C# docs, so every field
    /// a form shows has to say in words what it means, and the schema has to carry it. </summary>
    public class GeneratorDescriptionTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryVisibleField_HasADescription_InHintsAndSchema()
        {
            var missing = new List<string>();
            foreach (var generator in GeneratorRegistry.All)
            {
                var defaults = generator.CreateDefaultParameters();
                var properties = (JObject)GeneratorSchema.Build(generator)["properties"];

                foreach (var field in GeneratorSchema.OrderedFields(generator))
                {
                    var property = (JObject)properties[field.Name];
                    if (property.ContainsKey("x-hostFilled")) continue;
                    if (!generator.Hints.IsVisible(field.Name, defaults)) continue;

                    var name = $"{generator.NameKey}.{field.Name}";
                    if (!generator.Hints.TryGetDescription(field.Name, out var text) || string.IsNullOrWhiteSpace(text))
                        missing.Add(name + " (no hint)");
                    else if ((string)property["description"] != text)
                        missing.Add(name + " (not in the schema)");
                }
            }

            Assert.IsEmpty(missing, string.Join("\n", missing));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryDescription_NamesAFieldOfItsGenerator()
        {
            var stray = new List<string>();
            foreach (var generator in GeneratorRegistry.All)
            {
                var fields = generator.ParametersType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var names = new HashSet<string>();
                foreach (var field in fields) names.Add(field.Name);

                foreach (var pair in generator.Hints.Descriptions)
                    if (!names.Contains(pair.Key))
                        stray.Add($"{generator.NameKey}.{pair.Key}");
            }

            Assert.IsEmpty(stray, string.Join("\n", stray));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFieldWithoutDescription_HasNoDescriptionKey()
        {
            var schema = GeneratorSchema.Build(new SpawnTestGenerator());
            var count = (JObject)schema["properties"]["Count"];
            Assert.IsFalse(count.ContainsKey("description"));
        }
    }
}

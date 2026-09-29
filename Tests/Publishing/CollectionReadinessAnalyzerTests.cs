using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models.Collections;
using BH.SDK.Models.Data;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Publishing;
using BH.SDK.Rules;
using NUnit.Framework;
using MetaAuthor = BH.SDK.Models.Meta.Author;

namespace BH.SDK.Tests.Publishing
{
    /// <summary>
    /// CollectionReadinessAnalyzer: whether a collection stands on its own - named, licensed, not empty,
    /// every file it names present, every user resource its entries point at carried along.
    /// </summary>
    public class CollectionReadinessAnalyzerTests
    {
        private static readonly DateTime Now = new(2027, 3, 14, 12, 0, 0, DateTimeKind.Utc);

        private static (ResourceCollection Manifest, LevelResources Resources) CreateClean()
        {
            var manifest = new ResourceCollection
            {
                Name = new StringValue("pack"),
                License = new TypicalLicense(TypicalLicenseType.CC0_1_0),
                Authors = new List<MetaAuthor> { new(new StringValue("vertoker"), "https://vertoker.com") },
            };

            var texture = new TextureResource(new TextureResourceId(-1),
                new List<ResourceKey> { new(ResourceUriType.LevelPath, "media/a.png") });
            manifest.Textures.Add(texture.TextureResourceId, texture);

            var resources = new LevelResources
            {
                Textures = manifest.Textures,
                Fonts = manifest.Fonts,
                Audios = manifest.Audios,
            };
            return (manifest, resources);
        }

        private static PublishReadinessReport Analyze(ResourceCollection manifest, LevelResources resources,
            ISet<string> files, long bytes = 0, PublishProfile profile = null)
            => new CollectionReadinessAnalyzer().Analyze(manifest, resources, profile ?? PublishProfile.CreateOpen(),
                files, bytes, Now);

        private static IEnumerable<PublishRule> Rules(PublishReadinessReport report)
            => report.Issues.Where(issue => issue.Group == RuleGroup.Error).Select(issue => issue.Rule);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ACompleteCollection_HasNoErrors()
        {
            var (manifest, resources) = CreateClean();
            var report = Analyze(manifest, resources, new HashSet<string> { "collection.json", "media/a.png" });
            Assert.That(report.HasErrors, Is.False, string.Join("\n", report.Issues));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ABlankCollection_IsNamelessUnlicensedAndEmpty()
        {
            var manifest = new ResourceCollection();
            var report = Analyze(manifest, new LevelResources(), new HashSet<string>(),
                profile: PublishProfile.CreateStandard());
            Assert.That(Rules(report), Is.SupersetOf(new[]
            {
                PublishRule.CollectionNameMissing, PublishRule.CollectionLicenseUnspecified,
                PublishRule.CollectionEmpty,
            }));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AMissingLicense_WarnsWhereTheProfileAllowsOne_AndBlocksWhereItDoesNot()
        {
            var (manifest, resources) = CreateClean();
            manifest.License = new NoSpecifiedLicense();
            var files = new HashSet<string> { "collection.json", "media/a.png" };

            var open = Analyze(manifest, resources, files, profile: PublishProfile.CreateOpen());
            Assert.That(open.HasErrors, Is.False, string.Join("; ", open.Issues));
            Assert.That(open.Issues.Select(issue => issue.Rule), Has.Member(PublishRule.CollectionLicenseUnspecified));

            var standard = Analyze(manifest, resources, files, profile: PublishProfile.CreateStandard());
            Assert.That(standard.HasErrors, Is.True);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFileTheManifestNames_MustBeInTheCollection()
        {
            var (manifest, resources) = CreateClean();
            var report = Analyze(manifest, resources, new HashSet<string> { "collection.json" });
            Assert.That(Rules(report), Has.Member(PublishRule.CollectionMediaMissing));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AUserShapeAnEffectNeeds_MustBeCarried()
        {
            var (manifest, resources) = CreateClean();
            var effect = new EffectData { EffectId = EffectId.NewGuid(), Name = "sparks" };
            effect.Core.ParticleShapeId = ShapeId.NewGuid();
            resources.Effects.Add(effect.EffectId, effect);

            var missing = Analyze(manifest, resources, new HashSet<string> { "media/a.png" });
            Assert.That(Rules(missing), Has.Member(PublishRule.CollectionReferenceMissing));

            var shape = new CompositeShape { ShapeId = effect.Core.ParticleShapeId, ShapeName = "spark" };
            resources.CompositeShapes.Add(shape.ShapeId, shape);
            var carried = Analyze(manifest, resources, new HashSet<string> { "media/a.png" });
            Assert.That(Rules(carried), Has.No.Member(PublishRule.CollectionReferenceMissing));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheProfilesSizeCap_AppliesToTheWholeFolder()
        {
            var (manifest, resources) = CreateClean();
            var profile = PublishProfile.CreateOpen();
            profile.MaxTotalBytes = 100;

            var report = Analyze(manifest, resources, new HashSet<string> { "media/a.png" }, 101, profile);
            Assert.That(Rules(report), Has.Member(PublishRule.PayloadTooLarge));
        }
    }
}
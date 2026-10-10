using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Validations;
using NUnit.Framework;

using ResourceMeta = BH.SDK.Models.Meta.ResourceMeta;

namespace BH.SDK.Tests
{
    /// <summary>
    /// The ILicense family; NoSpecifiedLicense gained NoLicenseSourceType, so its equality is "same
    /// origin", not "always true" - a change that breaks dictionaries and dirty checks silently.
    /// </summary>
    public class LicenseTests
    {
        private static ResourceMeta CreateRecord(ILicense license) => new()
        {
            ResourceType = ResourceType.Audio,
            ResourceId = new TypedResourceId(-1),
            ResourceTitle = new StringValue("a track"),
            ResourceUrl = "https://example.com/track",
            ResourceLicense = license,
        };

        #region NoSpecifiedLicense

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestDefaultIsUndefinedSource()
        {
            Assert.AreEqual(NoLicenseSourceType.Undefined, new NoSpecifiedLicense().Source);
            Assert.AreEqual(LicenseType.NoSpecified, new NoSpecifiedLicense().GetModelType());
        }

        // Before Source existed every NoSpecifiedLicense equalled every other one, so a record that
        // named YouTube compared equal to one that named nothing.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEqualityFollowsSource()
        {
            var youtube = new NoSpecifiedLicense(NoLicenseSourceType.YouTube);
            var spotify = new NoSpecifiedLicense(NoLicenseSourceType.Spotify);
            var undefined = new NoSpecifiedLicense();

            Assert.IsTrue(youtube.Equals(new NoSpecifiedLicense(NoLicenseSourceType.YouTube)));
            Assert.IsFalse(youtube.Equals(spotify));
            Assert.IsFalse(youtube.Equals(undefined));

            Assert.AreEqual(youtube.GetHashCode(),
                new NoSpecifiedLicense(NoLicenseSourceType.YouTube).GetHashCode());
            Assert.AreNotEqual(youtube.GetHashCode(), spotify.GetHashCode());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEqualityAcrossLicenseKinds()
        {
            ILicense unlicensed = new NoSpecifiedLicense(NoLicenseSourceType.YouTube);
            ILicense typical = new TypicalLicense(TypicalLicenseType.CC0_1_0);

            Assert.IsFalse(unlicensed.Equals(typical));
            Assert.IsFalse(typical.Equals(unlicensed));
            Assert.IsFalse(unlicensed.Equals(null));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestCopyAndReset()
        {
            var license = new NoSpecifiedLicense(NoLicenseSourceType.SoundCloud);

            var copy = license.Copy();
            Assert.AreEqual(NoLicenseSourceType.SoundCloud, copy.Source);
            Assert.IsTrue(license.Equals(copy));

            copy.Reset();
            Assert.AreEqual(NoLicenseSourceType.Undefined, copy.Source);
            Assert.IsFalse(license.Equals(copy));
            Assert.AreEqual(NoLicenseSourceType.SoundCloud, license.Source);
        }

        // ILicense is polymorphic through a two-element array, so a variant that gains its first
        // field is exactly where the payload half of that array can quietly stop round-tripping.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestSourceSurvivesRoundTrip()
        {
            var serializationService = new SerializationService(new SerializationSettings());

            var meta = new LevelMeta
            {
                LevelLicense = new NoSpecifiedLicense(NoLicenseSourceType.Spotify),
                ResourcesMeta = new List<ResourceMeta>
                {
                    CreateRecord(new NoSpecifiedLicense(NoLicenseSourceType.YouTube)),
                    CreateRecord(new NoSpecifiedLicense()),
                },
            };

            var json = serializationService.SerializeData(meta);
            var restored = serializationService.DeserializeData<LevelMeta>(json);

            Assert.IsTrue(meta.Equals(restored));
            Assert.AreEqual(NoLicenseSourceType.Spotify,
                ((NoSpecifiedLicense)restored.LevelLicense).Source);
            Assert.AreEqual(NoLicenseSourceType.YouTube,
                ((NoSpecifiedLicense)restored.ResourcesMeta[0].ResourceLicense).Source);
            Assert.AreEqual(NoLicenseSourceType.Undefined,
                ((NoSpecifiedLicense)restored.ResourcesMeta[1].ResourceLicense).Source);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUndeclaredSourceIsRepaired()
        {
            var analyzer = new RuleAnalyzer();
            var settings = new RuleAnalyzerSettings(true, true);
            var license = new NoSpecifiedLicense((NoLicenseSourceType)200);

            Assert.IsNotEmpty(analyzer.Analyze(license, settings));

            new RuleFixer().FixUntilStable(analyzer, license, settings, new RuleFixerSettings());

            Assert.AreEqual(NoLicenseSourceType.Undefined, license.Source);
        }

        #endregion

        #region TypicalLicenseType

        // The 150+ range held NoLicense_SourcedFrom_* values that named a platform rather than a
        // license. They moved to NoLicenseSourceType, and nothing may reuse those numbers: a level
        // written before the move would deserialize into whatever took its place.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestPlatformValuesAreGone()
        {
            foreach (TypicalLicenseType type in System.Enum.GetValues(typeof(TypicalLicenseType)))
            {
                Assert.Less((short)type, 150,
                    $"{type} sits in the range vacated by the NoLicense_SourcedFrom_* values");
            }
        }

        #endregion
    }
}

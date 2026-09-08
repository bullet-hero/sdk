using System.Collections.Generic;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Publishing;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using NUnit.Framework;

namespace BH.SDK.Tests.Publishing
{
    /// <summary>
    /// PublishProfile and the site roster it grades against: the policy-as-data half of publishing.
    /// A profile is a file an operator edits, so it has to survive a round trip and match hosts the
    /// way a person typing a URL expects.
    /// </summary>
    public class PublishProfileTests
    {
        #region Host matching

        [TestCase("https://freesound.org/people/x/sounds/1/", "freesound.org")]
        [TestCase("https://www.freesound.org/", "freesound.org")]
        [TestCase("http://WWW.Freesound.ORG/x", "freesound.org")]
        [TestCase("freesound.org/people/x", "freesound.org")]
        [TestCase("", "")]
        [TestCase("   ", "")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestExtractHost(string url, string expected)
        {
            Assert.AreEqual(expected, TrustedSource.ExtractHost(url));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestCoversHostIncludesSubdomains()
        {
            var source = new TrustedSource("x", "X", "https://x.test/",
                new List<string> { "x.test" }, SourceTrust.Approved,
                new List<TypicalLicenseType>(), string.Empty);

            Assert.IsTrue(source.CoversHost("x.test"));
            Assert.IsTrue(source.CoversHost("cdn.x.test"));
            Assert.IsFalse(source.CoversHost("notx.test"));
            Assert.IsFalse(source.CoversHost(string.Empty));
        }

        #endregion

        #region Grading

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestGetTrustUsesRoster()
        {
            var profile = PublishProfile.CreateStandard();

            Assert.AreEqual(SourceTrust.Approved, profile.GetTrust("https://kenney.nl/assets/ui-audio"));
            Assert.AreEqual(SourceTrust.NotAllowed, profile.GetTrust("https://youtu.be/abc"));
            Assert.AreEqual(SourceTrust.RequiresLicenseCheck,
                profile.GetTrust("https://opengameart.org/content/x"));
        }

        // An empty roster is "source grading is switched off", not "every site is unknown" - the
        // difference is what keeps a purely local profile from flagging every resource a player owns.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEmptyRosterApprovesEverything()
        {
            var profile = PublishProfile.CreateOpen();

            Assert.AreEqual(SourceTrust.Approved, profile.GetTrust("https://anything.example/x"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestUnknownSourceFallsBackToProfile()
        {
            var profile = PublishProfile.CreateStandard();

            Assert.AreEqual(profile.UnknownSourceTrust, profile.GetTrust("https://unheard-of.example/x"));
            Assert.AreNotEqual(SourceTrust.Approved, profile.UnknownSourceTrust,
                "a site nobody has vetted must never grade as approved");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEmptyAllowListAcceptsEveryLicense()
        {
            var open = PublishProfile.CreateOpen();
            var standard = PublishProfile.CreateStandard();

            Assert.IsTrue(open.AllowsLicense(TypicalLicenseType.Proprietary));
            Assert.IsFalse(standard.AllowsLicense(TypicalLicenseType.Proprietary));
            Assert.IsTrue(standard.AllowsLicense(TypicalLicenseType.CC0_1_0));
        }

        // Not a style preference: a GPL work redistributed through the App Store collides with
        // Apple's own terms, so a catalogue that accepted GPL could never be served to iOS later.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestStandardProfileRefusesStoreIncompatibleLicenses()
        {
            var profile = PublishProfile.CreateStandard();

            Assert.IsFalse(profile.AllowsLicense(TypicalLicenseType.GPL_3_0));
            Assert.IsFalse(profile.AllowsLicense(TypicalLicenseType.AGPL_3_0));
            Assert.IsFalse(profile.AllowsLicense(TypicalLicenseType.LGPL_3_0));
            Assert.IsFalse(profile.AllowsLicense(TypicalLicenseType.CC_BY_SA_4_0));
            Assert.IsFalse(profile.AllowsLicense(TypicalLicenseType.CC_BY_ND_4_0));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestStrictProfileRefusesArbitraryUrls()
        {
            var standard = PublishProfile.CreateStandard();
            var strict = PublishProfile.CreateStrict();

            Assert.IsTrue(standard.AllowsUriType(ResourceUriType.DirectUrl));
            Assert.IsFalse(strict.AllowsUriType(ResourceUriType.DirectUrl));
            Assert.IsTrue(strict.AllowsUriType(ResourceUriType.LevelPath));
        }

        #endregion

        #region Serialization

        // A profile is a file an operator keeps for years while its shape moves under them, which is
        // why it carries [ModelGeneration] and goes through SerializeData like any other aggregate root.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestProfileRoundTrip()
        {
            var serializationService = new SerializationService(new SerializationSettings());
            var profile = PublishProfile.CreateStrict();

            var json = serializationService.SerializeData(profile);
            var restored = serializationService.DeserializeData<PublishProfile>(json);

            Assert.IsTrue(profile.Equals(restored), json);
        }

        // Limits are what the store build differs by as much as its license list - a level that fits
        // the server can still be too heavy for a phone.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestSizeLimitsTightenWithTheProfile()
        {
            var open = PublishProfile.CreateOpen();
            var standard = PublishProfile.CreateStandard();
            var strict = PublishProfile.CreateStrict();

            Assert.IsFalse(open.HasSizeLimits);
            Assert.IsTrue(standard.HasSizeLimits);
            Assert.Less(strict.MaxResourceBytes, standard.MaxResourceBytes);
            Assert.Less(strict.MaxDataFileBytes, standard.MaxDataFileBytes);
            Assert.Less(strict.MaxTotalBytes, standard.MaxTotalBytes);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCopyIsIndependent()
        {
            var profile = PublishProfile.CreateStandard();
            var copy = profile.Copy();

            Assert.IsTrue(profile.Equals(copy));

            copy.AllowedLicenses.Clear();
            copy.Sources.Clear();

            Assert.IsFalse(profile.Equals(copy));
            Assert.IsNotEmpty(profile.AllowedLicenses);
            Assert.IsNotEmpty(profile.Sources);
        }

        #endregion
    }
}

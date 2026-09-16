using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Publishing;
using BH.SDK.Rules;
using BH.SDK.Utils;
using NUnit.Framework;

// Models.Meta is imported member by member rather than as a namespace: its Author class and NUnit's
// [Author] attribute collide, and every test file here carries the attribute.
using MetaAuthor = BH.SDK.Models.Meta.Author;
using PermissionGrant = BH.SDK.Models.Meta.PermissionGrant;
using ResourceMeta = BH.SDK.Models.Meta.ResourceMeta;

namespace BH.SDK.Tests.Publishing
{
    /// <summary>
    /// PublishReadinessAnalyzer: whether a level may be handed to a service. Every case here is one
    /// the licensing policy names in prose, so a change in what these assert is a change of policy,
    /// not of implementation.
    /// </summary>
    public class PublishReadinessAnalyzerTests
    {
        private static readonly DateTime Now = new(2027, 3, 14, 12, 0, 0, DateTimeKind.Utc);

        #region Fixtures

        private static LevelMeta CreateCleanMeta()
        {
            var meta = new LevelMeta
            {
                LevelName = new StringValue("clean level"),
                LevelLicense = new TypicalLicense(TypicalLicenseType.CC_BY_NC_4_0),
                LevelAgeRating = AgeRating.Age12,
                LevelLogo = new ResourceKey(ResourceUriType.LevelPath, "logo.png"),
                LevelAuthors = new List<MetaAuthor>
                {
                    new(new StringValue("vertoker"), "https://vertoker.com"),
                },
            };
            meta.ResourcesMeta.Add(CreateResourceMeta(TypicalLicenseType.CC0_1_0));
            return meta;
        }

        private static ResourceMeta CreateResourceMeta(TypicalLicenseType license)
        {
            var resourceMeta = new ResourceMeta
            {
                ResourceType = ResourceType.Audio,
                ResourceId = new TypedResourceId(-1),
                ResourceTitle = new StringValue("a track"),
                ResourceUrl = "https://freesound.org/people/someone/sounds/1/",
                ResourceLicense = new TypicalLicense(license),
                ResourceAuthors = new List<MetaAuthor>
                {
                    new(new StringValue("someone"), "https://freesound.org/people/someone/"),
                },
            };
            resourceMeta.ResourceHashes.Add("sha256:0123456789abcdef");
            return resourceMeta;
        }

        private static PermissionGrant CreatePermission(DateTime expiresAt)
            => new(new MetaAuthor(new StringValue("rights holder"), "https://example.com"),
                PermissionScope.AnyLevel, Now.AddDays(-30), expiresAt,
                "https://x.com/rightsholder/status/1", string.Empty);

        private static Level CreateLevelWithAudio(int id, ResourceUriType uriType)
        {
            var level = new Level();
            level.Resources.Audios.Add(new AudioResourceId(id), new AudioResource(
                new AudioResourceId(id), new List<ResourceKey>
                {
                    new(uriType, "track.ogg"),
                }));
            return level;
        }

        private static PublishPayload CreatePayload(long resourceBytes)
        {
            var payload = new PublishPayload
            {
                LevelBytes = 256 * ByteSizeUtils.Kilobyte,
                MetaBytes = 4 * ByteSizeUtils.Kilobyte,
                TotalBytes = resourceBytes + 260 * ByteSizeUtils.Kilobyte,
            };
            payload.SetResourceBytes(ResourceType.Audio, new TypedResourceId(-1), resourceBytes);
            return payload;
        }

        private static bool Has(PublishReadinessReport report, PublishRule rule)
            => report.Issues.Any(issue => issue.Rule == rule);

        private static PublishIssue Get(PublishReadinessReport report, PublishRule rule)
            => report.Issues.First(issue => issue.Rule == rule);

        #endregion

        #region Level meta

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCleanMetaPassesStandard()
        {
            var report = new PublishReadinessAnalyzer()
                .Analyze(CreateCleanMeta(), PublishProfile.CreateStandard(), null, Now);

            Assert.IsFalse(report.HasErrors, report.ToString());
            Assert.IsFalse(report.NeedsManualReview, report.ToString());
        }

        // A meta-only pass cannot see a resource that has no record at all, so it is never allowed to
        // report readiness - only the absence of anything wrong in what it did read.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMetaOnlyPassIsNeverReady()
        {
            var report = new PublishReadinessAnalyzer()
                .Analyze(CreateCleanMeta(), PublishProfile.CreateStandard(), null, Now);

            Assert.IsFalse(report.LevelInspected);
            Assert.IsFalse(report.IsReady);
            Assert.AreEqual(0, report.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFullPassIsReady()
        {
            var report = new PublishReadinessAnalyzer().Analyze(CreateCleanMeta(),
                PublishProfile.CreateStandard(), CreateLevelWithAudio(-1, ResourceUriType.LevelPath),
                Now, CreatePayload(4 * ByteSizeUtils.Megabyte));

            Assert.IsTrue(report.LevelInspected);
            Assert.IsTrue(report.PayloadInspected);
            Assert.IsTrue(report.IsReady, report.ToString());
        }

        // A profile that bounds sizes cannot call a level ready off a check that measured none of
        // them - and one that bounds nothing must not demand measurements to reach a verdict.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnmeasuredLevelIsNotReadyOnlyWhenSizesAreBounded()
        {
            var analyzer = new PublishReadinessAnalyzer();
            var meta = CreateCleanMeta();
            var level = CreateLevelWithAudio(-1, ResourceUriType.LevelPath);

            var bounded = analyzer.Analyze(meta, PublishProfile.CreateStandard(), level, Now);

            var unbounded = PublishProfile.CreateStandard();
            unbounded.MaxResourceBytes = 0;
            unbounded.MaxDataFileBytes = 0;
            unbounded.MaxTotalBytes = 0;

            var report = analyzer.Analyze(meta, unbounded, level, Now);

            Assert.IsFalse(bounded.IsReady, "sizes are bounded and nothing was measured");
            Assert.IsFalse(bounded.HasErrors, "an unmeasured level is incomplete, not refused");
            Assert.IsTrue(report.IsReady, report.ToString());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnratedLevelIsRefused()
        {
            var meta = CreateCleanMeta();
            meta.LevelAgeRating = AgeRating.Unrated;

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsTrue(Has(report, PublishRule.LevelAgeRatingMissing));
            Assert.IsTrue(report.HasErrors);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUncreditedLevelIsRefused()
        {
            var meta = CreateCleanMeta();
            meta.LevelAuthors.Clear();

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsTrue(Has(report, PublishRule.LevelAuthorsMissing));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestOpenProfileRefusesNothing()
        {
            var meta = CreateCleanMeta();
            meta.LevelAgeRating = AgeRating.Unrated;
            meta.LevelAuthors.Clear();
            meta.ResourcesMeta[0].ResourceLicense = new NoSpecifiedLicense();
            meta.ResourcesMeta[0].ResourceUrl = "https://www.youtube.com/watch?v=x";

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateOpen(), null, Now);

            Assert.AreEqual(0, report.Count, report.ToString());
        }

        #endregion

        #region Resource licensing

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestProprietaryResourceIsRefused()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceLicense = new TypicalLicense(TypicalLicenseType.Proprietary);

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.AreEqual(RuleGroup.Error, Get(report, PublishRule.ResourceLicenseNotAllowed).Group);
        }

        // The default a fresh record carries. It must read as "unknown", never as permission - this
        // is the case the old CC BY-NC constructor default silently passed.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnfilledRecordIsRefused()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceLicense = new ResourceMeta().ResourceLicense;

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.AreEqual(RuleGroup.Error, Get(report, PublishRule.ResourceLicenseUnspecified).Group);
        }

        // Naming the platform is not a license and must not soften the verdict - it only makes the
        // message actionable.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNamedUnlicensedSourceIsStillRefused()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceLicense = new NoSpecifiedLicense(NoLicenseSourceType.YouTube);

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            var issue = Get(report, PublishRule.ResourceLicenseUnspecified);
            Assert.AreEqual(RuleGroup.Error, issue.Group);
            StringAssert.Contains("YouTube", issue.Message);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCustomLicenseIsRefused()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceLicense = new CustomLicense("studio terms",
                "https://example.com", "...");

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsTrue(Has(report, PublishRule.ResourceLicenseNotAllowed));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUncreditedResourceIsRefusedWhenTheProfileAsksForAttribution()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceAuthors.Clear();

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStrict(), null, Now);

            Assert.AreEqual(RuleGroup.Error,
                Get(report, PublishRule.ResourceAttributionMissing).Group);
        }

        #endregion

        #region Permissions

        // Option B in full: a license the service refuses, carried by a permission that names a
        // scope and points at evidence. The outcome is a review, never a silent pass - the evidence
        // exists to be opened by somebody.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPermissionTurnsRefusalIntoReview()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceLicense = new TypicalLicense(TypicalLicenseType.Proprietary);
            meta.ResourcesMeta[0].ResourcePermissions.Add(CreatePermission(default));

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsFalse(report.HasErrors, report.ToString());
            Assert.IsTrue(report.NeedsManualReview);
            Assert.AreEqual(RuleGroup.Warning, Get(report, PublishRule.ResourceLicenseNotAllowed).Group);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestExpiredPermissionDoesNotCarryARefusal()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceLicense = new TypicalLicense(TypicalLicenseType.Proprietary);
            meta.ResourcesMeta[0].ResourcePermissions.Add(CreatePermission(Now.AddDays(-1)));

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsTrue(report.HasErrors);
            Assert.IsTrue(Has(report, PublishRule.PermissionExpired));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPermissionWithoutProofIsIncomplete()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceLicense = new TypicalLicense(TypicalLicenseType.Proprietary);
            meta.ResourcesMeta[0].ResourcePermissions.Add(new PermissionGrant
            {
                Scope = PermissionScope.AnyLevel,
            });

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsTrue(Has(report, PublishRule.PermissionIncomplete));
            Assert.IsTrue(report.HasErrors, "an unverifiable permission carries nothing");
        }

        // An unset `now` means "this caller has no clock" (an offline client), and a grant must not
        // silently lapse because nobody passed the time in.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNoClockNeverExpiresAGrant()
        {
            var permission = CreatePermission(Now.AddDays(-1));

            Assert.IsFalse(permission.IsActiveAt(Now));
            Assert.IsTrue(permission.IsActiveAt(default));
        }

        #endregion

        #region Sources

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestStreamingSourceIsRefused()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceUrl = "https://www.youtube.com/watch?v=NH-GAwLAO30";

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.AreEqual(RuleGroup.Error, Get(report, PublishRule.SourceNotAllowed).Group);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnknownSourceIsReviewed()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceUrl = "https://some-blog.example/track";

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsFalse(report.HasErrors);
            Assert.IsTrue(Has(report, PublishRule.SourceUnknown));
        }

        // A rostered site and an unrostered one usually carry the same grade, so the grade cannot be
        // what tells the two findings apart - OpenGameArt was vetted and needs its upload read,
        // some-blog.example was never looked at by anyone.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMixedTermsSourceIsReviewed()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceUrl = "https://opengameart.org/content/something";

            var report = new PublishReadinessAnalyzer()
                .Analyze(meta, PublishProfile.CreateStandard(), null, Now);

            Assert.IsFalse(report.HasErrors);

            var issue = Get(report, PublishRule.SourceNeedsReview);
            Assert.AreEqual(RuleGroup.Warning, issue.Group);
            StringAssert.Contains("OpenGameArt", issue.Message);
            Assert.IsFalse(Has(report, PublishRule.SourceUnknown));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnknownSourceSeverityFollowsTheProfile()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceUrl = "https://some-blog.example/track";

            var profile = PublishProfile.CreateStandard();
            profile.UnknownSourceTrust = SourceTrust.NotAllowed;

            var report = new PublishReadinessAnalyzer().Analyze(meta, profile, null, Now);

            Assert.AreEqual(RuleGroup.Error, Get(report, PublishRule.SourceUnknown).Group);
        }

        #endregion

        #region Sizes

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestOversizedResourceIsRefused()
        {
            var report = new PublishReadinessAnalyzer().Analyze(CreateCleanMeta(),
                PublishProfile.CreateStandard(), CreateLevelWithAudio(-1, ResourceUriType.LevelPath),
                Now, CreatePayload(128 * ByteSizeUtils.Megabyte));

            var issue = Get(report, PublishRule.ResourceTooLarge);
            Assert.AreEqual(RuleGroup.Error, issue.Group);
            StringAssert.Contains("128 MB", issue.Message);
        }

        // The same level passes the server's profile and fails the store build's - which is the
        // whole reason a limit is a number in a profile rather than a constant in the analyzer.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestResourceLimitFollowsTheProfile()
        {
            var analyzer = new PublishReadinessAnalyzer();
            var meta = CreateCleanMeta();
            var level = CreateLevelWithAudio(-1, ResourceUriType.LevelPath);
            var payload = CreatePayload(48 * ByteSizeUtils.Megabyte);

            var standard = analyzer.Analyze(meta, PublishProfile.CreateStandard(), level, Now, payload);
            var strict = analyzer.Analyze(meta, PublishProfile.CreateStrict(), level, Now, payload);

            Assert.IsFalse(Has(standard, PublishRule.ResourceTooLarge));
            Assert.IsTrue(Has(strict, PublishRule.ResourceTooLarge));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestOversizedDataFileIsRefused()
        {
            var payload = CreatePayload(ByteSizeUtils.Megabyte);
            payload.LevelBytes = 64 * ByteSizeUtils.Megabyte;

            var report = new PublishReadinessAnalyzer().Analyze(CreateCleanMeta(),
                PublishProfile.CreateStandard(), CreateLevelWithAudio(-1, ResourceUriType.LevelPath),
                Now, payload);

            Assert.AreEqual(RuleGroup.Error, Get(report, PublishRule.DataFileTooLarge).Group);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestOversizedPayloadIsRefused()
        {
            var payload = CreatePayload(ByteSizeUtils.Megabyte);
            payload.TotalBytes = 512 * ByteSizeUtils.Megabyte;

            var report = new PublishReadinessAnalyzer().Analyze(CreateCleanMeta(),
                PublishProfile.CreateStandard(), CreateLevelWithAudio(-1, ResourceUriType.LevelPath),
                Now, payload);

            Assert.AreEqual(RuleGroup.Error, Get(report, PublishRule.PayloadTooLarge).Group);
        }

        // Zero is "nobody measured this", and a file nobody measured must not be reported as
        // comfortably within a limit it was never checked against.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnmeasuredFilesAreNotJudged()
        {
            var report = new PublishReadinessAnalyzer().Analyze(CreateCleanMeta(),
                PublishProfile.CreateStrict(), CreateLevelWithAudio(-1, ResourceUriType.LevelPath),
                Now, new PublishPayload());

            Assert.IsFalse(Has(report, PublishRule.ResourceTooLarge));
            Assert.IsFalse(Has(report, PublishRule.DataFileTooLarge));
            Assert.IsFalse(Has(report, PublishRule.PayloadTooLarge));
        }

        #endregion

        #region Level pass

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestResourceWithoutRecordIsRefused()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta.Clear();

            var report = new PublishReadinessAnalyzer().Analyze(meta,
                PublishProfile.CreateStandard(), CreateLevelWithAudio(-1, ResourceUriType.LevelPath), Now);

            Assert.AreEqual(RuleGroup.Error, Get(report, PublishRule.ResourceMetaMissing).Group);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestRecordWithoutResourceIsAdviceOnly()
        {
            var report = new PublishReadinessAnalyzer().Analyze(CreateCleanMeta(),
                PublishProfile.CreateStandard(), CreateLevelWithAudio(-2, ResourceUriType.LevelPath), Now);

            Assert.AreEqual(RuleGroup.Advice, Get(report, PublishRule.ResourceMetaOrphaned).Group);
            Assert.IsTrue(Has(report, PublishRule.ResourceMetaMissing));
        }

        // The store profile's whole point: an arbitrary URL is a fetch nobody moderated, and the
        // standard profile allows it while the strict one does not.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestDirectUrlIsRefusedOnlyByStrict()
        {
            var meta = CreateCleanMeta();
            var level = CreateLevelWithAudio(-1, ResourceUriType.DirectUrl);
            var analyzer = new PublishReadinessAnalyzer();

            var standard = analyzer.Analyze(meta, PublishProfile.CreateStandard(), level, Now);
            var strict = analyzer.Analyze(meta, PublishProfile.CreateStrict(), level, Now);

            Assert.IsFalse(Has(standard, PublishRule.ResourceUriTypeNotAllowed));
            Assert.AreEqual(RuleGroup.Error, Get(strict, PublishRule.ResourceUriTypeNotAllowed).Group);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestStrictRequiresHashes()
        {
            var meta = CreateCleanMeta();
            meta.ResourcesMeta[0].ResourceHashes.Clear();

            var report = new PublishReadinessAnalyzer().Analyze(meta,
                PublishProfile.CreateStrict(), CreateLevelWithAudio(-1, ResourceUriType.LevelPath), Now);

            Assert.IsTrue(Has(report, PublishRule.ResourceHashMissing));
        }

        #endregion
    }
}

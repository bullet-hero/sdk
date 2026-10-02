using System;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Profile;
using BH.SDK.Services.Profile;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    /// <summary> Which incoming level a merge adds, replaces or skips - matched by id, decided by time. </summary>
    public class ProfileMergePlannerTests
    {
        private static readonly DateTime Older = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Newer = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheNewerCopyWins_AndReplacesTheLocalFolder()
        {
            var id = LevelId.NewId();
            var plan = ProfileMergePlanner.PlanLevels(
                new[] { new ProfileLevelEntry(id, "theirs", Newer) },
                new[] { new ProfileLevelEntry(id, "mine", Older) });

            Assert.AreEqual(1, plan.Replaced.Count);
            Assert.AreEqual("mine", plan.Replaced[0].TargetFolder);
            Assert.AreEqual("mine", plan.Replaced[0].ReplacedFolder);
            Assert.IsEmpty(plan.Added);
            Assert.IsEmpty(plan.KeptLocal);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ATieOrAnOlderIncomingCopy_KeepsTheLocalOne(int incomingDays)
        {
            var id = LevelId.NewId();
            var plan = ProfileMergePlanner.PlanLevels(
                new[] { new ProfileLevelEntry(id, "theirs", Older.AddDays(incomingDays)) },
                new[] { new ProfileLevelEntry(id, "mine", Older) });

            Assert.AreEqual(1, plan.KeptLocal.Count);
            Assert.IsEmpty(plan.Replaced);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AnUnknownLevel_IsAddedUnderItsOwnFolder()
        {
            var plan = ProfileMergePlanner.PlanLevels(
                new[] { new ProfileLevelEntry(LevelId.NewId(), "new one", Newer) },
                Array.Empty<ProfileLevelEntry>());

            Assert.AreEqual(1, plan.Added.Count);
            Assert.AreEqual("new one", plan.Added[0].TargetFolder);
            Assert.IsNull(plan.Added[0].ReplacedFolder);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ADifferentLevelUnderATakenFolderName_IsPlacedUnderItsId()
        {
            var incoming = LevelId.NewId();
            var plan = ProfileMergePlanner.PlanLevels(
                new[] { new ProfileLevelEntry(incoming, "Shared Name", Newer) },
                new[] { new ProfileLevelEntry(LevelId.NewId(), "shared name", Older) });

            Assert.AreEqual(1, plan.Added.Count);
            Assert.AreEqual(ProfileMergePlanner.FolderOf(incoming), plan.Added[0].TargetFolder);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void WhenTheIdIsTakenAsAFolderToo_ANumberIsAppended()
        {
            var incoming = LevelId.NewId();
            var plan = ProfileMergePlanner.PlanLevels(
                new[] { new ProfileLevelEntry(incoming, "name", Newer) },
                new[]
                {
                    new ProfileLevelEntry(LevelId.NewId(), "name", Older),
                    new ProfileLevelEntry(LevelId.NewId(), ProfileMergePlanner.FolderOf(incoming), Older),
                });

            Assert.AreEqual(ProfileMergePlanner.FolderOf(incoming) + "-2", plan.Added[0].TargetFolder);
        }
    }
}

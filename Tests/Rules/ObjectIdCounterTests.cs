using System;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Rules;
using BH.SDK.Validations.Graph;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // THE COUNTER IS NOT THE COLLECTION, and binding it to LevelRules.MaxObjects said it was. Ids
    // are never reused, so the counter measures how many have ever been MINTED over a level's whole
    // authoring life - generator runs, pastes, prefab materializes, undone creations - while
    // MaxObjects bounds how many objects may EXIST at once. The two diverge with editing TIME rather
    // than with level size, so a long-lived level could fail validation while holding almost
    // nothing.
    //
    // And fail it DESTRUCTIVELY: RuleInRange is an Error whose Fix clamps to the nearer bound, so
    // the repair wrote the counter back down to 262 144 and the next object created took an id that
    // was already live - the never-reuse invariant broken by the rule meant to protect the file.
    // That second half is the regression these tests exist for; the first half only says the rule
    // no longer fires.
    //
    // Docs/Issues/LEVEL_MODEL_ANALYSIS.md section D is the record.

    /// <summary>
    /// The object-id counters: what bounds them, what happens at the end of the id space, and the
    /// warning that arrives long before it.
    /// </summary>
    public class ObjectIdCounterTests : BaseRuleTests
    {
        private static readonly LevelGraphAnalyzer Analyzer = new();

        #region The bound

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCounterPastObjectCapIsValid()
        {
            AssertValid(new LevelSettings { ObjectIdCounter = LevelRules.MaxObjects + 1 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCounterAtEndOfIdSpaceIsValid()
        {
            AssertValid(new LevelSettings { ObjectIdCounter = LevelRules.MaxObjectIds });
        }

        // The floor still matters, and it is why this stayed RuleInRange rather than dropping to
        // RuleMinValue: a counter below MinLevelValue hands out Null or a reserved negative -
        // Camera, LocalPlayer or PrefabRoot - on the very next create.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCounterBelowUserSpaceIsInvalid()
        {
            AssertHasIssue<BH.SDK.Rules.Attributes.RuleInRangeAttribute>(
                new LevelSettings { ObjectIdCounter = ObjectId.NullValue });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPrefabCounterPastObjectCapIsValid()
        {
            var prefab = new Prefab { PrefabId = PrefabId.NewGuid(), ObjectIdCounter = PrefabRules.MaxObjects + 1 };
            AssertValid(prefab);
        }

        // THE HALF THAT ACTUALLY BIT. The old bound was an Error, so the fixer clamped the counter
        // down into a range where the ids it hands out are already taken.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestMintingPastTheObjectCapDoesNotCollide()
        {
            var level = new Level();
            level.Settings.ObjectIdCounter = LevelRules.MaxObjects + 1;

            var existing = new RectObject { ObjectId = new ObjectId(LevelRules.MaxObjects) };
            level.Game.Objects.Add(existing.ObjectId, existing);

            Fix(level);

            var minted = level.Settings.GetNextObjectId();
            Assert.IsFalse(level.Game.Objects.ContainsKey(minted),
                $"Minted {minted} is already an object of this level");
            Assert.Greater(minted.value, existing.ObjectId.value);
        }

        #endregion

        #region Reservation and refusal

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRemainingCountsTheLastMintableId()
        {
            Assert.AreEqual(1L, LevelRules.RemainingObjectIds(LevelRules.MaxObjectIds));
            Assert.AreEqual(2L, LevelRules.RemainingObjectIds(LevelRules.MaxObjectIds - 1));
        }

        // The int spelling of this arithmetic overflows at exactly the end of the range it describes,
        // and an overflow here reports an exhausted counter as having room.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRemainingDoesNotOverflowAtTheFloor()
        {
            Assert.AreEqual(LevelRules.MaxObjectIds, LevelRules.RemainingObjectIds(ObjectId.MinLevelValue));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestBulkReservationStopsAtTheExactEdge()
        {
            var settings = new LevelSettings { ObjectIdCounter = LevelRules.MaxObjectIds - 9 };

            Assert.IsTrue(settings.CanMintObjectIds(10));
            Assert.IsFalse(settings.CanMintObjectIds(11));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestExhaustedCounterCanMintNothing()
        {
            var settings = new LevelSettings { ObjectIdCounter = LevelRules.MaxObjectIds };

            Assert.IsTrue(settings.CanMintObjectIds(1));
            settings.GetNextObjectId();

            Assert.IsFalse(settings.CanMintObjectIds(1));
        }

        // Never a wrap and never a negative: negatives are game-space objects and the three reserved
        // parents, so handing one out is a silent collision rather than an error. The polite refusal
        // lives at the caller (Core's ObjectIdBudget); this is the net under it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestMintingPastTheEndThrowsRatherThanWrapping()
        {
            var settings = new LevelSettings { ObjectIdCounter = LevelRules.MaxObjectIds };
            settings.GetNextObjectId();

            Assert.Throws<Exception>(() => settings.GetNextObjectId());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPrefabMintingPastTheEndThrowsRatherThanWrapping()
        {
            var prefab = new Prefab { PrefabId = PrefabId.NewGuid(), ObjectIdCounter = LevelRules.MaxObjectIds };
            prefab.GetNextObjectId();

            Assert.Throws<Exception>(() => prefab.GetNextObjectId());
        }

        #endregion

        #region The warning

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestOrdinaryCounterRaisesNoWarning()
        {
            var level = new Level();
            level.Settings.ObjectIdCounter = 50_000;

            AssertNoWarning(level);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestCounterPastThreeQuartersWarns()
        {
            var level = new Level();
            level.Settings.ObjectIdCounter = LevelRules.ObjectIdCounterWarning;

            AssertWarns(level, "Level.ObjectIdCounter");
        }

        // An emptied-out level whose counter sits near the ceiling is precisely the case the warning
        // is for, so it must not ride behind the "no objects, nothing to compare" early return that
        // the IdCounterBehind check needs.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestEmptyLevelStillWarns()
        {
            var level = new Level();
            level.Settings.ObjectIdCounter = LevelRules.MaxObjectIds - 1;

            Assert.IsEmpty(level.Game.Objects);
            AssertWarns(level, "Level.ObjectIdCounter");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPrefabScopeWarnsOnItsOwnCounter()
        {
            var level = new Level();
            var prefab = new Prefab
            {
                PrefabId = PrefabId.NewGuid(),
                ObjectIdCounter = LevelRules.ObjectIdCounterWarning,
            };
            level.Resources.Prefabs.Add(prefab.PrefabId, prefab);

            AssertWarns(level, $"Prefab[{prefab.PrefabId}].ObjectIdCounter");
        }

        // It is a WARNING, not an Error: the level is entirely correct, it is only going to stop
        // being able to grow one day. Reported as an Error it would be a repair request for
        // something with no repair short of renumbering the whole file.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestNearExhaustionIsAWarningNotAnError()
        {
            var level = new Level();
            level.Settings.ObjectIdCounter = LevelRules.ObjectIdCounterWarning;

            foreach (var issue in Analyzer.Analyze(level))
            {
                if (issue.Rule != GraphRule.IdCounterNearExhaustion) continue;
                Assert.AreEqual(RuleGroup.Warning, issue.Group);
                return;
            }

            Assert.Fail("No IdCounterNearExhaustion issue was reported at all");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWarningRuleIsNamed()
        {
            Assert.AreEqual("graph_id_counter_near_exhaustion", GraphRule.IdCounterNearExhaustion.GetKey());
        }

        #endregion

        private static void AssertWarns(Level level, string expectedPath)
        {
            foreach (var issue in Analyzer.Analyze(level))
            {
                if (issue.Rule != GraphRule.IdCounterNearExhaustion) continue;
                if (issue.Path != expectedPath) continue;
                return;
            }

            Assert.Fail($"Expected IdCounterNearExhaustion at {expectedPath}");
        }

        private static void AssertNoWarning(Level level)
        {
            foreach (var issue in Analyzer.Analyze(level))
            {
                Assert.AreNotEqual(GraphRule.IdCounterNearExhaustion, issue.Rule,
                    $"Unexpected exhaustion warning: {issue}");
            }
        }
    }
}

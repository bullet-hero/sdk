using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Validations;
using BH.SDK.Validations.Graph;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// ValidationFacade: one call that covers both halves of the standard, so a consumer never has
    /// to know the rule pass and the graph pass exist separately.
    /// </summary>
    public class ValidationFacadeTests
    {
        private static readonly ValidationFacade Facade = new();

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestValidLevel()
        {
            var report = Facade.Validate(MockData.CreateTestLevel());

            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.IsFalse(report.HasErrors);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestInvalidLevelIsRepaired()
        {
            var report = Facade.ValidateAndFix(MockData.CreateInvalidTestLevel());

            CollectionAssert.IsEmpty(report.RuleIssues,
                string.Join("\n", report.RuleIssues.Select(issue => issue.ToString())));
        }

        // The two halves are independent: a level can satisfy every declarative rule and still be
        // structurally broken. Catching that is the whole reason the facade runs both.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestGraphOnlyFailureIsReported()
        {
            var level = new Level();
            var first = new RectObject { ObjectId = new ObjectId(1), ParentObjectId = new ObjectId(2) };
            var second = new RectObject { ObjectId = new ObjectId(2), ParentObjectId = new ObjectId(1) };
            level.Game.Objects.Add(first.ObjectId, first);
            level.Game.Objects.Add(second.ObjectId, second);
            level.Settings.ObjectIdCounter = 3;

            var report = Facade.Validate(level);

            CollectionAssert.IsEmpty(report.RuleIssues);
            Assert.IsTrue(report.GraphIssues.Any(issue => issue.Rule == GraphRule.ParentCycle));
            Assert.IsTrue(report.HasErrors);
        }

        // Repairing never touches the graph, and the graph pass runs after repairs - so a report
        // describes the level as it now stands, not as it arrived.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestGraphIssuesSurviveFixing()
        {
            var level = new Level();
            var first = new RectObject { ObjectId = new ObjectId(1), ParentObjectId = new ObjectId(2) };
            var second = new RectObject { ObjectId = new ObjectId(2), ParentObjectId = new ObjectId(1) };
            level.Game.Objects.Add(first.ObjectId, first);
            level.Game.Objects.Add(second.ObjectId, second);
            level.Settings.ObjectIdCounter = 3;

            var report = Facade.ValidateAndFix(level);

            Assert.IsTrue(report.GraphIssues.Any(issue => issue.Rule == GraphRule.ParentCycle));
        }

        // A standalone aggregate has no level to resolve relationships against, so only the
        // declarative half applies - and it must not fail merely for being validated on its own.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestStandalonePrefab()
        {
            var prefab = new Prefab
            {
                PrefabId = PrefabId.NewGuid(),
                Root = { Span = new FrameSpan(FrameRules.MinFrame, 100) },
            };
            var inner = new RectObject { ObjectId = new ObjectId(1), Span = new FrameSpan(0, 51) };
            prefab.Objects.Add(inner.ObjectId, inner);
            prefab.ObjectIdCounter = 2;

            var report = Facade.Validate(prefab);

            Assert.IsTrue(report.IsValid, report.ToString());
            CollectionAssert.IsEmpty(report.GraphIssues);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestStandaloneMetaAndSettings()
        {
            Assert.IsTrue(Facade.Validate(MockData.CreateTestLevelMeta()).IsValid);
            Assert.IsTrue(Facade.Validate(MockData.CreateValidTestSettings()).IsValid);
        }
    }
}

using System.Diagnostics;
using BH.SDK.Validations;
using BH.SDK.Validations.Graph;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Validation is the one full-graph reflection pass a level pays on every load
    // (LevelLoaderService.LoadLevel), so its cost is not a library curiosity - it is the level's
    // load time. The point of this fixture is that the walk stays linear in node count and does not
    // regress into per-node reflection lookups again.

    /// <summary> Guards the analyzer's cost on a level the size of a real, heavy one. </summary>
    [TestFixture]
    public class RuleAnalyzerPerformanceTests
    {
        private const int ObjectCount = 4750;
        private const int PrefabCount = 33;
        private const int PrefabObjectCount = 10;

        // Wall-clock budget, deliberately loose: it is a regression tripwire, not a benchmark
        // assertion, and machines vary. A tenfold regression trips it; a 20% one does not.
        private const double BudgetMs = 800d;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void Analyze_LargeLevel_CompletesWithinBudget()
        {
            var level = MockData.CreateLargeTestLevel(ObjectCount, PrefabCount, PrefabObjectCount);

            var ctorWatch = Stopwatch.StartNew();
            var analyzer = new RuleAnalyzer();
            ctorWatch.Stop();

            var settings = new RuleAnalyzerSettings();
            var analyzeWatch = Stopwatch.StartNew();
            var issues = analyzer.Analyze(level, settings);
            analyzeWatch.Stop();

            var graphAnalyzer = new LevelGraphAnalyzer();
            var graphWatch = Stopwatch.StartNew();
            var graphIssues = graphAnalyzer.Analyze(level);
            graphWatch.Stop();

            TestContext.WriteLine($"objects={level.Game.Objects.Count} prefabs={level.Resources.Prefabs.Count}");
            TestContext.WriteLine($"RuleAnalyzer.ctor={ctorWatch.Elapsed.TotalMilliseconds:F1}ms");
            TestContext.WriteLine($"RuleAnalyzer.Analyze={analyzeWatch.Elapsed.TotalMilliseconds:F1}ms issues={issues.Count}");
            TestContext.WriteLine($"LevelGraphAnalyzer.Analyze={graphWatch.Elapsed.TotalMilliseconds:F1}ms issues={graphIssues.Count}");

            Assert.That(analyzeWatch.Elapsed.TotalMilliseconds, Is.LessThan(BudgetMs),
                $"RuleAnalyzer.Analyze took {analyzeWatch.Elapsed.TotalMilliseconds:F0}ms on " +
                $"{ObjectCount} objects (ctor {ctorWatch.Elapsed.TotalMilliseconds:F0}ms, " +
                $"graph {graphWatch.Elapsed.TotalMilliseconds:F0}ms, {issues.Count} issues)");
        }
    }
}

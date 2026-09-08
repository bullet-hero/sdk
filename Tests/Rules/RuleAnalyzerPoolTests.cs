using System;
using System.Collections;
using System.Reflection;
using BH.SDK.Rules.Attributes;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // RuleAnalyzer rents one child buffer per visited object from a prewarmed pool, and a buffer that
    // never comes back is invisible until the pool runs dry - from then on every Analyze dies on an
    // empty stack instead of reporting rules, and a shared analyzer carries that damage to every
    // consumer that touches it afterwards. That is how this regressed once: the misapplied-rule throw
    // is by design (AssertWrongType provokes it in most files of this folder) and it leaked a buffer
    // per occurrence, so the whole folder started failing once past the prewarmed count - including
    // across separate test runs, since an analyzer can outlive one run in a static field.
    //
    // These tests pin the rental contract rather than the pool's size: returned on success, returned
    // on throw, and the prewarmed count is an optimization rather than a depth limit.

    /// <summary>
    /// RuleAnalyzer's internal buffer pool: an instance stays reusable no matter how many analyses
    /// threw, and an object graph deeper than the prewarmed pool allocates instead of failing.
    /// </summary>
    public class RuleAnalyzerPoolTests
    {
        // Own analyzer, deliberately not BaseRuleTests' shared one: these assertions read the pool's
        // own depth, and any other fixture analyzing through the same instance would perturb it.
        private static readonly RuleAnalyzer Analyzer = new();

        private const string PoolFieldName = "_nextObjectsPool";

        /// <summary> A model with one bounded number, small enough that many analyses of it exercise the buffer pool rather than the rule. </summary>
        [RuleContainer]
        private class RangeModel
        {
            [RuleInRange(0, 10)]
            public int Value { get; set; }
        }

        /// <summary> A rule sitting on a type it cannot handle - the analyzer aborts the walk with
        /// ArgumentException, which is the throw path the pool has to survive. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleCollectionCount(3)]
            public int Value { get; set; }
        }

        /// <summary> Self-nesting container, so a test can build a graph of any depth. </summary>
        [RuleContainer]
        private class NodeModel
        {
            [RuleInRange(0, 10)]
            public int Value { get; set; }

            public NodeModel Child { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestBufferReturnedAfterSuccess()
        {
            var before = PoolCount();
            Analyzer.Analyze(new RangeModel { Value = 5 }, new RuleAnalyzerSettings());

            Assert.AreEqual(before, PoolCount(), "A completed analysis kept its rented buffer");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestBufferReturnedAfterThrow()
        {
            var before = PoolCount();
            Assert.Throws<ArgumentException>(
                () => Analyzer.Analyze(new WrongTypeModel(), new RuleAnalyzerSettings()));

            Assert.AreEqual(before, PoolCount(),
                "An aborted analysis leaked its rented buffer - the pool drains one throw at a time");
        }

        // The failure this reproduces is cumulative, so a single throw proves nothing: the analyzer
        // stayed usable for exactly as many misapplied rules as the pool was prewarmed for.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestSurvivesThrowsPastPoolSize()
        {
            var throws = PoolCount() * 2 + 8;
            for (var i = 0; i < throws; i++)
            {
                Assert.Throws<ArgumentException>(
                    () => Analyzer.Analyze(new WrongTypeModel(), new RuleAnalyzerSettings()));
            }

            var issues = Analyzer.Analyze(new RangeModel { Value = 100 }, new RuleAnalyzerSettings());

            Assert.AreEqual(1, issues.Count,
                $"Analyzer stopped reporting rules after {throws} aborted analyses");
            Assert.IsInstanceOf<RuleInRangeAttribute>(issues[0].Rule);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestGraphDeeperThanPool()
        {
            var depth = PoolCount() * 2 + 4;
            var issues = Analyzer.Analyze(BuildChain(depth), new RuleAnalyzerSettings());

            Assert.AreEqual(1, issues.Count, "The rule at the bottom of the chain went unreported");
            Assert.AreEqual(depth, issues[0].Trace.Count,
                "Trace does not span the whole chain - the walk stopped short of the deepest node");
        }

        // Growing past the prewarm is not a leak in the other direction either: what a deep graph
        // allocates is pooled on the way out, so the next deep analysis reuses it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPoolKeepsAllocatedBuffers()
        {
            var before = PoolCount();
            Analyzer.Analyze(BuildChain(before * 2 + 4), new RuleAnalyzerSettings());

            Assert.GreaterOrEqual(PoolCount(), before,
                "A graph deeper than the pool ended up shrinking it");
        }

        // The trace is the other piece of state an aborted walk leaves behind, and it fails quietly
        // rather than loudly: every later issue is reported under a path prefixed by the dead one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestTraceResetAfterThrow()
        {
            Assert.Throws<ArgumentException>(
                () => Analyzer.Analyze(new WrongTypeModel(), new RuleAnalyzerSettings()));

            var issues = Analyzer.Analyze(new RangeModel { Value = 100 }, new RuleAnalyzerSettings());

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(1, issues[0].Trace.Count,
                $"Trace carries the aborted analysis' path: {issues[0].GetPath()}");
            Assert.AreEqual(nameof(RangeModel.Value), issues[0].Trace[0].Property.Name);
        }

        /// <summary> Chain of depth nodes, every one valid except the deepest - so a single issue
        /// proves the walk reached the bottom, and its trace length proves how far it went. </summary>
        private static NodeModel BuildChain(int depth)
        {
            var root = new NodeModel { Value = 5 };

            var current = root;
            for (var i = 1; i < depth; i++)
            {
                current.Child = new NodeModel { Value = 5 };
                current = current.Child;
            }
            current.Value = 100;

            return root;
        }

        /// <summary> Buffers currently available for rent. Read reflectively on purpose: the pool is
        /// an implementation detail with no public surface, but "the buffer came back" is the exact
        /// invariant that broke, and asserting it directly beats waiting for a drained pool. </summary>
        private static int PoolCount()
        {
            var field = typeof(RuleAnalyzer).GetField(PoolFieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, $"RuleAnalyzer.{PoolFieldName} is gone - if the pool was renamed " +
                                    "or reshaped, point these tests at it rather than deleting them");

            return ((ICollection)field.GetValue(Analyzer)).Count;
        }
    }
}

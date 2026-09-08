using BH.SDK.Interop;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop
{
    // An interop report aggregates by code, and for a long time that meant it kept ONE path and a
    // count - so a finding that fired on eight thousand objects surfaced a single example, and an
    // author could reach exactly one of the objects it named. These pin the bounded list that
    // replaced it, and in particular the two ways it can be incomplete, which mean different things:
    // the COUNT can exceed the paths because the same place was hit repeatedly, and the PATHS can be
    // cut because more distinct places were reached than the list keeps.

    /// <summary> What an interop report remembers about where a finding was hit. </summary>
    public class InteropReportTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Add_TheSameCodeAtSeveralPlaces_KeepsEveryPlace()
        {
            var report = new InteropReport();
            report.Dropped("code", "message", "objects[1]");
            report.Dropped("code", "message", "objects[2]");
            report.Dropped("code", "message", "objects[3]");

            var issue = report.Issues[0];
            Assert.AreEqual(1, report.Issues.Count, "one code is still one line");
            Assert.AreEqual(3, issue.Count);
            CollectionAssert.AreEqual(new[] { "objects[1]", "objects[2]", "objects[3]" }, issue.Paths);
            Assert.IsFalse(issue.HasMorePaths);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Add_TheSamePlaceTwice_CountsTwiceAndListsItOnce()
        {
            var report = new InteropReport();
            report.Dropped("code", "message", "objects[1]");
            report.Dropped("code", "message", "objects[1]");

            var issue = report.Issues[0];
            Assert.AreEqual(2, issue.Count);
            Assert.AreEqual(1, issue.Paths.Count);
        }

        // The list is bounded because an unbounded one is the wall of text this aggregation exists
        // to avoid - but a reader has to be told it was cut, or a sample reads as the whole story.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Add_MoreDistinctPlacesThanItKeeps_SaysSo()
        {
            var report = new InteropReport();
            for (var i = 0; i < InteropIssue.MaxTrackedPaths + 10; i++)
                report.Dropped("code", "message", $"objects[{i}]");

            var issue = report.Issues[0];
            Assert.AreEqual(InteropIssue.MaxTrackedPaths + 10, issue.Count);
            Assert.AreEqual(InteropIssue.MaxTrackedPaths, issue.Paths.Count);
            Assert.IsTrue(issue.HasMorePaths);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Add_NoPath_CountsWithoutInventingOne()
        {
            var report = new InteropReport();
            report.Dropped("code", "message");
            report.Dropped("code", "message");

            var issue = report.Issues[0];
            Assert.AreEqual(2, issue.Count);
            Assert.IsEmpty(issue.Paths);
            Assert.IsEmpty(issue.FirstPath);
        }

        // A merge has to carry both halves, and they are different lengths - replaying one path per
        // count would either lose paths or invent them.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Absorb_MergesCountsAndPlaces()
        {
            var first = new InteropReport();
            first.Dropped("code", "message", "objects[1]");
            first.Dropped("code", "message", "objects[1]");

            var second = new InteropReport();
            second.Dropped("code", "message", "objects[2]");
            second.Dropped("other", "message", "events");

            first.Absorb(second);

            Assert.AreEqual(2, first.Issues.Count);

            var merged = first.Issues[0];
            Assert.AreEqual("code", merged.Code);
            Assert.AreEqual(3, merged.Count, "two occurrences plus one absorbed");
            CollectionAssert.AreEquivalent(new[] { "objects[1]", "objects[2]" }, merged.Paths);

            Assert.AreEqual("other", first.Issues[1].Code);
            Assert.AreEqual(1, first.Issues[1].Count);
            CollectionAssert.AreEqual(new[] { "events" }, first.Issues[1].Paths);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Absorb_IntoAnEmptyReport_KeepsTheSeverity()
        {
            var source = new InteropReport();
            source.Failed("code", "message", "level");

            var target = new InteropReport();
            target.Absorb(source);

            Assert.AreEqual(1, target.Issues.Count);
            Assert.AreEqual(1, target.Issues[0].Count);
            Assert.AreEqual(InteropSeverity.Failed, target.Worst);
            Assert.IsTrue(target.HasFailure);
        }
    }
}

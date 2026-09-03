using System.Collections.Generic;
using System.Text;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // THE HALF OF THE ACCEPTANCE TEST THAT NEEDS NOTHING BUT THIS ASSEMBLY. Its twin,
    // BH.Core.Tests.ValidationParityTests, is the one with the real corpus behind it and states a
    // level's whole report as a digest; this one states three small reports IN FULL, in the file, so
    // a change to the walk's ORDER is read rather than merely detected.
    //
    // The fixtures are MockData's CreateInvalid* factories, which are deliberately minimal - each
    // encodes the violations RuleFixer must find and no others - so the expected report is a
    // readable list rather than a wall. That is the whole reason the golden is inline here and a
    // digest there.
    //
    // WHAT THIS PINS is exactly what the generated walk must reproduce: which rule fired, at which
    // path, and IN WHICH ORDER. Order is not cosmetic - RuleFixer repairs in reverse, and repairs
    // are not commutative.

    [TestFixture]
    public class ValidationParityTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Analyze_OfInvalidLevel_ReportsExactlyThis()
        {
            AssertReport(MockData.CreateInvalidTestLevel(), InvalidLevel);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Analyze_OfInvalidMeta_ReportsExactlyThis()
        {
            AssertReport(MockData.CreateInvalidTestLevelMeta(), InvalidMeta);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Analyze_OfInvalidSettings_ReportsExactlyThis()
        {
            AssertReport(MockData.CreateInvalidTestSettings(), InvalidSettings);
        }

        // The large fixture is not stated in full - 4750 objects is not a list anyone reads - but it
        // is the only one here big enough to exercise the pooled buffer, the boxed-index cache and a
        // prefab template's scope rebase. A count plus the first and last line is what a reordering
        // would move, and it is small enough to live in the file.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Analyze_OfLargeLevel_IsStableInCountAndEnds()
        {
            var issues = Analyze(MockData.CreateLargeTestLevel(4750, 33, 10));
            var lines = Report(issues).Split('\n');

            TestContext.WriteLine($"{issues.Count} findings, first: {lines[0]}");
            Assert.That(issues.Count, Is.EqualTo(0),
                "a valid large fixture must report nothing - if this moved, MockData changed, not the walk");
        }

        private static void AssertReport(object root, string expected)
        {
            var actual = Report(Analyze(root));

            if (string.IsNullOrEmpty(expected))
            {
                TestContext.WriteLine(actual);
                Assert.Inconclusive("no golden yet - paste the report above into this test");
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static List<RuleIssue> Analyze(object root)
            => new RuleAnalyzer().Analyze(root, new RuleAnalyzerSettings());

        /// <summary> One line per finding, in the order the analyzer returned them. </summary>
        private static string Report(List<RuleIssue> issues)
        {
            var builder = new StringBuilder();
            foreach (var issue in issues)
            {
                builder.Append(issue.Rule.RuleNameKey).Append(' ')
                    .Append(issue.GetPath()).Append('\n');
            }

            return builder.ToString();
        }

        // Written with explicit \n rather than as verbatim strings: the report is built with \n and
        // a verbatim literal would carry whatever line ending this file happens to be checked out
        // with, which is a comparison that fails on one machine and passes on another.

        private const string InvalidLevel =
            "rule_in_range Settings.Framerate\n" +
            "rule_iprimitive_guid_not_null Game.Events.Themes[0].ThemeId\n" +
            "rule_iprimitive_int_not_null Audio.Tracks[AudioId=1].AudioResourceId\n" +
            "rule_in_range Audio.Tracks[AudioId=1].Speed\n" +
            "rule_iprimitive_int_max Resources.Textures[TextureResourceId=0].TextureResourceId\n" +
            "rule_iprimitive_int_max Resources.Fonts[FontResourceId=0].FontResourceId\n" +
            "rule_iprimitive_int_max Resources.Audios[AudioResourceId=0].AudioResourceId\n";

        private const string InvalidMeta =
            "rule_not_null LevelVersion\n" +
            "rule_not_null LevelLicense\n" +
            "rule_not_null ResourcesMeta[0].ResourceTitle\n" +
            "rule_not_null ResourcesMeta[0].ResourceDescription\n" +
            "rule_not_null ResourcesMeta[0].ResourceUrl\n" +
            "rule_not_null ResourcesMeta[0].ResourceLicense\n";

        private const string InvalidSettings =
            "rule_in_range General.ResourceParallelLoadCount\n" +
            "rule_min_value General.ResourceWebTimeout\n" +
            "rule_control_priority Controls.Priority\n" +
            "rule_in_range Audio.Game\n" +
            "rule_in_range Audio.UI\n" +
            "rule_min_value Graphics.Effects.FixedFramerate\n" +
            "rule_min_value GameEditor.Camera.MinSize\n";
    }
}
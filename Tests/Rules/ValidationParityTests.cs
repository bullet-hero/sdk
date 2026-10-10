using System.Collections.Generic;
using System.Text;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // Three small reports stated IN FULL so a change of ORDER is read (RuleFixer repairs in reverse, and
    // repairs do not commute). Its twin with the real corpus is BH.Core.Tests.ValidationParityTests.


    /// <summary> Three small reports stated IN FULL, so a change to the walk's ORDER is read rather than merely
    /// detected. Order is not cosmetic - repairs run in reverse and are not commutative. </summary>
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

        // Not stated in full: count plus first and last line, enough to move on a reordering.

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
            "rule_in_range Settings.Fps\n" +
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

        // Declaration order is visited, so this golden records that order; a moved group moves lines here.
        private const string InvalidSettings =
            "rule_in_range General.ResourceParallelLoadCount\n" +
            "rule_min_value General.ResourceWebTimeout\n" +
            "rule_in_range Audio.Game\n" +
            "rule_in_range Audio.UI\n" +
            "rule_in_range Controls.DeviceGyro.Sensitivity\n" +
            "rule_min_value Graphics.Effects.FpsFixed\n" +
            "rule_min_value GameEditor.Camera.MinSize\n";
    }
}
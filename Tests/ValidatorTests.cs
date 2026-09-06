using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    public class ValidatorTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestValidatorLevel()
        {
            var validator = new RuleAnalyzer();
            var level = MockData.CreateTestLevel();
            var issues = validator.Analyze(level, new RuleAnalyzerSettings());
            
            Assert.IsEmpty(issues);
        }
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestFixerLevel()
        {
            var fixer = new RuleFixer();
            var validator = new RuleAnalyzer();
            var level = MockData.CreateInvalidTestLevel();

            var settings = new RuleAnalyzerSettings(true, true);
            var issues = validator.Analyze(level, settings);
            Assert.IsNotEmpty(issues);

            // Repeated passes, because one repair can create the next: giving an unset resource an
            // id makes it disagree with the key it is filed under, and only a re-analysis sees that.
            issues = fixer.FixUntilStable(validator, level, settings, new RuleFixerSettings());
            Assert.IsEmpty(issues);
        }
        
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestValidatorLevelMeta()
        {
            var validator = new RuleAnalyzer();
            var level = MockData.CreateTestLevelMeta();
            var issues = validator.Analyze(level, new RuleAnalyzerSettings());
            
            Assert.IsEmpty(issues);
        }
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestFixerLevelMeta()
        {
            var fixer = new RuleFixer();
            var validator = new RuleAnalyzer();
            var level = MockData.CreateInvalidTestLevelMeta();
            
            var analyzerSettings = new RuleAnalyzerSettings();
            Assert.IsNotEmpty(validator.Analyze(level, analyzerSettings));

            var issues = fixer.FixUntilStable(validator, level, analyzerSettings, new RuleFixerSettings());
            Assert.IsEmpty(issues);
        }
        
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestValidatorSettings()
        {
            var validator = new RuleAnalyzer();
            var settings = MockData.CreateValidTestSettings();
            var issues = validator.Analyze(settings, new RuleAnalyzerSettings());
            
            Assert.IsEmpty(issues);
        }
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestFixerSettings()
        {
            var fixer = new RuleFixer();
            var validator = new RuleAnalyzer();
            var settings = MockData.CreateInvalidTestSettings();
            
            var analyzerSettings = new RuleAnalyzerSettings();
            Assert.IsNotEmpty(validator.Analyze(settings, analyzerSettings));

            var issues = fixer.FixUntilStable(validator, settings, analyzerSettings, new RuleFixerSettings());
            Assert.IsEmpty(issues);
        }
        
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestCopyLevel()
        {
            var validator = new RuleAnalyzer();
            var level = MockData.CreateTestLevel();
            var copyLevel = level.Copy();
            
            var issues = validator.Analyze(copyLevel, new RuleAnalyzerSettings());
            Assert.IsEmpty(issues);
        }
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestCopyEqualsLevel()
        {
            var level = MockData.CreateTestLevel();
            var copyLevel = level.Copy();
            Assert.IsTrue(level.Equals(copyLevel));
        }
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestCopyNotEqualsLevel()
        {
            var level = MockData.CreateTestLevel();
            var copyLevel = level.Copy();
            copyLevel.Game.Objects[new ObjectId(1)].AnchorsMin[0].Value = Alignment.LeftBottomValue;
            Assert.IsFalse(level.Equals(copyLevel));
        }
        
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCopySettings()
        {
            var validator = new RuleAnalyzer();
            var settings = MockData.CreateValidTestSettings();
            var copySettings = settings.Copy();
            
            var issues = validator.Analyze(copySettings, new RuleAnalyzerSettings());
            Assert.IsEmpty(issues);
        }
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCopyEqualsSettings()
        {
            var settings = MockData.CreateValidTestSettings();
            var copySettings = settings.Copy();
            Assert.IsTrue(settings.Equals(copySettings));
        }
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCopyNotEqualsSettings()
        {
            var settings = MockData.CreateValidTestSettings();
            var copySettings = settings.Copy();
            copySettings.Audio.Game = 0.123f;
            Assert.IsFalse(settings.Equals(copySettings));
        }
    }
}
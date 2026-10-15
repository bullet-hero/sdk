using System;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> <see cref="DifficultyRules.Read"/> - how a stored level difficulty reads on the scale. </summary>
    [TestFixture]
    public class DifficultyRulesTests
    {
        [TestCase(int.MinValue)]
        [TestCase(-1)]
        [TestCase(0)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ZeroOrLess_IsUndefined(int value)
            => Assert.AreEqual(DifficultyTier.Undefined, DifficultyRules.Read(value));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void One_IsAuto() => Assert.AreEqual(DifficultyTier.Auto, DifficultyRules.Read(1));

        [TestCase(2, DifficultyTier.Drizzle)]
        [TestCase(3, DifficultyTier.Rain)]
        [TestCase(4, DifficultyTier.Downpour)]
        [TestCase(5, DifficultyTier.Hail)]
        [TestCase(6, DifficultyTier.Barrage)]
        [TestCase(7, DifficultyTier.Onslaught)]
        [TestCase(8, DifficultyTier.Deluge)]
        [TestCase(9, DifficultyTier.Inferno)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TwoToNine_MapToTheirTiersInOrder(int value, DifficultyTier expected)
            => Assert.AreEqual(expected, DifficultyRules.Read(value));

        [TestCase(10)]
        [TestCase(11)]
        [TestCase(47)]
        [TestCase(int.MaxValue)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ScaleMaxOrAbove_IsSingularity(int value)
            => Assert.AreEqual(DifficultyTier.Singularity, DifficultyRules.Read(value));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void EveryRatedTier_EqualsTheNumberItIsReadFrom()
        {
            foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
            {
                if (tier == DifficultyTier.Undefined) continue;
                Assert.AreEqual(tier, DifficultyRules.Read((int)tier), tier.ToString());
            }
        }
    }
}

using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleReferenceExists: a texture/font/clip id no entry in the level answers to; only the int-backed id
    /// family, where the sign says who owns the resource.
    /// </summary>
    public class RuleReferenceExistsTests : BaseRuleTests
    {
        private static readonly RuleReferenceExistsAttribute Rule = new(ResourceReferenceKind.Texture);

        private static readonly RuleReferenceExistsAttribute NullableRule =
            new(ResourceReferenceKind.Texture, true);

        private static RuleContext LevelWithTexture(int id)
        {
            var level = new Level();
            var textureId = new TextureResourceId(id);
            level.Resources.Textures.Add(textureId,
                new TextureResource(textureId, new List<ResourceKey>()));

            return RuleContext.ForRoot(level);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUserDefinedResolves()
        {
            Assert.IsTrue(Rule.IsValid(new TextureResourceId(-1), LevelWithTexture(-1)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUserDefinedDangling()
        {
            Assert.IsFalse(Rule.IsValid(new TextureResourceId(-99), LevelWithTexture(-1)));
        }

        // Positive ids belong to the game's own registries, which are baked into the build and
        // invisible from the SDK - accepting them is the only correct answer available here.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestGameDefinedIsAccepted()
        {
            Assert.IsTrue(Rule.IsValid(new TextureResourceId(5), LevelWithTexture(-1)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullRejectedByDefault()
        {
            Assert.IsFalse(Rule.IsValid(new TextureResourceId(0), LevelWithTexture(-1)));
        }

        // AllowNull is for the properties where "points at nothing" is a real authored state rather
        // than a broken reference.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullAcceptedWhenAllowed()
        {
            Assert.IsTrue(NullableRule.IsValid(new TextureResourceId(0), LevelWithTexture(-1)));
        }

        // Validating a standalone template or a LevelMeta has no resources to resolve against, so
        // the rule stands down instead of reporting every reference as dangling.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNoLevelInContextStandsDown()
        {
            Assert.IsTrue(Rule.IsValid(new TextureResourceId(-99), RuleContext.ForRoot(new object())));
        }

        // Deliberately unfixable: pointing the reference elsewhere shows the wrong asset, clearing it
        // hides the object or changes what it means. Both are decisions for whoever edits the level.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestHasNoFix()
        {
            Assert.IsFalse(Rule.HasFix);
        }
    }
}

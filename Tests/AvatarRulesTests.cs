using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THIS FILE IS THE "NEVER CHANGE THESE" IN EXECUTABLE FORM, and it is the whole reason the numbers
    // became constants. They were serialized fields with a ScriptableObject overriding them, so the
    // project carried two answers for each and they had silently drifted apart - the asset played
    // dashTime 0.15 against a field initializer of 0.2, dashCooldown 0.25 against 0.5, damageTime 0.2
    // against 0.3, collisionScale 0.4 against 0.5, and knockoutSpeed 50 against 2.
    //
    // A test that merely reads AvatarRules back would pass no matter what anyone typed there. These
    // spell the values out a second time on purpose, so changing a constant fails HERE, with the old
    // number visible beside the new one, rather than in a level that stops being clearable.
    //
    // IF ONE OF THESE FAILS, THE QUESTION IS NOT HOW TO MAKE IT PASS. Levels are authored against the
    // dash reach below, the bot corpus compares runs across sessions against these speeds, and
    // Docs/Bots/README.md promises a player and a bot share them. Changing one is a decision about the
    // whole game, taken deliberately, with the corpus re-baselined afterwards - not a test to update.

    /// <summary> <see cref="AvatarRules"/> - the avatar's frozen balance. </summary>
    [TestFixture]
    public class AvatarRulesTests
    {
        private const float Tolerance = 1e-6f;

        #region The shipped values

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void MoveSpeed_IsTen() => Assert.AreEqual(10f, AvatarRules.MoveSpeed, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SizeSpeedInfluence_IsFull()
            => Assert.AreEqual(1f, AvatarRules.SizeSpeedInfluence, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DashSpeed_IsFifty() => Assert.AreEqual(50f, AvatarRules.DashSpeed, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DashTime_IsFifteenHundredths()
            => Assert.AreEqual(0.15f, AvatarRules.DashTime, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DashCooldown_IsAQuarterSecond()
            => Assert.AreEqual(0.25f, AvatarRules.DashCooldown, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DashInvulnerabilityTime_IsATwoTenths()
            => Assert.AreEqual(0.2f, AvatarRules.DashInvulnerabilityTime, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void KnockoutSpeed_IsFifty()
            => Assert.AreEqual(50f, AvatarRules.KnockoutSpeed, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DamageTime_IsTwoTenths()
            => Assert.AreEqual(0.2f, AvatarRules.DamageTime, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DamageTimeout_IsOneSecond()
            => Assert.AreEqual(1f, AvatarRules.DamageTimeout, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AvatarScale_IsHalf() => Assert.AreEqual(0.5f, AvatarRules.AvatarScale, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CollisionScale_IsFourTenths()
            => Assert.AreEqual(0.4f, AvatarRules.CollisionScale, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void RotateLerpSpeed_IsThirty()
            => Assert.AreEqual(30f, AvatarRules.RotateLerpSpeed, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ArrivedDistance_IsAHundredthOfAUnit()
            => Assert.AreEqual(0.01f, AvatarRules.ArrivedDistance, Tolerance);

        #endregion

        #region The relations levels are authored against

        // The product, not either factor. A level built around crossing a hazard in one dash stops
        // working the moment this number moves, however the two constants behind it were adjusted.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void OneDash_Reaches7Point5Units()
            => Assert.AreEqual(7.5f, AvatarRules.DashSpeed * AvatarRules.DashTime, 1e-5f);

        // A dash has to be worth taking: five times the walk over its own length, and it recovers
        // faster than it lasts twice over.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ADash_OutrunsAWalk()
            => Assert.Greater(AvatarRules.DashSpeed, AvatarRules.MoveSpeed);

        // The i-frames outlast the dash itself, which is what makes the window a LANDING GRACE rather
        // than a promise that only holds while the avatar is still travelling.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IFrames_OutlastTheDash()
            => Assert.Greater(AvatarRules.DashInvulnerabilityTime, AvatarRules.DashTime);

        // Control comes back long before the player can be hit again, so a shove into a second hazard
        // is survivable. The two windows are what a reader most easily conflates.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheDamageTimeout_OutlastsTheKnockback()
            => Assert.Greater(AvatarRules.DamageTimeout, AvatarRules.DamageTime);

        // The hitbox is smaller than what is drawn, deliberately: a bullet that visibly clips the
        // outline and does not kill reads as generous, the reverse reads as broken.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheHitbox_IsSmallerThanWhatIsDrawn()
            => Assert.Less(AvatarRules.CollisionScale, 0.5f);

        #endregion
    }
}

using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE "NEVER CHANGE THESE" IN EXECUTABLE FORM: every value is spelled out a second time so a change
    // fails HERE with the old number visible. A failure is a game-wide decision, not a test to update.
    // Source of truth: the avatar.* keys of DocsPublic/values.yaml; history: BH.SDK/Docs/TESTING.md.

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
        public void MoveSpeed_IsFifteen() => Assert.AreEqual(15f, AvatarRules.MoveSpeed, Tolerance);

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
        public void DashCooldown_IsThreeTenths()
            => Assert.AreEqual(0.3f, AvatarRules.DashCooldown, Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DashInvulnerabilityTime_IsThreeTenths()
            => Assert.AreEqual(0.3f, AvatarRules.DashInvulnerabilityTime, Tolerance);

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
        public void DamageTime_IsFifteenHundredths()
            => Assert.AreEqual(0.15f, AvatarRules.DamageTime, Tolerance);

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
        public void SpawnTime_IsThreeTenths()
            => Assert.AreEqual(0.3f, AvatarRules.SpawnTime, Tolerance);

        // Spans two assemblies (CheckpointService.RampSeconds is not visible here): the relation is
        // pinned with the number restated.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheSpawn_FitsInsideTheCheckpointRewind()
            => Assert.LessOrEqual(AvatarRules.SpawnTime, 0.5f);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CollisionScale_IsThreeTenths()
            => Assert.AreEqual(0.3f, AvatarRules.CollisionScale, Tolerance);

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

        // May be equal, never i-frames OUTLASTING the cooldown: then no frame is touchable (immunity).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheCooldown_IsNeverShorterThanTheIFrames()
            => Assert.GreaterOrEqual(AvatarRules.DashCooldown, AvatarRules.DashInvulnerabilityTime);

        // No duration to assert for the vulnerable window; the guarantee is the call order of three
        // types (see AvatarRules.DashCooldown). The floor kept here: i-frames must outlast the dash.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheIFrames_CoverTheWholeDashAndThenSome()
            => Assert.Greater(AvatarRules.DashInvulnerabilityTime - AvatarRules.DashTime, 0f);

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

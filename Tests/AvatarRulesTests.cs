using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THESE NUMBERS MOVED ONCE, DELIBERATELY, AND THE MOVE IS THE POINT OF THE FILE. The balance was
    // re-read against Just Shapes & Beats' own, measured out of its binary rather than guessed (its
    // field is 1280x720 units against this game's 10-unit camera height, so its numbers divide by 72
    // to land here). What that comparison actually changed is smaller than it first looked: the walk,
    // the dash, the reach and the shove all stayed where they were, and only the hitbox moved,
    // 0.4 -> 0.3. The dash i-frames went 0.2 -> 0.3 to sit exactly on the cooldown - the one
    // structural change, documented on DashCooldown rather than here. Every value is restated below,
    // so the NEXT move fails here first.
    //
    // THE PLAYER-FACING STATEMENT OF ALL OF IT IS Docs/PLAYER.md, and it is the SOURCE OF TRUTH
    // rather than a description of one: the values there are what the game is meant to play like, and
    // this file plus these tests are how the code is held to them. A number that moves moves THERE
    // first, and then here.
    //
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
        public void SpawnTime_IsThreeTenths()
            => Assert.AreEqual(0.3f, AvatarRules.SpawnTime, Tolerance);

        // THE CEILING, AND IT SPANS TWO ASSEMBLIES so nothing but a test can hold it: above
        // CheckpointService.RampSeconds the departure is still playing when the rewind seeks, the
        // arrival replaces it mid-way, and the avatar is never removed at all. The number is
        // restated rather than referenced - Services.Game is not visible from here, and it is the
        // relation being pinned rather than either constant.
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

        // THE ONE RELATION THAT IS A GAME RULE RATHER THAN A FEEL DECISION, AND IT IS NOW AN
        // INEQUALITY IN ONE DIRECTION ONLY. The two may be equal - that is the shipped balance, an
        // unbroken stream of dashes with exactly one touchable frame between them. What may never
        // happen is i-frames OUTLASTING the cooldown: the next dash would then be available while
        // the previous one still protects, no frame is ever observed touchable, and dash spam is
        // literal immunity with nothing left to stop it. This is the assertion that says so out loud
        // rather than leaving it to two constants twenty lines apart.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheCooldown_IsNeverShorterThanTheIFrames()
            => Assert.GreaterOrEqual(AvatarRules.DashCooldown, AvatarRules.DashInvulnerabilityTime);

        // THE WINDOW IS NO LONGER A DURATION, SO THERE IS NO DURATION TO ASSERT. It used to be
        // DashCooldown - DashInvulnerabilityTime >= 0.1 s, one frame at 10 fps, and a test here
        // guarded that floor because the collision pass is a per-frame POINT SAMPLE: a window
        // narrower than a frame falls between two samples and never happens. That is exactly what
        // 0.05 s did on a phone.
        //
        // With the two equal, the guarantee is structural instead, and it is NOT expressible in this
        // file - it lives in the order three types run in (BaseAvatarService.DriveAvatar launches
        // the dash, AvatarController.UpdateAvatar calls AvatarMovement.Observe, GameAvatarService
        // sizes the collider), which is why this assertion is about the ordering of the constants
        // and the rest is pinned where the order is. What survives here is the floor the whole
        // arrangement rests on: i-frames must outlast the dash, or the avatar is touchable while
        // still travelling at DashSpeed.
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

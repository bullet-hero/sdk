using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // THE STATE MACHINE AROUND THE STEP - the dash and damage windows, which is what a verifier may
    // claim anything about damage on. What these cases pin is not arithmetic for its own sake - it is
    // the agreement between the replayed avatar and the two services that decide a real hit:
    //
    //   GameCycleService.ProcessDamage - takes ONE life and then blocks damage for DamageTimeout, so
    //   the game counts at most one hit per second however long the avatar sits inside something.
    //   The replay counted every FRAME of an overlap and reported 25 to 31 hits on volcano where the
    //   run took 3 - and the repair pass, which is driven by that count, spent its whole budget on
    //   the difference.
    //
    //   AvatarController.Damage - shoves the avatar away from the collision point for DamageTime,
    //   answering no input. A replay that kept following the route through that was measuring a
    //   position the game will never be in.
    //
    // There is ONE implementation of both windows now. These cases used to cover a hand-restated copy
    // in the bake's own WarmReplayCheckpoint, which existed only because the type it needed was private
    // to a class three assemblies above it; they cover the real thing, so the game's own avatar is
    // pinned by them too.

    /// <summary> <see cref="AvatarMovement"/> - the dash and damage windows. </summary>
    [TestFixture]
    public class AvatarMovementStateTests
    {
        // Every window is an AvatarRules constant now and the state machine reads them itself, so
        // nothing is passed in at all - the timeout was the last one, and it stopped being an asset
        // when GameSettings was deleted.

        #region A fresh avatar

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void At_HasNeverBeenHitOrDashed()
        {
            var replay = AvatarMovement.At(new float2(3f, 4f));

            Assert.IsFalse(replay.InDamage(0f));
            Assert.IsFalse(replay.DamageBlocked(0f));
            Assert.IsFalse(replay.InDash(0f));
            Assert.IsFalse(replay.InInvulnerability(0f, AvatarRules.DashInvulnerabilityTime));
        }

        // The whole point of the sentinel: a level does not start with the avatar mid-knockback, and
        // it must not read that way at time zero either.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void At_StaysUnhitAcrossAWholeLevel()
        {
            var replay = AvatarMovement.At(float2.zero);

            for (var time = 0f; time < 600f; time += 13f)
            {
                Assert.IsFalse(replay.DamageBlocked(time));
                Assert.IsFalse(replay.InDamage(time));
            }
        }

        #endregion

        #region The damage timeout

        // THE CASE THE WHOLE FIX IS ABOUT: one overlap lasting a second and a half is ONE hit to the
        // game, because every collision after the first is ignored until the timeout falls.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Damage_BlocksEveryFurtherCollisionForTheWholeTimeout()
        {
            var replay = AvatarMovement.At(float2.zero).Damage(10f, new float2(1f, 0f));

            Assert.IsTrue(replay.DamageBlocked(10f));
            Assert.IsTrue(replay.DamageBlocked(10.5f));
            Assert.IsTrue(replay.DamageBlocked(11f));
            Assert.IsFalse(replay.DamageBlocked(11.01f));
        }

        // The knockback is SHORTER than the timeout, and the two are not the same window: the avatar
        // steers again long before it can be hit again.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Damage_KnockbackEndsWellBeforeTheTimeoutDoes()
        {
            var replay = AvatarMovement.At(float2.zero).Damage(10f, new float2(1f, 0f));

            Assert.IsTrue(replay.InDamage(10.1f));
            Assert.IsFalse(replay.InDamage(10.3f));
            Assert.IsTrue(replay.DamageBlocked(10.4f));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Damage_NormalisesTheShoveAndKeepsItsDirection()
        {
            var replay = AvatarMovement.At(float2.zero).Damage(1f, new float2(0f, 5f));

            Assert.AreEqual(0f, replay.KnockoutDirection.x, 1e-5f);
            Assert.AreEqual(1f, replay.KnockoutDirection.y, 1e-5f);
        }

        // A hit exactly on the collision point has no direction to aim away from. It must not produce
        // a NaN, and it must not invent one either: AvatarController used to roll a random angle here,
        // which broke determinism on a path the bot corpus replays. Standing still is the honest
        // answer - the avatar was on top of what hit it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Damage_WithNoDirection_IsZeroRatherThanNaN()
        {
            var replay = AvatarMovement.At(float2.zero).Damage(1f, float2.zero);

            Assert.IsFalse(float.IsNaN(replay.KnockoutDirection.x));
            Assert.IsFalse(float.IsNaN(replay.KnockoutDirection.y));
            Assert.AreEqual(0f, math.lengthsq(replay.KnockoutDirection), 1e-6f);
        }

        #endregion

        #region The two windows do not clobber each other

        // Advance carries EVERYTHING forward. A checkpoint is restored wholesale by a repair rewind,
        // so a field dropped here is a field the replay disagrees with itself about across a repair -
        // precisely the class of bug a verifier must not have.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Advance_KeepsBothTheDashAndTheDamage()
        {
            var replay = AvatarMovement.At(float2.zero)
                .StartDash(1f, new float2(1f, 0f))
                .Damage(1.05f, new float2(0f, -1f))
                .Advance(new float2(9f, 9f));

            Assert.AreEqual(9f, replay.Position.x, 1e-5f);
            Assert.IsTrue(replay.InDash(1.05f));
            Assert.IsTrue(replay.DashHadMove);
            Assert.IsTrue(replay.InDamage(1.1f));
            Assert.AreEqual(-1f, replay.KnockoutDirection.y, 1e-5f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void StartDash_AfterAHit_LeavesTheDamageWindowAlone()
        {
            var replay = AvatarMovement.At(float2.zero)
                .Damage(5f, new float2(1f, 0f))
                .StartDash(5.4f, new float2(0f, 1f));

            Assert.IsTrue(replay.DamageBlocked(5.5f));
            Assert.IsTrue(replay.InDash(5.5f));
        }

        #endregion
    }
}

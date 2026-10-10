using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // A paused avatar as a clock: Time stopped with a live DeltaTime is a pair that cannot happen, and the
    // knockback's own window then never closes. The fix is in the consumer's PlayerService.ProcessTime;
    // given a zero delta, no branch may move the avatar (Docs/AVATAR_MOVEMENT.md).

    /// <summary> <see cref="AvatarMovement"/> under a stopped clock - the shape of a paused run. </summary>
    [TestFixture]
    public class AvatarPausedClockTests
    {
        private static readonly float2 Start = new(3f, -2f);

        private static readonly AvatarStepSpeeds Speeds = AvatarStepSpeeds.Default(1f);

        // One case per way into GetTargetMove, all asserting the same thing - a paused clock leaves
        // the avatar exactly where it stood - because the branch that failed to is not guessable from
        // the outside and a later edit may move which one it is.
        [TestCase(0f)]
        [TestCase(0.1f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AZeroDelta_MovesNothingWhileWalking(float pausedAt)
        {
            var movement = AvatarMovement.At(Start);

            var stepped = movement.Step(false, float2.zero, new float2(1f, 0f), Speeds,
                pausedAt, 0f, out var result);

            Assert.AreEqual(Start.x, stepped.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, stepped.Position.y, 1e-6f);
            Assert.AreEqual(Start.x, result.Position.x, 1e-6f);
        }

        // A STEERED DASH, with the direction still held - which is the only way this branch carries
        // speed at all, since the dash scales DashSpeed by the input's own length.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AZeroDelta_MovesNothingMidDash()
        {
            var movement = AvatarMovement.At(Start).StartDash(0f, 1f);

            var pausedAt = AvatarRules.DashTime * 0.5f;
            Assert.IsTrue(movement.InDash(pausedAt), "the fixture must be inside the dash window");

            var stepped = movement.Step(false, float2.zero, new float2(1f, 0f), Speeds,
                pausedAt, 0f, out _);

            Assert.AreEqual(Start.x, stepped.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, stepped.Position.y, 1e-6f);
        }

        // Empty input (a pause) dashes at zero; both halves are asserted, this pairing failed in a shipped
        // build.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AnEmptyInput_MovesNothingMidDash_WithOrWithoutAClock()
        {
            var movement = AvatarMovement.At(Start).StartDash(0f, 1f);
            var midDash = AvatarRules.DashTime * 0.5f;

            var paused = movement.Step(false, float2.zero, float2.zero, Speeds, midDash, 0f, out _);
            var live = movement.Step(false, float2.zero, float2.zero, Speeds, midDash, 0.016f, out _);

            Assert.AreEqual(Start.x, paused.Position.x, 1e-6f);
            Assert.AreEqual(Start.x, live.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, live.Position.y, 1e-6f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AZeroDelta_MovesNothingMidKnockback()
        {
            var movement = AvatarMovement.At(Start).Damage(0f, new float2(0f, 1f));

            var pausedAt = AvatarRules.DamageTime * 0.5f;
            Assert.IsTrue(movement.InDamage(pausedAt), "the fixture must be inside the damage window");

            var stepped = movement.Step(false, float2.zero, float2.zero, Speeds,
                pausedAt, 0f, out _);

            Assert.AreEqual(Start.x, stepped.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, stepped.Position.y, 1e-6f);
        }

        // A dash follows the cursor rather than its launch direction (the lock is gone: no overshoot to
        // prevent).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ADash_FollowsTheTargetRatherThanItsLaunchDirection()
        {
            var launch = math.normalize(new float2(1f, 1f));
            var movement = AvatarMovement.At(Start).StartDash(0f, 1f);

            // The target sits back the way the avatar came - the pull that used to be ignored.
            var behind = Start - launch * 5f;
            var stepped = movement.Step(true, behind, launch, Speeds,
                AvatarRules.DashTime * 0.5f, 0.016f, out var result);

            Assert.Less(math.dot(math.normalize(stepped.Position - Start), launch), -0.99f,
                "a dash follows the cursor, so a cursor behind the avatar turns it around");
            Assert.Less(math.dot(math.normalize(result.TargetDirection), launch), -0.99f);
        }

        // THE OTHER HALF OF THE CONTRACT, so these cases cannot pass by the step being broken
        // outright: the same fixtures with a real delta DO move, which is exactly what a resumed run
        // has to go on doing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ALiveDelta_StillCarriesTheDashAndTheKnockback()
        {
            var dashing = AvatarMovement.At(Start).StartDash(0f, 1f)
                .Step(false, float2.zero, new float2(1f, 0f), Speeds,
                    AvatarRules.DashTime * 0.5f, 0.016f, out _);

            var knocked = AvatarMovement.At(Start).Damage(0f, new float2(0f, 1f))
                .Step(false, float2.zero, float2.zero, Speeds,
                    AvatarRules.DamageTime * 0.5f, 0.016f, out _);

            Assert.Greater(math.distance(dashing.Position, Start), 1e-4f,
                "a dash under a running clock has to keep covering ground");
            Assert.Greater(math.distance(knocked.Position, Start), 1e-4f,
                "a knockback under a running clock has to keep shoving");
        }
    }
}
using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // WHAT A PAUSED AVATAR IS, EXPRESSED AS A CLOCK. This type takes the time and the delta as two
    // separate arguments, which is what lets a caller hand it a pair that cannot happen: a Time that
    // has stopped and a DeltaTime that has not. The dash and the knockback are the two branches of
    // GetTargetMove that supply a direction of their OWN rather than taking one from the input, so
    // they keep stepping under that pair however empty the input is - and their windows, being tested
    // against the stopped Time, never close. That is a run that moves around a level whose effects
    // and both counters have visibly stopped.
    //
    // THE KNOCKBACK IS THE ONE THAT ALWAYS SLID, at KnockoutSpeed, which is five times the walk. A
    // dash slides only when it was launched from a standstill: DashHadMove makes a dash that WAS
    // steered scale its speed by the input still being held, so releasing the stick already took it
    // to zero and a paused input reads the same way. Both are covered by the same fix and both are
    // pinned below, because the difference between them is a detail of one branch rather than
    // anything a caller should have to know.
    //
    // The fix is one line in Services.Root's PlayerService.ProcessTime - a stopped inframe clock now
    // reports DeltaTime 0 rather than keeping the last live one - and it cannot be tested from here:
    // that service needs a LevelPlayer and a built LevelState. What IS testable is the half this type
    // owns and the half the fix depends on, which is these cases: given a zero delta, no branch moves
    // the avatar, whatever window it is inside.

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

            var stepped = movement.Step(false, float2.zero, new float2(1f, 0f), 0f, Speeds,
                pausedAt, 0f, out var result);

            Assert.AreEqual(Start.x, stepped.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, stepped.Position.y, 1e-6f);
            Assert.AreEqual(Start.x, result.Position.x, 1e-6f);
        }

        // A STEERED DASH, with the direction still held - which is the only way this branch carries
        // speed at all, since DashHadMove scales it by the input's own length.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AZeroDelta_MovesNothingMidDash()
        {
            var movement = AvatarMovement.At(Start).StartDash(0f, new float2(1f, 0f));

            var pausedAt = AvatarRules.DashTime * 0.5f;
            Assert.IsTrue(movement.InDash(pausedAt), "the fixture must be inside the dash window");

            var stepped = movement.Step(false, float2.zero, new float2(1f, 0f), 0f, Speeds,
                pausedAt, 0f, out _);

            Assert.AreEqual(Start.x, stepped.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, stepped.Position.y, 1e-6f);
        }

        // A DASH FROM A STANDSTILL, which is the dash that DID slide through a pause: with no
        // direction to scale by, the branch falls back to full DashSpeed along the idle angle and an
        // empty input takes nothing away from it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AZeroDelta_MovesNothingMidStandingDash()
        {
            var movement = AvatarMovement.At(Start).StartDash(0f, float2.zero);

            var pausedAt = AvatarRules.DashTime * 0.5f;
            Assert.IsTrue(movement.InDash(pausedAt), "the fixture must be inside the dash window");

            var stepped = movement.Step(false, float2.zero, float2.zero, 0f, Speeds,
                pausedAt, 0f, out _);

            Assert.AreEqual(Start.x, stepped.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, stepped.Position.y, 1e-6f);
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

            var stepped = movement.Step(false, float2.zero, float2.zero, 0f, Speeds,
                pausedAt, 0f, out _);

            Assert.AreEqual(Start.x, stepped.Position.x, 1e-6f);
            Assert.AreEqual(Start.y, stepped.Position.y, 1e-6f);
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
            var dashing = AvatarMovement.At(Start).StartDash(0f, new float2(1f, 0f))
                .Step(false, float2.zero, new float2(1f, 0f), 0f, Speeds,
                    AvatarRules.DashTime * 0.5f, 0.016f, out _);

            var knocked = AvatarMovement.At(Start).Damage(0f, new float2(0f, 1f))
                .Step(false, float2.zero, float2.zero, 0f, Speeds,
                    AvatarRules.DamageTime * 0.5f, 0.016f, out _);

            Assert.Greater(math.distance(dashing.Position, Start), 1e-4f,
                "a dash under a running clock has to keep covering ground");
            Assert.Greater(math.distance(knocked.Position, Start), 1e-4f,
                "a knockback under a running clock has to keep shoving");
        }
    }
}

using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // The arithmetic a baked route is certified against: a defect here is a bot walking into a hazard it
    // promised to clear.

    /// <summary> <see cref="AvatarMovement"/> - one frame of avatar movement. </summary>
    [TestFixture]
    public class AvatarMovementTests
    {
        private const float Tolerance = 1e-5f;

        // THE SHIPPED NUMBERS, and the fixture used to disagree with them: KnockoutSpeed sat at 2f here
        // while the asset the game actually read played 50f, so the knockback was never once exercised
        // at the speed it has. Reading them off AvatarRules is what stops that recurring.
        private const float MoveSpeed = AvatarRules.MoveSpeed;
        private const float DashSpeed = AvatarRules.DashSpeed;
        private const float KnockoutSpeed = AvatarRules.KnockoutSpeed;

        // Every window is measured from the level clock, so the states below are built AT this instant
        // and stepped at it. Far enough from TimePoint.Never that nothing is accidentally still open.
        private const float Now = 100f;

        private static AvatarStepSpeeds Speeds(float scale = 1f) => AvatarStepSpeeds.Default(scale);

        private static AvatarMovement Walking(float2 position = default)
            => AvatarMovement.At(position);

        private static AvatarMovement Dashing(float2 dashDirection, float2 position = default)
            => AvatarMovement.At(position).StartDash(Now, 1f);

        private static AvatarMovement Knocked(float2 knockoutDirection, bool inDash = false,
            float2 position = default)
        {
            var movement = AvatarMovement.At(position);
            if (inDash) movement = movement.StartDash(Now, 1f);
            return movement.Damage(Now, knockoutDirection);
        }

        // The old free-standing Step took the state as its first argument; keeping that shape here is
        // what makes every case below read exactly as it did before the mechanism became a value.
        private static AvatarStepResult Step(AvatarMovement movement, bool hasTarget, float2 target,
            float2 direction, in AvatarStepSpeeds speeds, float deltaTime, float idleAngle = 0f)
        {
            movement.Step(hasTarget, target, direction, speeds, Now, deltaTime,
                out var result);
            return result;
        }

        #region Speed scale

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetSpeedScale_NeutralInputs_IsOne()
            => Assert.AreEqual(1f, AvatarMovement.GetSpeedScale(1f, 1f, 1f), Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetSpeedScale_ZeroInfluence_IgnoresSize()
            => Assert.AreEqual(2f, AvatarMovement.GetSpeedScale(9f, 2f, 0f), Tolerance);

        #endregion

        #region Direction mode

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Step_NoInput_MovesNothingAndIsNotMoving()
        {
            var result = Step(Walking(new float2(3f, 4f)), false, float2.zero,
                float2.zero, Speeds(), 0.1f);

            Assert.IsFalse(result.Moving);
            Assert.AreEqual(3f, result.Position.x, Tolerance);
            Assert.AreEqual(4f, result.Position.y, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Step_Direction_TravelsSpeedTimesDelta()
        {
            var result = Step(Walking(), false, float2.zero, new float2(1f, 0f),
                Speeds(), 0.1f);

            Assert.IsTrue(result.Moving);
            Assert.AreEqual(MoveSpeed * 0.1f, result.Position.x, Tolerance);
        }

        // The length of the direction IS the fraction of full speed - it is what a bot's throttle and
        // an analog stick both ride on, and normalizing it here would silence both.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_HalfLengthDirection_TravelsHalfAsFar()
        {
            var full = Step(Walking(), false, float2.zero, new float2(1f, 0f),
                Speeds(), 0.1f);
            var half = Step(Walking(), false, float2.zero, new float2(0.5f, 0f),
                Speeds(), 0.1f);

            Assert.AreEqual(full.Position.x * 0.5f, half.Position.x, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Step_Scale_MultipliesTheTravel()
        {
            var result = Step(Walking(), false, float2.zero, new float2(1f, 0f),
                Speeds(0.5f), 0.1f);

            Assert.AreEqual(MoveSpeed * 0.5f * 0.1f, result.Position.x, Tolerance);
        }

        #endregion

        #region Target mode

        // Landing exactly ON the target is what makes the next frame's distance exactly zero. A step
        // of the remaining distance leaves a float-epsilon residue the same size as the "is there any
        // distance left" test, which made a held, motionless cursor flicker between moving and stopped.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_TargetWithinOneStep_SnapsExactlyOntoIt()
        {
            var target = new float2(0.25f, 0f);
            var result = Step(Walking(), true, target, float2.zero, Speeds(), 0.1f);

            Assert.AreEqual(target.x, result.Position.x);
            Assert.AreEqual(target.y, result.Position.y);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_TargetFurtherThanOneStep_ChasesAtWalkSpeed()
        {
            var result = Step(Walking(), true, new float2(100f, 0f), float2.zero,
                Speeds(), 0.1f);

            Assert.IsTrue(result.Moving);
            Assert.AreEqual(MoveSpeed * 0.1f, result.Position.x, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_StandingOnTarget_IsArrivedAndNotMoving()
        {
            var position = new float2(5f, 5f);
            var result = Step(Walking(position), true,
                position + new float2(AvatarRules.ArrivedDistance * 0.5f, 0f), float2.zero,
                Speeds(), 0.1f);

            Assert.IsTrue(result.Arrived);
            Assert.IsFalse(result.Moving);
            Assert.AreEqual(position.x, result.Position.x, Tolerance);
        }

        #endregion

        #region Micro movement

        // "Moving" is not "travelling": TargetSpeed follows the travel, not the setting, or a followed
        // route's move trail sits under the avatar. The position must not change.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_TargetCloserThanOneStep_ReportsTheSpeedActuallyTravelled()
        {
            const float dt = 0.1f;
            var target = new float2(0.25f, 0f);
            var result = Step(Walking(), true, target, float2.zero, Speeds(), dt);

            Assert.IsTrue(result.Moving);
            Assert.AreEqual(0.25f / dt, result.TargetSpeed, Tolerance);
            Assert.Less(result.TargetSpeed, MoveSpeed);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_TargetFurtherThanOneStep_StillReportsFullWalkSpeed()
        {
            var result = Step(Walking(), true, new float2(100f, 0f), float2.zero,
                Speeds(), 0.1f);

            Assert.AreEqual(MoveSpeed, result.TargetSpeed, Tolerance);
        }

        // The throttle is measured after the per-frame scaling, so a level that halves the player's
        // speed halves what full speed MEANS rather than making every step read as a crawl.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_ScaledSpeed_ThrottlesAgainstTheScaledSpeed()
        {
            const float dt = 0.1f;
            var result = Step(Walking(), true, new float2(100f, 0f), float2.zero,
                Speeds(0.5f), dt);

            Assert.AreEqual(MoveSpeed * 0.5f, result.TargetSpeed, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_ThrottledApproach_LandsOnTheTargetExactly()
        {
            var target = new float2(0.03f, 0.04f);
            var result = Step(Walking(), true, target, float2.zero, Speeds(), 0.1f);

            Assert.AreEqual(target.x, result.Position.x);
            Assert.AreEqual(target.y, result.Position.y);
        }

        // The knockback is the only unthrottled branch; a target inside the arrival radius means no speed.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_DashingOntoTheTarget_ReportsNoSpeed()
        {
            var result = Step(Dashing(new float2(1f, 0f)), true, new float2(0.01f, 0f),
                float2.zero, Speeds(), 0.1f);

            Assert.AreEqual(0f, result.TargetSpeed, Tolerance);
            Assert.IsFalse(result.Moving);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Step_KnockedBackTowardsANearTarget_KeepsFullKnockoutSpeed()
        {
            var result = Step(Knocked(new float2(0f, 1f)), true, new float2(0.01f, 0f),
                float2.zero, Speeds(), 0.1f);

            Assert.AreEqual(KnockoutSpeed, result.TargetSpeed, Tolerance);
        }

        #endregion

        #region Dash

        // A dash stops on its target: its window is sized to the distance first, and the clamp guarantees
        // it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Step_DashingPastANearTarget_StopsOnItInsteadOfOvershooting()
        {
            var target = new float2(0.25f, 0f);
            var result = Step(Dashing(new float2(1f, 0f)), true, target, float2.zero,
                Speeds(), 0.1f);

            Assert.AreEqual(target.x, result.Position.x, Tolerance);
        }

        // A dash follows the target: a dash that cannot overshoot has nothing to lock its direction against.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Step_DashingAwayFromTheTarget_TurnsTowardsIt()
        {
            var result = Step(Dashing(new float2(-1f, 0f)), true, new float2(10f, 0f),
                float2.zero, Speeds(), 0.1f);

            Assert.Greater(result.Position.x, 0f);
        }

        #endregion

        #region Damage outranks everything

        // "Uncontrolled" is an ORDERING, not a flag: a dash that still steered during a knockback
        // would let the player cancel their own knockback by pressing a button.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Step_InDamage_IgnoresTheInputAndFliesTheKnockout()
        {
            var result = Step(Knocked(new float2(0f, 1f)), false, float2.zero,
                new float2(1f, 0f), Speeds(), 0.1f);

            Assert.AreEqual(0f, result.Position.x, Tolerance);
            Assert.AreEqual(KnockoutSpeed * 0.1f, result.Position.y, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Step_InDamageAndInDash_StillFliesTheKnockout()
        {
            var result = Step(Knocked(new float2(0f, 1f), inDash: true), false,
                float2.zero, new float2(1f, 0f), Speeds(), 0.1f);

            Assert.AreEqual(KnockoutSpeed * 0.1f, result.Position.y, Tolerance);
        }

        #endregion
    }
}

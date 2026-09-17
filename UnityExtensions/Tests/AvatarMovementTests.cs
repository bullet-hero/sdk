using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // THE ARITHMETIC A BAKED ROUTE IS CERTIFIED AGAINST. AvatarController calls this for the avatar a
    // player drives and the warm bot's verifier calls it to replay a route it is about to promise is
    // damage-free - so a defect here is not a movement bug, it is a bot confidently walking into a
    // hazard it told itself it would clear.
    //
    // Every case below is a property the extraction had to preserve, not a property invented for it:
    // each one is a line of AvatarController that carries its own comment explaining a bug it fixed.

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

        // WHAT THIS PINS IS THE DIFFERENCE BETWEEN "MOVING" AND "TRAVELLING", and it was a real
        // defect rather than a nicety: a followed route hands the avatar a target a few hundredths of
        // a unit away every frame, and TargetSpeed answered with the full walking speed because it
        // was read off the setting rather than off the travel. The avatar's own move trail is emitted
        // at that velocity, so during those small steps the particles were launched as if it were
        // sprinting while it crawled - and they sat under it instead of trailing behind it.
        //
        // The POSITION must not move by any of this. The snap onto the target was already exact; what
        // changed is only what the step reports about itself.

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

        // THE KNOCKBACK IS THE ONLY UNTHROTTLED BRANCH LEFT. It covers a distance of its own rather
        // than closing on a point, so what is left in front of it means nothing to it. The dash used
        // to be the second such branch and is not any more: it is sized to its target before it
        // starts, so being clamped to the distance left is what it is FOR - see the dash region.
        //
        // A TARGET INSIDE THE ARRIVAL RADIUS IS A TARGET THE AVATAR IS STANDING ON, dash or no dash,
        // and standing on it means no direction and therefore no speed. That is the same answer the
        // walk gives, which is the point: the dash stopped being a special case.
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

        // A DASH STOPS ON ITS TARGET, AND THIS ASSERTION IS THE REVERSE OF THE ONE IT REPLACES. It
        // used to overshoot on purpose: a dash covered DashSpeed * DashTime whatever was in front of
        // it, because clamping a FULL dash to a near target would have made it cover nothing. What
        // changed is that a dash aimed at a near point is no longer a full dash - the caller sizes
        // the window to the distance first (AvatarMovement.ResolveDashFraction), so the clamp and the
        // window agree instead of fighting. The clamp is what makes an overshoot impossible even when
        // they cannot agree, which is a level moving its Speed track mid-dash.
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

        // A DASH FOLLOWS THE TARGET, and this assertion reversed with the one above for the same
        // reason. The launch direction used to be kept for the length of the dash because a re-aimed
        // dash oscillated around a point it had overshot - and a dash that cannot overshoot cannot
        // oscillate. Steering during a dash is what a direction player always had; this is the line
        // that gives it to a cursor player.
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

using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // THE FOUR RULES A DASH AIMED AT A POINT FOLLOWS, AND THE ONE NUMBER THEY ALL COME OUT OF. A dash
    // is one shape scaled by DashFraction: a point beyond the reach gets a full dash, a point inside
    // it gets a proportionally shorter one that ENDS on it however near it is, a point the avatar
    // already stands on (AvatarRules.ArrivedDistance) gets no dash at all, and the travel is steered
    // towards the point every frame rather than locked to where it was launched.
    //
    // WHY THIS FIXTURE EXISTS SEPARATELY FROM THE GATE AND THE STEP: those two pin what a dash DOES
    // once it has started, and this pins what a dash IS - the arithmetic that decides, before it
    // starts, how long it runs and therefore how long its two windows last. Scaling the travel without
    // scaling the windows would hand a cursor player a free dash every time they aimed close, which is
    // the failure this whole scheme has to avoid: a cursor and a direction player must be on the same
    // terms, and the terms are these.

    /// <summary> <see cref="AvatarMovement"/>'s dash length: what a target asks for, and what that
    /// costs. </summary>
    [TestFixture]
    public class AvatarDashReachTests
    {
        private const float Tolerance = 1e-5f;

        private static readonly AvatarStepSpeeds Speeds = AvatarStepSpeeds.Default(1f);

        private static float FullReach => AvatarMovement.GetDashReach(1f);

        #region What a target asks for

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheReach_IsTheSpeedTimesTheTime()
            => Assert.AreEqual(AvatarRules.DashSpeed * AvatarRules.DashTime, FullReach, Tolerance);

        // The reach is the LEVEL's, not the constant: a level that halves the player's speed halves
        // how far one dash goes, so the same cursor distance is twice the fraction of a dash there.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheReach_FollowsTheLevelsOwnScaling()
        {
            Assert.AreEqual(FullReach * 0.5f, AvatarMovement.GetDashReach(0.5f), Tolerance);
            Assert.AreEqual(1f,
                AvatarMovement.ResolveDashFraction(FullReach * 0.5f, AvatarMovement.GetDashReach(0.5f)),
                Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void APointBeyondTheReach_AsksForAFullDash()
            => Assert.AreEqual(1f, AvatarMovement.ResolveDashFraction(FullReach * 4f, FullReach),
                Tolerance);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void APointInsideTheReach_AsksForItsOwnFraction()
            => Assert.AreEqual(0.5f, AvatarMovement.ResolveDashFraction(FullReach * 0.5f, FullReach),
                Tolerance);

        // RULE 1, AND IT IS A REFUSAL RATHER THAN A TINY DASH - but only for a point the avatar is
        // already standing on, which Step would move it nowhere towards. Zero is the answer the caller
        // turns into "the press was not a dash": no window opens, no cooldown starts.
        [TestCase(0f)]
        [TestCase(AvatarRules.ArrivedDistance * 0.5f)]
        [TestCase(AvatarRules.ArrivedDistance)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void APointAlreadyArrivedAt_AsksForNoDashAtAll(float distance)
            => Assert.AreEqual(0f, AvatarMovement.ResolveDashFraction(distance, FullReach), Tolerance);

        // THERE IS NO FLOOR ABOVE ARRIVAL. A point a hair past it gets a dash a hair long, with every
        // consequence a dash has; a floor of one world unit stood here once and read as a dropped
        // press.
        [TestCase(0.02f)]
        [TestCase(0.1f)]
        [TestCase(0.5f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void APointJustPastArrival_AsksForItsOwnFraction(float distance)
            => Assert.AreEqual(distance / FullReach,
                AvatarMovement.ResolveDashFraction(distance, FullReach), Tolerance);

        // A level may zero every speed the avatar has; asking for a fraction of a reach of nothing has
        // no answer, and a division would hand back an infinity the clamp would happily accept.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AReachOfNothing_AsksForNoDash()
            => Assert.AreEqual(0f, AvatarMovement.ResolveDashFraction(5f, 0f), Tolerance);

        #endregion

        #region What it costs

        // THE WHOLE POINT, IN ONE ASSERTION. Travel, i-frames and cooldown are the same dash scaled by
        // the same number - so a short dash is not a cheap dash, and two half dashes cost the time one
        // full dash costs. Scaling the travel alone is the exploit this forecloses.
        [TestCase(1f)]
        [TestCase(0.5f)]
        [TestCase(0.2f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryWindow_ScalesWithTheDash(float fraction)
        {
            var movement = AvatarMovement.At(float2.zero).StartDash(0f, fraction);

            Assert.AreEqual(AvatarRules.DashTime * fraction, movement.DashDuration, Tolerance);

            // Just inside each window, and just outside it.
            Assert.IsTrue(movement.InDash(AvatarRules.DashTime * fraction * 0.99f));
            Assert.IsFalse(movement.InDash(AvatarRules.DashTime * fraction * 1.01f));

            var iFrames = AvatarRules.DashInvulnerabilityTime * fraction;
            Assert.IsTrue(movement.InInvulnerability(iFrames * 0.99f,
                AvatarRules.DashInvulnerabilityTime));
            Assert.IsFalse(movement.InInvulnerability(iFrames * 1.01f,
                AvatarRules.DashInvulnerabilityTime));
        }

        // THE ORDERING SURVIVES THE SCALING, which is what makes the shortest dash as safe to land
        // from as the longest: i-frames outlast the travel at every fraction, because both are the
        // same constant ratio times it.
        [TestCase(1f)]
        [TestCase(0.5f)]
        [TestCase(0.2f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheIFrames_OutlastTheTravelAtEveryLength(float fraction)
        {
            var movement = AvatarMovement.At(float2.zero).StartDash(0f, fraction);
            var justAfterTheDash = movement.DashDuration * 1.01f;

            Assert.IsFalse(movement.InDash(justAfterTheDash));
            Assert.IsTrue(movement.InInvulnerability(justAfterTheDash,
                AvatarRules.DashInvulnerabilityTime),
                "landing grace is what the i-frames outlasting the dash IS, at any dash length");
        }

        // A LEVEL THAT TURNED I-FRAMES OFF KEEPS THEM OFF AT EVERY LENGTH. Zero times a fraction is
        // still zero, and the guard in InInvulnerability is what says so rather than the arithmetic.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AWindowOfZero_StaysOffHoweverShortTheDash()
        {
            var movement = AvatarMovement.At(float2.zero).StartDash(0f, 0.01f);

            Assert.IsFalse(movement.InInvulnerability(0f, 0f));
            Assert.IsFalse(movement.InInvulnerability(0.001f, 0f));
        }

        // A FRACTION OF NOTHING OPENS NO WINDOW. Callers never pass one (ResolveDashFraction answers
        // 0 and the caller refuses the press), and a negative one is clamped rather than let run the
        // windows backwards.
        [TestCase(0f)]
        [TestCase(-3f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFractionOfZeroOrLess_IsClampedToZero(float asked)
            => Assert.AreEqual(0f, AvatarMovement.At(float2.zero).StartDash(0f, asked).DashFraction,
                Tolerance);

        // What the dash-spam guarantee rests on now that the value has no floor: a sliver of a dash
        // still waits for one observed touchable frame, so it can come back every other frame at most.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ASliverOfADash_StillWaitsForAnObservedFrame()
        {
            const float frame = 1f / 60f;
            var window = AvatarRules.DashInvulnerabilityTime;
            var movement = AvatarMovement.At(float2.zero).StartDash(0f, 0.001f);

            // The launch frame is observed while still inside its own i-frames.
            movement = movement.Observe(0f, window);
            Assert.IsFalse(movement.CanDash(frame), "the next frame is refused however short the dash");

            movement = movement.Observe(frame, window);
            Assert.IsTrue(movement.CanDash(frame * 2f), "one touchable frame later it is back");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFractionAboveOne_IsClampedDownToIt()
            => Assert.AreEqual(1f, AvatarMovement.At(float2.zero).StartDash(0f, 9f).DashFraction,
                Tolerance);

        #endregion

        #region Where it lands

        // RULE 2, MEASURED RATHER THAN ASSERTED ABOUT: stepped frame by frame to the end of its own
        // window, a dash aimed at a point inside the reach finishes ON that point. The travel and the
        // window were sized by the same arithmetic, so this is the two agreeing.
        [TestCase(1f)]
        [TestCase(0.75f)]
        [TestCase(0.5f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ADashAimedInsideTheReach_EndsOnThePoint(float fraction)
        {
            var target = new float2(FullReach * fraction, 0f);
            var movement = AvatarMovement.At(float2.zero)
                .StartDash(0f, AvatarMovement.ResolveDashFraction(math.length(target), FullReach));

            const float delta = 1f / 240f;
            var time = 0f;

            // One frame past the window, so the last partial frame of the dash is included.
            while (time <= movement.DashDuration + delta)
            {
                movement = movement.Step(true, target, float2.zero, Speeds, time, delta, out _);
                time += delta;
            }

            Assert.AreEqual(target.x, movement.Position.x, 1e-3f,
                "a dash sized to its target has to finish on it, not short of it or past it");
            Assert.AreEqual(0f, movement.Position.y, 1e-3f);
        }

        // AND IT MAY NOT OVERSHOOT EVEN WHEN THE ARITHMETIC IS MADE TO DISAGREE. The clamp in Step is
        // the guarantee rather than the mechanism: here the dash is launched at full length against a
        // target well inside it, which is exactly what a level moving its Speed track mid-dash would
        // produce.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AFullDashAtANearTarget_StopsOnItRatherThanPastIt()
        {
            var target = new float2(1f, 0f);
            var movement = AvatarMovement.At(float2.zero).StartDash(0f, 1f);

            const float delta = 1f / 120f;
            var time = 0f;

            while (time <= movement.DashDuration + delta)
            {
                movement = movement.Step(true, target, float2.zero, Speeds, time, delta, out _);
                time += delta;
            }

            Assert.AreEqual(target.x, movement.Position.x, 1e-3f);
        }

        // A DIRECTION DASH IS NOT CLAMPED BY ANYTHING, because there is no point to stop on: it covers
        // the reach and that is the number levels are authored against. This is the case the arrival
        // clamp must never reach.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ADirectionDash_CoversTheWholeReach()
        {
            var movement = AvatarMovement.At(float2.zero).StartDash(0f, 1f);

            const float delta = 1f / 240f;
            var time = 0f;

            // Stepped for exactly as long as the window is open, so the tolerance is one frame of
            // travel: a direction dash has no point to land on, so it stops wherever the last frame
            // inside the window left it rather than on a number.
            while (movement.InDash(time))
            {
                movement = movement.Step(false, float2.zero, new float2(1f, 0f), Speeds, time, delta,
                    out _);
                time += delta;
            }

            Assert.AreEqual(FullReach, movement.Position.x, AvatarRules.DashSpeed * delta,
                "a direction dash covers its whole reach - the arrival clamp must never reach it");
        }

        #endregion
    }
}

using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // Dash spam is never immunity: the sampled gate (Observe) leaves exactly one touchable FRAME per cycle,
    // and the sweep asserts that at frame rates no rig reaches. Measurements: Docs/AVATAR_MOVEMENT.md.

    /// <summary> <see cref="AvatarMovement.CanDash"/> and <see cref="AvatarMovement.Observe"/> -
    /// the vulnerability window between two dashes. </summary>
    [TestFixture]
    public class AvatarDashGateTests
    {
        private const float Window = AvatarRules.DashInvulnerabilityTime;
        private const float Cooldown = AvatarRules.DashCooldown;

        // THERE IS NO GAP IN SECONDS ANY MORE - the two windows are the same length - so the moment
        // a frame can be touchable at all is the moment the i-frames lapse, and this is the first
        // instant past them rather than a midpoint between two different numbers.

        /// <summary> The first instant the avatar is touchable again after a dash. </summary>
        private const float JustPastTheIFrames = Window + 0.001f;

        #region The gate

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AFreshAvatar_MayDashAtOnce()
            => Assert.IsTrue(AvatarMovement.At(float2.zero).CanDash(0f));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ADashInProgress_IsNotOverridden()
        {
            var replay = AvatarMovement.At(float2.zero).StartDash(0f, 1f);

            Assert.IsFalse(replay.CanDash(0f));
            Assert.IsFalse(replay.CanDash(Cooldown * 0.5f));
        }

        // The half this file exists for: the cooldown expiring is NOT on its own permission to dash.
        // Nothing has been sampled since the launch, so nothing has been exposed, so the dash waits -
        // which is precisely what happens on a device whose frame is longer than the window.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheCooldownAlone_DoesNotReleaseTheDash()
        {
            var replay = AvatarMovement.At(float2.zero).StartDash(0f, 1f);

            Assert.IsFalse(replay.CanDash(Cooldown));
            Assert.IsFalse(replay.CanDash(Cooldown * 10f));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ATouchableFrame_ReleasesTheDashAsSoonAsTheIFramesLapse()
        {
            var replay = AvatarMovement.At(float2.zero)
                .StartDash(0f, 1f)
                .Observe(JustPastTheIFrames, Window);

            Assert.IsTrue(replay.ExposedSinceDash);
            Assert.IsFalse(replay.CanDash(Cooldown - 0.01f), "the cooldown still gates it");
            Assert.IsTrue(replay.CanDash(JustPastTheIFrames),
                "with the two windows equal, the frame that counts as exposure also clears the "
                + "cooldown - the next dash is the frame after it");
        }

        // An observation taken while the i-frames are still up says nothing about being touchable,
        // and counting it would hand back exactly the exploit the flag exists to close. The second
        // call sits on the closing edge, which is inclusive - a window of 0.2 covers a delta of 0.2.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AnObservationInsideTheIFrames_ReleasesNothing()
        {
            var replay = AvatarMovement.At(float2.zero)
                .StartDash(0f, 1f)
                .Observe(Window * 0.5f, Window)
                .Observe(Window, Window);

            Assert.IsFalse(replay.ExposedSinceDash);
            Assert.IsFalse(replay.CanDash(Cooldown));
        }

        // A window of 0 is the global off switch for i-frames, which a level authored around solid
        // obstacles needs - and with them off every frame is a touchable one, so the gate has to
        // collapse back to the plain cooldown rather than blocking a dash that was never protected.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void WithIFramesOff_TheCooldownIsTheWholeGate()
        {
            var replay = AvatarMovement.At(float2.zero)
                .StartDash(0f, 1f)
                .Observe(0f, 0f);

            Assert.IsTrue(replay.ExposedSinceDash);
            Assert.IsTrue(replay.CanDash(Cooldown));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ANewDash_ClosesTheWindowAgain()
        {
            var replay = AvatarMovement.At(float2.zero)
                .StartDash(0f, 1f)
                .Observe(JustPastTheIFrames, Window)
                .StartDash(Cooldown, 1f);

            Assert.IsFalse(replay.ExposedSinceDash);
            Assert.IsFalse(replay.CanDash(Cooldown * 2f));
        }

        // The flag rides on the value, and every other transition has to carry it: the warm bot
        // repair pass rewinds by restoring one of these wholesale, so a transition that dropped it
        // would let the replay dash where the game will not.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheFlag_SurvivesEveryOtherTransition()
        {
            var replay = AvatarMovement.At(float2.zero)
                .StartDash(0f, 1f)
                .Observe(JustPastTheIFrames, Window)
                .Advance(new float2(3f, 4f))
                .Damage(JustPastTheIFrames, new float2(0f, 1f));

            Assert.IsTrue(replay.ExposedSinceDash);
            Assert.IsTrue(replay.CanDash(Cooldown));
        }

        #endregion

        #region The frame-rate sweep

        /// <summary> What a dash-spamming run at one frame rate came to. </summary>
        private readonly struct SpamRun
        {
            public readonly int Dashes;
            public readonly int ExposedSamples;
            public readonly int CyclesWithoutExposure;
            public readonly int Frames;
            public readonly float DashPeriod;

            public SpamRun(int dashes, int exposedSamples, int cyclesWithoutExposure, int frames,
                float dashPeriod)
            {
                Dashes = dashes;
                ExposedSamples = exposedSamples;
                CyclesWithoutExposure = cyclesWithoutExposure;
                Frames = frames;
                DashPeriod = dashPeriod;
            }
        }

        // One frame in the order the game runs it: launch, step and Observe, then the collider off the same
        // window. The period is measured between the first and last dash, not as run length over a count.
        private static SpamRun Spam(float fps, int frames)
        {
            var h = 1f / fps;
            var replay = AvatarMovement.At(float2.zero);

            var dashes = 0;
            var exposed = 0;
            var cyclesWithoutExposure = 0;
            var thisCycle = 0;

            var time = 0f;
            var firstDash = 0f;
            var lastDash = 0f;

            for (var frame = 0; frame < frames; frame++)
            {
                time += h;

                if (replay.CanDash(time))
                {
                    if (dashes > 0 && thisCycle == 0) cyclesWithoutExposure++;
                    else if (dashes == 0) firstDash = time;

                    thisCycle = 0;
                    lastDash = time;
                    replay = replay.StartDash(time, 1f);
                    dashes++;
                }

                var touchable = !replay.InInvulnerability(time, Window);
                replay = replay.Observe(time, Window);

                if (!touchable) continue;

                exposed++;
                thisCycle++;
            }

            var period = dashes > 1 ? (lastDash - firstDash) / (dashes - 1) : 0f;
            return new SpamRun(dashes, exposed, cyclesWithoutExposure, frames, period);
        }

        // The game rule: between any two dashes at least one frame could have been hit, at any frame rate.
        [TestCase(240f)]
        [TestCase(144f)]
        [TestCase(120f)]
        [TestCase(90f)]
        [TestCase(60f)]
        [TestCase(50f)]
        [TestCase(45f)]
        [TestCase(30f)]
        [TestCase(24f)]
        [TestCase(20f)]
        [TestCase(15f)]
        [TestCase(12f)]
        [TestCase(10f)]
        [TestCase(7f)]
        [TestCase(5f)]
        [TestCase(3f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DashSpam_IsNeverImmunity(float fps)
        {
            var run = Spam(fps, (int)math.ceil(fps * 20f));

            Assert.Greater(run.Dashes, 1,
                "the spam has to actually dash for the case to mean anything");
            Assert.Greater(run.ExposedSamples, 0, "no frame was ever touchable");
            Assert.AreEqual(0, run.CyclesWithoutExposure,
                $"at {fps} fps, {run.CyclesWithoutExposure} of {run.Dashes} dash cycles were never " +
                "sampled while touchable - dash spam is immunity there");
        }

        // The other half: above about 7 fps the gate costs nothing and the cooldown alone paces the dash.
        [TestCase(240f)]
        [TestCase(120f)]
        [TestCase(60f)]
        [TestCase(30f)]
        [TestCase(20f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AtOrdinaryFrameRates_TheCooldownIsStillWhatPacesIt(float fps)
        {
            var run = Spam(fps, (int)math.ceil(fps * 20f));

            Assert.GreaterOrEqual(run.DashPeriod, Cooldown - 1e-4f);
            // Two frames: one to be observed touchable, one for the dash. Paid in frames, so slow devices
            // lose.
            Assert.LessOrEqual(run.DashPeriod, Cooldown + 2f / fps + 1e-4f);
        }

        // Exposure is a share of every cycle (cooldown minus i-frames over cooldown); sampled, so one frame
        // low.
        [TestCase(240f)]
        [TestCase(120f)]
        [TestCase(60f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TheWindow_IsAShareOfEveryCycleRatherThanAScrapedFrame(float fps)
        {
            var run = Spam(fps, (int)math.ceil(fps * 20f));

            var share = run.ExposedSamples / (float)run.Frames;
            var expected = (Cooldown - Window) / Cooldown;

            Assert.AreEqual(expected, share, 2f / (fps * Cooldown) + 0.01f);

            // The shape: ONE frame per dash; a tolerance of one covers a run ending mid-cycle.
            Assert.AreEqual(run.Dashes, run.ExposedSamples, 1,
                "a spamming run is touchable exactly once per dash");
        }

        #endregion
    }
}
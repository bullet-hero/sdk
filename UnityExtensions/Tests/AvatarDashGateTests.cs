using BH.SDK.Avatars;
using BH.SDK.Rules;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // WHAT KEEPS DASH SPAM FROM BEING IMMUNITY, and it is two gates rather than one because a
    // duration on its own could not do it. The balance says a dash grants i-frames for
    // DashInvulnerabilityTime and returns after DashCooldown, so the difference between them is the
    // only window in which a player who never stops dashing can be hit. That window exists in
    // seconds no matter what - but the collision pass is a per-frame POINT SAMPLE, and an
    // invulnerable frame does not merely discard its result, it never runs the narrowphase at all
    // (GameAvatarService zeroes the radius). So on a device slow enough that one frame is longer
    // than the window, every sample lands inside a dash and the exposure never happens.
    //
    // MEASURED, BEFORE THE FIX, at the old 0.25 cooldown - a window of 0.05 s: 3 exposed samples per
    // dash at 60 fps, 1 at 20 fps, and at 15 fps 43% of dash cycles had NONE, which on a phone under
    // a heavy level is not a corner case. The cooldown is 0.35 now, and Observe makes the guarantee
    // absolute rather than merely likely.
    //
    // THE SWEEP IS THE POINT OF THIS FILE. The single-window cases pin the mechanism; the sweep is
    // what actually asserts the game rule, at frame rates no test rig can otherwise reach.

    /// <summary> <see cref="AvatarMovement.CanDash"/> and <see cref="AvatarMovement.Observe"/> -
    /// the vulnerability window between two dashes. </summary>
    [TestFixture]
    public class AvatarDashGateTests
    {
        private const float Window = AvatarRules.DashInvulnerabilityTime;
        private const float Cooldown = AvatarRules.DashCooldown;

        /// <summary> Comfortably past the i-frames and comfortably short of the cooldown. </summary>
        private const float InsideTheGap = (Window + Cooldown) * 0.5f;

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
            var replay = AvatarMovement.At(float2.zero).StartDash(0f, new float2(1f, 0f));

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
            var replay = AvatarMovement.At(float2.zero).StartDash(0f, new float2(1f, 0f));

            Assert.IsFalse(replay.CanDash(Cooldown));
            Assert.IsFalse(replay.CanDash(Cooldown * 10f));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ATouchableFrame_ReleasesTheDashWhenTheCooldownDoes()
        {
            var replay = AvatarMovement.At(float2.zero)
                .StartDash(0f, new float2(1f, 0f))
                .Observe(InsideTheGap, Window);

            Assert.IsTrue(replay.ExposedSinceDash);
            Assert.IsFalse(replay.CanDash(Cooldown - 0.01f), "the cooldown still gates it");
            Assert.IsTrue(replay.CanDash(Cooldown));
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
                .StartDash(0f, new float2(1f, 0f))
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
                .StartDash(0f, new float2(1f, 0f))
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
                .StartDash(0f, new float2(1f, 0f))
                .Observe(InsideTheGap, Window)
                .StartDash(Cooldown, new float2(0f, 1f));

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
                .StartDash(0f, new float2(1f, 0f))
                .Observe(InsideTheGap, Window)
                .Advance(new float2(3f, 4f))
                .Damage(InsideTheGap, new float2(0f, 1f));

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

        // ONE FRAME OF A DASH-SPAMMING RUN, IN THE ORDER THE GAME RUNS IT: BaseAvatarService.
        // DriveAvatar launches the dash first, then AvatarController.UpdateAvatar steps and observes,
        // and only then does the avatar service size the collider off the same window at the same
        // instant. So a launch frame is invulnerable when it is observed and cannot count as its own
        // exposure - which is why the order here is the assertion rather than an implementation
        // detail of the harness.
        //
        // THE PERIOD IS MEASURED BETWEEN THE FIRST AND LAST DASH, not as a run length over a count:
        // the first dash lands on frame one and the last one somewhere short of the end, so dividing
        // the whole run by the tally reports a period up to two cooldowns short of the real one.
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
                    replay = replay.StartDash(time, new float2(1f, 0f));
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

        // THE GAME RULE, ASSERTED AT FRAME RATES NOTHING ELSE CAN REACH. Constant dashing is meant to
        // buy speed at the price of control, never safety, so between any two dashes there must be at
        // least one frame on which the avatar could actually have been hit - on a gaming monitor, on
        // a phone holding 30, and on a phone that has stopped holding anything.
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

        // AND THE OTHER HALF: the gate may not cost anything at a frame rate that never needed it.
        // Above about 7 fps the timed window is wider than a frame, so a touchable sample always
        // lands inside it and the dash comes back on the cooldown alone - one dash per DashCooldown,
        // to within the one frame a launch waits for the next tick.
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
            Assert.LessOrEqual(run.DashPeriod, Cooldown + 1f / fps + 1e-4f);
        }

        // The exposure is a SHARE of every cycle rather than one frame scraped in at the end, and
        // that share is what the balance promises: the cooldown minus the i-frames, over the whole
        // cooldown - three sevenths at the shipped numbers. It is measured on samples, so it reads
        // one frame per cycle low (the sample landing exactly on the closing edge of the i-frames is
        // still covered by them), which is what the frame-shaped half of the tolerance allows for.
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
        }

        #endregion
    }
}
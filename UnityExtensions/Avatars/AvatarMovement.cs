using System.Runtime.CompilerServices;
using BH.SDK.Rules;
using Unity.Mathematics;

namespace BH.SDK.Avatars
{
    // WHERE THE AVATAR IS AND WHAT IS CURRENTLY MOVING IT - the whole mechanism, in one value.
    //
    // IT USED TO BE THREE COPIES. The consumer's AvatarController held the dash and damage timers, the
    // two directions and the launch flag as loose fields, plus a TimePoint private to itself; a
    // per-frame AvatarStepState re-assembled the same six values to hand to a free-standing Step; and
    // the warm bot's route verifier, which could reach neither, restated the state machine a third
    // time in its own struct - windows, sentinel and all - because the type it needed was private to a
    // class three assemblies above it. Three copies of "is the dash still going" is how a verifier
    // starts certifying routes the game will not fly. This is the one.
    //
    // IT IS A VALUE, NOT AN OBJECT, and that is a requirement rather than a preference: the bake's
    // repair pass REWINDS, keeping one of these per slot boundary and restoring it wholesale when a
    // window is re-planned. A mutable controller would have to be unwound field by field, and the first
    // field anyone forgot would make the replay disagree with itself across a repair - precisely the
    // class of bug a verifier must not have. The game simply reassigns its own copy each frame.
    //
    // IT TOUCHES NO UNITY RUNTIME. No Time, no Transform, no Camera, no UnityEngine.Random - the clock
    // arrives as a float and every direction arrives resolved. That is what lets the game (on the level
    // clock, bent by playback speed and the checkpoint ramp) and the bake (on the frame being replayed)
    // share one implementation, and what keeps a run reproducible.
    //
    // IT DOES NOT KEEP THE AVATAR ON SCREEN. Clamping to the camera bounds stays with the consumer
    // (BaseAvatarService.ClampToCameraView), because it reads the level's own camera rect for the frame
    // - pulling it in here would make the mechanism depend on level state it otherwise never sees.

    /// <summary> The avatar's position and the state driving it: dash, knockback, and one frame's step. </summary>
    public readonly struct AvatarMovement
    {
        /// <summary> Where the avatar stands. </summary>
        public readonly float2 Position;

        /// <summary> The direction the current dash was launched with; unit, or zero. </summary>
        public readonly float2 DashDirection;

        /// <summary> Which way the last hit shoved the avatar; unit, or zero. </summary>
        public readonly float2 KnockoutDirection;

        /// <summary> Whether that dash was launched with a direction rather than from a standstill. </summary>
        public readonly bool DashHadMove;

        // A WINDOW THAT EXISTS IN SECONDS IS WORTH NOTHING IF NO FRAME SAMPLES IT, and this flag is
        // the whole of the fix. DashCooldown outlasts DashInvulnerabilityTime by 0.15 s precisely so
        // that a player spending every dash is still exposed between them - but the collision pass is
        // a per-frame POINT SAMPLE (the consumer zeroes the avatar radius while the i-frames are up,
        // so an invulnerable frame skips the narrowphase entirely rather than discarding its result).
        // On a device slow enough that one frame is longer than the gap, every sample lands inside a
        // dash i-frame window and the exposure never happens at all. At the shipped numbers that was
        // true below ~7 fps; before the cooldown moved to 0.35 it was true below TWENTY, which is
        // inside what a phone does under a heavy level, and dash spam there was genuine immunity.
        //
        // So the gap is a COUNTED SAMPLE rather than an elapsed duration: a dash is refused until the
        // avatar has actually been observed while touchable at least once since the last one. Below
        // the frame rate where the timed gap fits between two frames the dash simply comes back
        // slower, which costs the player rather than paying them - the only direction this may fail
        // in. Above it the flag is already true long before the cooldown expires and nothing changes.
        //
        // EVERY CONSUMER MUST CALL Observe ONCE PER SIMULATED FRAME, and the failure mode if one
        // forgets is fail-CLOSED: that avatar dashes once and never again. AvatarController does it
        // inside UpdateAvatar (which covers the game, the editor preview and the menu arena at once),
        // and the warm bot route verifier does it in StepReplay, in the same order relative to the
        // dash decision - a verifier whose dash is swallowed where the game is not certifies routes
        // the game will not fly.

        /// <summary> Whether a touchable frame has been sampled since the last dash launched. </summary>
        public readonly bool ExposedSinceDash;

        private readonly TimePoint _dashStarted;
        private readonly TimePoint _damagedAt;

        private AvatarMovement(float2 position, TimePoint dashStarted, float2 dashDirection,
            bool dashHadMove, TimePoint damagedAt, float2 knockoutDirection, bool exposedSinceDash)
        {
            Position = position;
            _dashStarted = dashStarted;
            DashDirection = dashDirection;
            DashHadMove = dashHadMove;
            _damagedAt = damagedAt;
            KnockoutDirection = knockoutDirection;
            ExposedSinceDash = exposedSinceDash;
        }

        // Exposed, not because a frame has been sampled but because no dash has been taken: the very
        // first dash of a run may never wait on a window that has nothing to open it.

        /// <summary> An avatar standing at a point, having done nothing yet. </summary>
        public static AvatarMovement At(float2 position)
            => new(position, TimePoint.Invalid, float2.zero, false, TimePoint.Invalid, float2.zero,
                true);

        #region State

        /// <summary> Inside the dash's movement window. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InDash(float time) => _dashStarted.IsActiveAt(time, AvatarRules.DashTime);

        /// <summary> Inside the knockback: the avatar is flying and answers no input. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InDamage(float time) => _damagedAt.IsActiveAt(time, AvatarRules.DamageTime);

        // TWO GATES, AND THE SECOND IS NOT REDUNDANT WITH THE FIRST. The cooldown is the timed half
        // and answers the balance; ExposedSinceDash is the sampled half and answers the frame rate.
        // See its own note above for why a duration alone could not.

        /// <summary> Whether a dash asked for now would be taken rather than swallowed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanDash(float time)
            => ExposedSinceDash && !_dashStarted.InCooldown(time, AvatarRules.DashCooldown);

        // The window is a PARAMETER while every other one is read from AvatarRules, and the reason is
        // that 0 is a real value here: it switches i-frames off entirely, which a level authored around
        // solid obstacles needs, and the bake is handed that choice per run. Everywhere else the game's
        // own constant is the only answer.

        /// <summary> Untouchable right now. A window of 0 means i-frames are off, so it must not read
        /// as invulnerable on the launch frame. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InInvulnerability(float time, float window)
            => window > 0f && _dashStarted.IsActiveAt(time, window);

        // The window this asks about is LONGER than InDamage's and means something else: InDamage is
        // how long the avatar is not steering, this is how long it cannot be hit. Confusing the two is
        // the easy mistake. It is what makes a replayed hit count mean the same thing as a played one.

        /// <summary> Whether another collision right now would be ignored, exactly as the game's own
        /// damage debounce ignores one. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DamageBlocked(float time) => _damagedAt.IsActiveAt(time, AvatarRules.DamageTimeout);

        // STAGE 3 OF TAKING DAMAGE IS AN EDGE AND THIS IS THE ONLY WAY TO SEE HOW LONG AGO IT WAS.
        // InDamage covers the knockback, during which every input is ignored - so a consumer that wants
        // to act on "I have just been hit" cannot use it: by the time it can act, the flag is false.
        // TimePoint.Never sits far in the past, so this reads as an enormous number before the first
        // hit of a run and needs no separate case.

        /// <summary> Seconds since the last hit landed, counting the knockback window. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SinceDamage(float time) => _damagedAt.GetDelta(time);

        /// <summary> Seconds since the last dash was launched. The consumer's dash trail outlives the
        /// dash itself, so it needs the elapsed time rather than <see cref="InDash"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SinceDash(float time) => _dashStarted.GetDelta(time);

        #endregion

        #region Transitions

        /// <summary> The same avatar, moved. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AvatarMovement Advance(float2 position)
            => new(position, _dashStarted, DashDirection, DashHadMove, _damagedAt, KnockoutDirection,
                ExposedSinceDash);

        // ONE CALL PER SIMULATED FRAME, FROM EVERY CONSUMER - see the ExposedSinceDash note above. It
        // asks the SAME question the consumer is about to answer when it sizes the avatar collider,
        // at the same instant, so "this frame could have been hit" and "this frame counted as
        // exposure" can never disagree.
        //
        // The window is a parameter for the reason InInvulnerability takes one: 0 means i-frames are
        // off, and with them off every frame is exposure, so the gate collapses back to the plain
        // cooldown rather than blocking a dash a level deliberately made unprotected.
        //
        // It is deliberately blind to whether the avatar is COLLIDABLE at all. A level that hides the
        // player has zeroed the radius itself, and gating the dash on that would let authored content
        // disarm the dash for as long as it stayed hidden. What is counted is the i-frames lapsing,
        // which is the only thing the dash itself controls.

        /// <summary> Records that this frame was sampled: if the avatar was touchable, the next dash
        /// is released. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AvatarMovement Observe(float time, float window)
            => ExposedSinceDash || InInvulnerability(time, window)
                ? this
                : new AvatarMovement(Position, _dashStarted, DashDirection, DashHadMove, _damagedAt,
                    KnockoutDirection, true);

        // The direction is captured ONCE here rather than re-aimed per frame, and Step drives the whole
        // dash along it. See Step's own note: a re-aimed dash towards a target reverses the moment it
        // overshoots and covers nothing.

        /// <summary> The same avatar, having just launched a dash. Callers gate on
        /// <see cref="CanDash"/> - this does not swallow anything itself. </summary>
        public AvatarMovement StartDash(float time, float2 direction)
        {
            var length = math.length(direction);
            var hasMove = length > math.EPSILON;

            return new AvatarMovement(Position, new TimePoint(time),
                hasMove ? direction / length : float2.zero, hasMove, _damagedAt, KnockoutDirection,
                false);
        }

        // A HIT IS AN EVENT WITH A DURATION, NOT A FRAME. The direction is captured once, for two
        // reasons: a shove re-aimed at its source every frame becomes a chase, and the source is
        // ordinary level content that may be gone a frame later, leaving nothing to aim away from.
        //
        // THE DIRECTION ARRIVES RESOLVED, AND A ZERO ONE IS LEGAL. It used to be drawn here with
        // UnityEngine.Random when the avatar stood exactly on the collision point - a Unity-runtime call
        // this type may not make, and a determinism break besides: the project resolves randomness by
        // ADDRESS rather than by drawing it (root CLAUDE.md, "Randomness is addressed, not drawn"), and
        // the bot corpus compares runs across sessions. A caller with no direction passes zero, and the
        // knockback then simply moves nothing - the avatar was on top of what hit it, so there is no
        // "away" to shove it towards, and inventing one was never better than not moving.

        /// <summary> The same avatar, having just been hit and shoved along
        /// <paramref name="direction"/>. </summary>
        public AvatarMovement Damage(float time, float2 direction)
        {
            var length = math.length(direction);

            return new AvatarMovement(Position, _dashStarted, DashDirection, DashHadMove,
                new TimePoint(time), length > math.EPSILON ? direction / length : float2.zero,
                ExposedSinceDash);
        }

        #endregion

        #region Step

        /// <summary> What every speed the avatar has is multiplied by: the level's own Speed track,
        /// times as much of the player's size as <paramref name="sizeSpeedInfluence"/> lets through. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSpeedScale(float size, float speed, float sizeSpeedInfluence)
            => math.lerp(1f, size, math.saturate(sizeSpeedInfluence)) * speed;

        /// <summary> The game's own scaling for a frame, from the level's Player tracks. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSpeedScale(float size, float speed)
            => GetSpeedScale(size, speed, AvatarRules.SizeSpeedInfluence);

        // THE THREE BRANCHES ARE ORDERED, AND THE ORDER IS THE MEANING OF "UNCONTROLLED". A hit takes
        // movement away, so it outranks every other branch, the dash included - a dash that still
        // steered during a knockback would let the player cancel their own knockback with a button.

        /// <summary> Which way the avatar is driven this frame, and how fast, before scaling. </summary>
        public void GetTargetMove(float time, float2 direction, bool moving, float length,
            float idleAngle, in AvatarStepSpeeds speeds,
            out float2 targetDirection, out float targetSpeed)
        {
            if (InDamage(time))
            {
                targetDirection = KnockoutDirection;
                targetSpeed = speeds.KnockoutSpeed;
                return;
            }

            if (!InDash(time))
            {
                targetDirection = direction;
                targetSpeed = speeds.MoveSpeed;
                return;
            }

            if (moving)
            {
                targetDirection = direction;
                targetSpeed = speeds.DashSpeed;
                return;
            }

            targetDirection = Math2D.GetVector(idleAngle);
            targetSpeed = DashHadMove ? speeds.DashSpeed * length : speeds.DashSpeed;
        }

        // TWO PROPERTIES HERE EACH FIX A REAL BUG AND LOOK LIKE NOTHING:
        //
        // A DASH KEEPS THE DIRECTION IT WAS LAUNCHED WITH rather than re-aiming at the target every
        // frame. A target is a point the avatar is already closing on, so a re-aimed dash reverses the
        // moment it overshoots and spends the rest of DashTime oscillating around it, covering nothing
        // - which made a cursor dash useless against the one burst its direction-mode twin gives.
        //
        // ARRIVING IS A SNAP ONTO THE TARGET, not a step of exactly the remaining distance. The two
        // look identical and are not: `position += dir * dist` where `dir = toTarget / dist` leaves a
        // rounding residue about one float epsilon wide, which is the same order as the "is there any
        // distance left" test - so a held, motionless target alternated between "moving" and "stopped"
        // every frame and the avatar sat there flickering its squish, its eyes and its move trail.
        //
        // The step clamp is deliberately SKIPPED during a dash: a dash covers DashSpeed * DashTime by
        // design, and clamping it to the distance left would make a dash with the target nearby do
        // nothing at all, destroying the fixed dash distance the whole balance is built on.

        /// <summary> Advances the avatar by one frame against this frame's control, returning both the
        /// moved state and what the consumer's animation half needs. </summary>
        public AvatarMovement Step(bool hasTarget, float2 target, float2 direction, float idleAngle,
            in AvatarStepSpeeds speeds, float time, float deltaTime, out AvatarStepResult result)
        {
            var inDash = InDash(time);
            var inDamage = InDamage(time);

            var length = math.length(direction);
            var distanceToTarget = 0f;
            var arrived = false;

            if (hasTarget && inDash && DashHadMove)
            {
                direction = DashDirection;
                length = 1f;
            }
            else if (hasTarget)
            {
                var toTarget = target - Position;
                distanceToTarget = math.length(toTarget);

                // Inside the arrival radius the avatar IS on its target: no direction, which also
                // means Moving stays false - and that is the visible half of the fix above, since the
                // twitch was never the position moving but everything driven off "am I moving".
                arrived = distanceToTarget <= AvatarRules.ArrivedDistance;

                direction = !arrived && distanceToTarget > math.EPSILON
                    ? toTarget / distanceToTarget
                    : float2.zero;
                length = math.length(direction);
            }

            var moving = length > 0f;

            GetTargetMove(time, direction, moving, length, idleAngle, speeds,
                out var targetDirection, out var targetSpeed);

            // Both scalings land on targetSpeed rather than on the three settings they come from, so
            // walking, dashing and the knockback a hit gives are scaled by exactly the same number - a
            // dash that kept its own speed while the walk was halved would cover a distance the whole
            // DashSpeed/DashTime balance was never tuned for.
            targetSpeed *= speeds.Scale;

            var step = targetSpeed * deltaTime;

            // IT APPROACHES AT THE SPEED THE DISTANCE NEEDS, NOT AT FULL SPEED WITH THE OVERSHOOT
            // CLAMPED OFF AFTERWARDS, and the POSITION is identical either way - the snap below
            // already lands exactly on the target. What changes is what everything else is told.
            //
            // TargetSpeed used to read full walking speed for a step of a hundredth of a unit,
            // because "how fast am I driven" was answered by the setting rather than by the travel.
            // The avatar's own move trail is emitted at that velocity, so during the small steps a
            // followed route is made of, the particles were launched as if the avatar were sprinting
            // while the avatar barely moved - and they sat on top of it instead of trailing behind
            // it. AvatarController reads this back to decide whether the avatar is TRAVELLING at all.
            //
            // The knockback and the dash are excluded on purpose: both cover a distance the balance
            // is built on rather than closing on a point, and throttling either to what is left in
            // front of it is exactly the bug the dash's own comment above describes.
            if (hasTarget && !inDash && !inDamage && moving && step > distanceToTarget)
            {
                targetSpeed = deltaTime > 0f ? distanceToTarget / deltaTime : 0f;
                step = distanceToTarget;
            }

            // An avatar that has already arrived falls through to the ordinary line, where its own
            // direction is zero and the step adds nothing - while a knockback, which sets a direction
            // of its own, still moves. Following the last hundredth of a unit would be chasing noise.
            var position = hasTarget && !inDash && !arrived && distanceToTarget <= step
                ? target
                : Position + targetDirection * step;

            result = new AvatarStepResult(position, targetDirection, targetSpeed, moving, arrived);
            return Advance(position);
        }

        #endregion
    }
}

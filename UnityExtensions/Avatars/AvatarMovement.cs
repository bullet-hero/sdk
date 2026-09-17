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

        /// <summary> Which way the last hit shoved the avatar; unit, or zero. </summary>
        public readonly float2 KnockoutDirection;

        // A DASH IS ONE SHAPE SCALED BY ONE NUMBER, AND THAT NUMBER IS THIS. A dash aimed at a point
        // nearer than its full reach does not overshoot and brake - it is simply a SHORTER DASH, and
        // shorter in every respect at once: the travel, the i-frames and the cooldown are all the
        // shipped constant times this fraction. That is what keeps a cursor player and a direction
        // player on the same terms. Two half-dashes cover the distance of one full dash and cost the
        // same total time, so neither input is faster down a straight line; what the short dash buys
        // is the ability to stop exactly on a point, and what it costs is one extra touchable frame
        // per dash, because every launch has one (see DashCooldown's note in AvatarRules).
        //
        // IT IS NEVER BELOW AvatarRules.MinDashFraction AND NEVER ABOVE 1. The floor is one world
        // unit of reach, and what it stops is a cursor resting a hair from the avatar launching a
        // dash every single frame: at the floor a dash still runs 0.02 s and its windows 0.04 s, and
        // it still waits for one observed touchable frame, so the real limiter at the bottom of the
        // range is the frame rate rather than the clock. The ceiling is the dash itself: a point
        // beyond the reach gets an ordinary full dash and the avatar stops short of it.
        //
        // A DASH THAT WAS NEVER TAKEN LEAVES THIS AT 1, so every window reads exactly the constant it
        // is named after until the first dash of the run, and the "no dash yet" state needs no case
        // of its own anywhere.

        /// <summary> How much of a full dash the current one is, in
        /// [<see cref="AvatarRules.MinDashFraction"/>, 1]. Scales its travel, its i-frames and its
        /// cooldown together. </summary>
        public readonly float DashFraction;

        // A WINDOW THAT EXISTS IN SECONDS IS WORTH NOTHING IF NO FRAME SAMPLES IT, and this flag is
        // the whole of the fix. The dash's i-frames and its cooldown are now the SAME duration
        // (AvatarRules.DashInvulnerabilityTime equals DashCooldown, and DashFraction scales both), so
        // there is no gap in seconds at all - the only window in which a player spending every dash
        // can be hit is the one frame between a dash's i-frames lapsing and the next dash launching.
        //
        // That frame exists because of the ORDER the consumer runs in, not because of the clock: the
        // dash is launched before the step, Observe is called after it, and the collider is sized
        // afterwards off the same window at the same instant. So the frame the i-frames lapse is
        // refused a dash (this flag is still false), counted as exposure here, and sampled REAL by the
        // narrowphase - and the dash goes on the next frame. It holds at 10 fps and at 300 alike.
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

        private AvatarMovement(float2 position, TimePoint dashStarted, float dashFraction,
            TimePoint damagedAt, float2 knockoutDirection, bool exposedSinceDash)
        {
            Position = position;
            _dashStarted = dashStarted;
            DashFraction = dashFraction;
            _damagedAt = damagedAt;
            KnockoutDirection = knockoutDirection;
            ExposedSinceDash = exposedSinceDash;
        }

        // Exposed, not because a frame has been sampled but because no dash has been taken: the very
        // first dash of a run may never wait on a window that has nothing to open it.

        /// <summary> An avatar standing at a point, having done nothing yet. </summary>
        public static AvatarMovement At(float2 position)
            => new(position, TimePoint.Invalid, 1f, TimePoint.Invalid, float2.zero, true);

        #region The dash a target asks for

        // THE THREE RULES A CURSOR DASH FOLLOWS, AND THEY ARE ONE LINE OF ARITHMETIC. A point further
        // than the reach is an ordinary dash (the fraction saturates at 1); a point nearer is a dash
        // that ENDS ON IT, because the travel time is exactly the distance over the dash speed; a
        // point nearer than the floor is no dash at all. The caller asks this, gets a fraction, and
        // passes it to StartDash - there is no fourth case and no special branch inside the step.
        //
        // IT LIVES HERE RATHER THAN IN THE CONSUMER because the warm bot's route verifier dashes too,
        // and a verifier answering "how long is this dash" differently from the game is the exact
        // class of drift this whole type exists to prevent.

        /// <summary> How far a full dash reaches this frame, under the level's own scaling. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDashReach(float scale)
            => AvatarRules.DashSpeed * AvatarRules.DashTime * scale;

        /// <summary> What fraction of a full dash a target this far away asks for, or 0 when it is too
        /// close to be worth a dash at all. </summary>
        public static float ResolveDashFraction(float distance, float reach)
        {
            if (reach <= 0f) return 0f;

            var fraction = distance / reach;
            return fraction < AvatarRules.MinDashFraction ? 0f : math.min(fraction, 1f);
        }

        #endregion

        #region State

        // EVERY WINDOW IS THE CONSTANT TIMES DashFraction, and they are read through these rather than
        // computed by callers for one reason: a consumer that sized a collider off the bare constant
        // while the step ended the dash early would make the avatar untouchable after it had visibly
        // stopped.

        /// <summary> How long the current dash moves the avatar, in seconds. </summary>
        public float DashDuration => AvatarRules.DashTime * DashFraction;

        /// <summary> Inside the dash's movement window. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InDash(float time) => _dashStarted.IsActiveAt(time, DashDuration);

        /// <summary> Inside the knockback: the avatar is flying and answers no input. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InDamage(float time) => _damagedAt.IsActiveAt(time, AvatarRules.DamageTime);

        // TWO GATES, AND THE SECOND IS NOT REDUNDANT WITH THE FIRST. The cooldown is the timed half
        // and answers the balance; ExposedSinceDash is the sampled half and answers the frame rate.
        // See its own note above for why a duration alone could not.

        /// <summary> Whether a dash asked for now would be taken rather than swallowed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanDash(float time)
            => ExposedSinceDash
               && !_dashStarted.InCooldown(time, AvatarRules.DashCooldown * DashFraction);

        // The window is a PARAMETER while every other one is read from AvatarRules, and the reason is
        // that 0 is a real value here: it switches i-frames off entirely, which a level authored around
        // solid obstacles needs, and the bake is handed that choice per run. Everywhere else the game's
        // own constant is the only answer. DashFraction scales whatever is passed, so a level that
        // turned i-frames off keeps them off at every dash length.

        /// <summary> Untouchable right now. A window of 0 means i-frames are off, so it must not read
        /// as invulnerable on the launch frame. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InInvulnerability(float time, float window)
            => window > 0f && _dashStarted.IsActiveAt(time, window * DashFraction);

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
            => new(position, _dashStarted, DashFraction, _damagedAt, KnockoutDirection,
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
                : new AvatarMovement(Position, _dashStarted, DashFraction, _damagedAt,
                    KnockoutDirection, true);

        // A DASH WITH NO DIRECTION IS NOT TAKEN AT ALL, and that is a rule of the game rather than a
        // guard: the caller establishes a direction and a fraction before it gets here, and both
        // inputs answer it the same way. A direction player with nothing held is not asking to go
        // anywhere; a cursor player standing on their own cursor is not either. There used to be a
        // third answer - the direction was HASHED from the run's seed and the dash's ordinal, so a
        // standstill dash flew somewhere arbitrary but replayable - and it existed only because the
        // alternative on offer was a twitch: the avatar thrown out along its idle angle at dash speed
        // and hauled straight back to the cursor at dash speed, a dash fighting the thing that asked
        // for it. Refusing the dash costs the player nothing they can see (no cooldown starts, no
        // window opens, the press is simply not a dash) and removes a random number from a path the
        // corpus replays.

        /// <summary> The same avatar, having just launched a dash of <paramref name="fraction"/> of a
        /// full one. Callers gate on <see cref="CanDash"/> and resolve the fraction with
        /// <see cref="ResolveDashFraction"/> - this swallows nothing itself. </summary>
        public AvatarMovement StartDash(float time, float fraction)
            => new(Position, new TimePoint(time),
                math.clamp(fraction, AvatarRules.MinDashFraction, 1f), _damagedAt, KnockoutDirection,
                false);

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

            return new AvatarMovement(Position, _dashStarted, DashFraction, new TimePoint(time),
                length > math.EPSILON ? direction / length : float2.zero, ExposedSinceDash);
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
        //
        // A DASH IS THE WALK'S DIRECTION AT THE DASH'S SPEED, and that one line is what makes the two
        // inputs the same mechanism. A direction player steers their dash frame by frame because the
        // direction IS this frame's stick; a cursor player steers theirs because the direction is
        // recomputed towards the cursor every frame by Step. Neither is a special case, and the dash
        // no longer carries a launch direction to be locked to.
        //
        // THE LENGTH IS PART OF THE SPEED, NOT JUST OF THE AIM. An input at half deflection dashes at
        // half speed, and - the part that is load-bearing rather than cosmetic - an EMPTY input dashes
        // at zero. That is what stops a dash launched a frame before the pause menu opened from
        // carrying the avatar across a stopped level: a gated service hands out AvatarInput.None, and
        // a dash with no direction to serve moves nothing.

        /// <summary> Which way the avatar is driven this frame, and how fast, before scaling. </summary>
        public void GetTargetMove(float time, float2 direction, float length,
            in AvatarStepSpeeds speeds, out float2 targetDirection, out float targetSpeed)
        {
            if (InDamage(time))
            {
                targetDirection = KnockoutDirection;
                targetSpeed = speeds.KnockoutSpeed;
                return;
            }

            targetDirection = direction;
            targetSpeed = InDash(time) ? speeds.DashSpeed * length : speeds.MoveSpeed;
        }

        // ARRIVING IS A SNAP ONTO THE TARGET, not a step of exactly the remaining distance. The two
        // look identical and are not: `position += dir * dist` where `dir = toTarget / dist` leaves a
        // rounding residue about one float epsilon wide, which is the same order as the "is there any
        // distance left" test - so a held, motionless target alternated between "moving" and "stopped"
        // every frame and the avatar sat there flickering its squish, its eyes and its move trail.
        //
        // THE ARRIVAL CLAMP NOW APPLIES DURING A DASH, AND THAT IS THE WHOLE OF RULE 2. It used to be
        // skipped deliberately, because a dash covers DashSpeed * DashTime by design and clamping it
        // to the distance left would make a dash with the target nearby do nothing at all. What
        // changed is that a short dash is no longer a full dash with its travel cut off - the caller
        // shortened the WINDOW to match the distance before it ever started (ResolveDashFraction), so
        // the clamp and the window agree and the avatar arrives exactly as the dash ends. The clamp
        // stays as the guarantee rather than as the mechanism: if the level's own Speed track moves
        // mid-dash the arithmetic no longer lands exactly, and this is what makes an overshoot
        // impossible anyway. An avatar that arrives early simply keeps its remaining i-frames while
        // standing still - and if the player moves the cursor, the dash carries it there, because the
        // direction is recomputed every frame.

        /// <summary> Advances the avatar by one frame against this frame's control, returning both the
        /// moved state and what the consumer's animation half needs. </summary>
        public AvatarMovement Step(bool hasTarget, float2 target, float2 direction,
            in AvatarStepSpeeds speeds, float time, float deltaTime, out AvatarStepResult result)
        {
            var inDash = InDash(time);
            var inDamage = InDamage(time);

            var length = math.length(direction);
            var distanceToTarget = 0f;
            var arrived = false;

            if (hasTarget)
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

            GetTargetMove(time, direction, length, speeds, out var targetDirection, out var targetSpeed);

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
            // A DASH KEEPS ITS OWN REPORTED SPEED while its travel is still clamped: the trail behind
            // a dash is the dash's, and throttling it on the frame the avatar lands would put the
            // last puff of it under the avatar's feet. The knockback is excluded outright - it covers
            // a distance of its own rather than closing on a point.
            if (hasTarget && !inDamage && moving && step > distanceToTarget)
            {
                if (!inDash) targetSpeed = deltaTime > 0f ? distanceToTarget / deltaTime : 0f;
                step = distanceToTarget;
            }

            // An avatar that has already arrived falls through to the ordinary line, where its own
            // direction is zero and the step adds nothing - while a knockback, which sets a direction
            // of its own, still moves. Following the last hundredth of a unit would be chasing noise.
            var position = hasTarget && !inDamage && !arrived && distanceToTarget <= step
                ? target
                : Position + targetDirection * step;

            result = new AvatarStepResult(position, targetDirection, targetSpeed, moving, arrived);
            return Advance(position);
        }

        #endregion
    }
}
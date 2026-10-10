using System.Runtime.CompilerServices;
using BH.SDK.Rules;
using Unity.Mathematics;

namespace BH.SDK.Avatars
{
    // The avatar's whole movement mechanism as a VALUE (a bake's repair pass restores it wholesale),
    // touching no Unity runtime: the clock and every direction arrive as arguments. Camera clamping stays in
    // the consumer. Invariants and reasons: Docs/AVATAR_MOVEMENT.md in the SDK.

    /// <summary> The avatar's position and the state driving it: dash, knockback, and one frame's step. </summary>
    public readonly struct AvatarMovement
    {
        /// <summary> Where the avatar stands. </summary>
        public readonly float2 Position;

        /// <summary> Which way the last hit shoved the avatar; unit, or zero. </summary>
        public readonly float2 KnockoutDirection;

        // One dash is one shape scaled by this number: travel, i-frames and cooldown all scale with it.
        // Within [AvatarRules.MinDashFraction, 1]; a dash never taken leaves it at 1
        // (Docs/AVATAR_MOVEMENT.md).

        /// <summary> How much of a full dash the current one is, in
        /// [<see cref="AvatarRules.MinDashFraction"/>, 1]. Scales its travel, its i-frames and its
        /// cooldown together. </summary>
        public readonly float DashFraction;

        // Lockstep: every consumer calls Observe once per simulated frame, in the order launch, step,
        // Observe, size the collider. Forgetting it fails CLOSED: one dash, never another
        // (Docs/AVATAR_MOVEMENT.md).

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

        // The three cursor-dash rules in one line of arithmetic: saturate at 1 beyond the reach, END on a
        // nearer point, no dash below the floor. Here so the game and the bot verifier answer identically.

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

        // Read windows through these, never the bare constants: a collider sized off the constant outlives a
        // dash that ended early.

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

        // The window is a parameter: 0 is a real value (i-frames off), and DashFraction scales whatever is
        // passed.

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

        // Stage 3 of damage is an edge: InDamage is already false by the time a consumer can act on it.
        // TimePoint.Never makes this huge before the first hit, so it needs no case.

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

        // Once per simulated frame, from every consumer; it asks what the collider sizing asks, at the same
        // instant. Blind to collidability on purpose: authored content must not disarm the dash by hiding
        // the player.

        /// <summary> Records that this frame was sampled: if the avatar was touchable, the next dash
        /// is released. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AvatarMovement Observe(float time, float window)
            => ExposedSinceDash || InInvulnerability(time, window)
                ? this
                : new AvatarMovement(Position, _dashStarted, DashFraction, _damagedAt,
                    KnockoutDirection, true);

        // A dash with no direction is not taken at all (no cooldown, no window): a game rule for both
        // inputs, and no random direction on a path the corpus replays.

        /// <summary> The same avatar, having just launched a dash of <paramref name="fraction"/> of a
        /// full one. Callers gate on <see cref="CanDash"/> and resolve the fraction with
        /// <see cref="ResolveDashFraction"/> - this swallows nothing itself. </summary>
        public AvatarMovement StartDash(float time, float fraction)
            => new(Position, new TimePoint(time),
                math.clamp(fraction, AvatarRules.MinDashFraction, 1f), _damagedAt, KnockoutDirection,
                false);

        // The direction is captured once (a re-aimed shove becomes a chase) and arrives resolved; a ZERO one
        // is legal and moves nothing - no UnityEngine.Random here, randomness is addressed.

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

        // Branch order is the meaning of "uncontrolled": a hit outranks the dash. A dash is the walk's
        // direction at the dash's speed, so an EMPTY input dashes at zero (a pause cannot be flown through).

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

        // Arrival SNAPS onto the target (a step of the remaining distance leaves an epsilon residue and
        // flickers). The clamp also applies in a dash: the window was sized to the distance, the clamp is
        // the guarantee.

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

            // Both scalings land on targetSpeed, so walk, dash and knockback scale by the same number.
            targetSpeed *= speeds.Scale;

            var step = targetSpeed * deltaTime;

            // Report the speed the DISTANCE needs; the position is identical either way. A dash keeps its
            // own reported speed while clamped, and the knockback is excluded (Docs/AVATAR_MOVEMENT.md).
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
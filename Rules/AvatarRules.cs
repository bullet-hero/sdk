namespace BH.SDK.Rules
{
    // THE AVATAR'S BALANCE, FROZEN ON PURPOSE: values are the ones measured out of the shipped asset, and
    // bots, the PlayMode corpus and level design are all authored against them. Nothing here is settable.
    // Why they may not move, per value: BH.SDK/Docs/RULES.md, "Avatar balance". AvatarRulesTests pins them.

    /// <summary>
    /// The avatar's movement balance: frozen constants, not settings.
    /// </summary>
    public static class AvatarRules
    {
        // 1.5 default camera heights per second (DefaultZoom is the full height); dash and knockback are
        // each 3.3 times it, so both read as bursts rather than walking.

        /// <summary> Ordinary walking speed, in world units per second. </summary>
        public const float MoveSpeed = 15f;

        // A bigger avatar covers more of the screen per step, so leaving its speed alone makes it feel
        // slower the larger it gets: the dodge it has to make grows while the distance it can travel
        // does not. At 1 speed is exactly proportional to size, at 0 a giant and a dot move alike.

        /// <summary> How much of the player's size carries into its speed, in [0, 1]. </summary>
        public const float SizeSpeedInfluence = 1f;

        // DashSpeed * DashTime = 7.5 units is the reach levels are authored against: change one factor
        // and the reach moves, change both and the uncontrollable time moves. Neither is a free knob.

        /// <summary> Speed for the length of a dash, in world units per second. </summary>
        public const float DashSpeed = 50f;

        /// <summary> How long a dash lasts, in seconds. </summary>
        public const float DashTime = 0.15f;

        // THE SHORTEST DASH, as a fraction of a full one (AvatarMovement.DashFraction); ArrivedDistance of
        // reach means effectively no floor. A fraction because the reach scales with Player Size and Speed.
        // Cost and the ExposedSinceDash limiter: BH.SDK/Docs/RULES.md, "Avatar balance".

        /// <summary> The shortest dash, as a fraction of a full one - <see cref="ArrivedDistance"/> of
        /// reach, so effectively none. A target nearer than this is not dashed to at all. </summary>
        public const float MinDashFraction = ArrivedDistance / (DashSpeed * DashTime);

        // Measured from the LAUNCH and EXACTLY DashInvulnerabilityTime: the vulnerable window is one sampled
        // frame. Lockstep: BaseAvatarService.DriveAvatar launches BEFORE AvatarController.UpdateAvatar, which
        // calls AvatarMovement.Observe; launching after Observe makes the window zero. See RULES.md.

        /// <summary> How long after a dash STARTS before another may be taken, in seconds. </summary>
        public const float DashCooldown = 0.3f;

        // Dash i-frames: its own number, not DashTime. Must outlast the dash (IFrames_OutlastTheDash) and
        // MAY NOT EXCEED DashCooldown: then no frame is ever touchable, i.e. literal immunity. See RULES.md.

        /// <summary> How long a dash keeps the avatar untouchable, in seconds. 0 means never. </summary>
        public const float DashInvulnerabilityTime = 0.3f;

        // The dash's own speed; over DamageTime it travels 7.5 units, one full dash. The distance is tuned
        // through DamageTime, not this speed (30 read as a nudge). DamageTime sells the hit, this lands it.

        /// <summary> Speed of the shove a hit gives, in world units per second. </summary>
        public const float KnockoutSpeed = 50f;

        /// <summary> How long that shove lasts, with the avatar answering no input, in seconds. </summary>
        public const float DamageTime = 0.15f;

        // How long the avatar CANNOT BE HIT after a hit (DamageTime is how long it is not steering): one
        // long collision costs one life. The warm bot's route verifier counts a replayed hit with it.

        /// <summary> How long after a counted hit every further collision is ignored, in seconds. </summary>
        public const float DamageTimeout = 1f;

        /// <summary> The avatar's own scale, before the level's own Player Size track. </summary>
        public const float AvatarScale = 0.5f;

        // One duration both ways; the asymmetry is the ease (AvatarPresenceRamp). Must stay under 0.5, the
        // checkpoint rewind's slowdown: above it the departure is still playing at the seek, the avatar
        // never reaches nothing and IsDespawned never becomes true.

        /// <summary> How long the avatar takes to grow in, and to compress back to a point, in
        /// seconds. </summary>
        public const float SpawnTime = 0.3f;

        // The hitbox is smaller than the drawing by well over a third: a bullet clipping the outline
        // without killing reads generous. Just Shapes & Beats runs 0.4 of the half-extent; see RULES.md.

        /// <summary> The collision radius as a fraction of the avatar's drawn scale. </summary>
        public const float CollisionScale = 0.3f;

        // Frame-rate dependent by construction (`lerp(current, target, speed * dt)`), which is why it
        // is 30 rather than a fraction: it is a per-second rate, not a per-frame one. It moves nothing -
        // only the heading the avatar is drawn facing.

        /// <summary> How fast the drawn heading catches up with the direction of travel. </summary>
        public const float RotateLerpSpeed = 30f;

        // WELL UNDER WHAT A PLAYER CAN SEE and well over the noise a resting stick, a moving camera or
        // a pointer between two pixels produces. The avatar is about 0.5 across.

        /// <summary> How close to a target counts as standing on it, in world units. </summary>
        public const float ArrivedDistance = 0.01f;
    }
}
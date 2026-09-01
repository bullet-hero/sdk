using BH.SDK.Rules;

namespace BH.SDK.Avatars
{
    // THE THREE SPEEDS ARE FROZEN, THE SCALE IS PER FRAME. Keeping the scale separate rather than
    // pre-multiplying it into each speed is what lets a caller hold this struct for a whole replay and
    // only refresh the one field the level's own Player tracks actually move.
    //
    // WHY THE THREE FIELDS SURVIVED BECOMING CONSTANTS. Collapsing this to a bare float, with Step
    // reading AvatarRules directly, was the obvious simplification and it costs the one thing that
    // matters here: Step could then only ever be exercised at the shipped numbers. The behaviours worth
    // testing - the arrival clamp being suppressed during a dash, damage outranking a dash, a knockback
    // outrunning a walk - are about the RELATIONS between these three, and pinning them needs the
    // freedom to vary them. So Default(scale) is what the game and the verifier call, and the full
    // constructor is what a test calls.

    /// <summary> How fast the avatar moves in each of its three modes, and this frame's scaling. </summary>
    public readonly struct AvatarStepSpeeds
    {
        /// <summary> Ordinary walking speed, before <see cref="Scale"/>. </summary>
        public readonly float MoveSpeed;

        /// <summary> Speed for the length of a dash, before <see cref="Scale"/>. </summary>
        public readonly float DashSpeed;

        /// <summary> Speed of the shove a hit gives, before <see cref="Scale"/>. </summary>
        public readonly float KnockoutSpeed;

        /// <summary> This frame's multiplier - the level's Speed track times as much of the player's
        /// size as <see cref="AvatarRules.SizeSpeedInfluence"/> lets through. See
        /// <see cref="AvatarMovement.GetSpeedScale"/>. </summary>
        public readonly float Scale;

        public AvatarStepSpeeds(float moveSpeed, float dashSpeed, float knockoutSpeed, float scale)
        {
            MoveSpeed = moveSpeed;
            DashSpeed = dashSpeed;
            KnockoutSpeed = knockoutSpeed;
            Scale = scale;
        }

        /// <summary> The game's own speeds under a given per-frame scaling - what every caller outside
        /// a test uses. </summary>
        public static AvatarStepSpeeds Default(float scale)
            => new(AvatarRules.MoveSpeed, AvatarRules.DashSpeed, AvatarRules.KnockoutSpeed, scale);

        /// <summary> The same speeds under a different per-frame scaling. </summary>
        public AvatarStepSpeeds Scaled(float scale) => new(MoveSpeed, DashSpeed, KnockoutSpeed, scale);
    }
}

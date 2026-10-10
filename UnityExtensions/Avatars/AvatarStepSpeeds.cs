using BH.SDK.Rules;

namespace BH.SDK.Avatars
{
    // The three speeds stay separate from the per-frame Scale so a replay refreshes one field. They survive
    // being constants so tests can vary the RELATIONS between them; Default(scale) is the game's call.

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

        /// <summary> The four numbers a step is driven by, passed in rather than read from a setting. </summary>
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

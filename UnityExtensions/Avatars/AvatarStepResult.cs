using Unity.Mathematics;

namespace BH.SDK.Avatars
{
    // IT RETURNS MORE THAN THE POSITION because the animation half of AvatarController needs exactly
    // these four and must not recompute any of them: the direction it turns towards, the squish it
    // picks, and the move/stop edges every effect and tween is driven off all come from here. Two
    // copies of "was it moving this frame" is how the squish and the trail start disagreeing.

    /// <summary> What one <see cref="AvatarMovement.Step"/> produced. </summary>
    public readonly struct AvatarStepResult
    {
        /// <summary> Where the avatar now stands. </summary>
        public readonly float2 Position;

        /// <summary> The direction it was driven in; unit, or zero when it is not being driven. </summary>
        public readonly float2 TargetDirection;

        /// <summary> How fast it was driven, scaling included - and it is the speed actually
        /// TRAVELLED, not the setting: closing the last fraction of a unit onto a target reports
        /// that fraction over the frame, so a caller can tell a crawl from a sprint. </summary>
        public readonly float TargetSpeed;

        /// <summary> Whether it had a direction to move in at all this frame. </summary>
        public readonly bool Moving;

        /// <summary> Whether it is standing on its target, inside
        /// <see cref="Rules.AvatarRules.ArrivedDistance"/>. </summary>
        public readonly bool Arrived;

        // THERE IS NO MoveAngle HERE, AND THAT IS THE POINT. It existed - `atan2(TargetDirection.y,
        // TargetDirection.x)` - and a zero direction turned into 0 radians, i.e. "facing +X" rather
        // than "nothing is driving this avatar". A consumer that lerps its heading towards it swings
        // the avatar to the right on every arrival; that is exactly what happened, on a followed
        // route and on a released key alike (docs/issues/MOVEMENT_HISTORY.md 16). The direction is
        // handed over raw so the decision about a zero one is made where the alternative is known -
        // `AvatarController.ResolveHeading`.

        /// <summary> Everything one simulated step produced; nothing mutates it afterwards. </summary>
        public AvatarStepResult(float2 position, float2 targetDirection, float targetSpeed,
            bool moving, bool arrived)
        {
            Position = position;
            TargetDirection = targetDirection;
            TargetSpeed = targetSpeed;
            Moving = moving;
            Arrived = arrived;
        }
    }
}
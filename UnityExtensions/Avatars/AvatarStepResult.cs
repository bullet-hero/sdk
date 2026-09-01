using Unity.Mathematics;

namespace BH.SDK.Avatars
{
    // IT RETURNS MORE THAN THE POSITION because the animation half of AvatarController needs exactly
    // these four and must not recompute any of them: the heading it lerps towards, the squish it
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

        public AvatarStepResult(float2 position, float2 targetDirection, float targetSpeed,
            bool moving, bool arrived)
        {
            Position = position;
            TargetDirection = targetDirection;
            TargetSpeed = targetSpeed;
            Moving = moving;
            Arrived = arrived;
        }

        /// <summary> The heading the animation half lerps towards. </summary>
        public float MoveAngle => math.atan2(TargetDirection.y, TargetDirection.x);
    }
}

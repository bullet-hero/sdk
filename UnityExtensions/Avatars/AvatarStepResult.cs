using Unity.Mathematics;

namespace BH.SDK.Avatars
{
    // Carries what the animation half needs so it never recomputes "was it moving this frame".

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

        // No MoveAngle on purpose: a zero direction would read as "facing +X". The raw direction is handed
        // over; the consumer decides (AvatarController.ResolveHeading).

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
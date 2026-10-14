using System;

namespace BH.SDK.Models
{
    /// <summary> Which parts of the avatar a skin draws besides its body grid, which is always drawn.
    /// A part left out keeps its colour, so switching it back on restores it. </summary>
    [Flags]
    public enum AvatarSkinElements
    {
        None = 0,
        /// <summary> Both eyes. </summary>
        Eyes = 1 << 0,
        /// <summary> Both hands. </summary>
        Hands = 1 << 1,
        /// <summary> The shadow under the body. </summary>
        Shadow = 1 << 2,
        /// <summary> The trail left while moving. </summary>
        MoveEffect = 1 << 3,
        /// <summary> The trail left by a dash. </summary>
        DashEffect = 1 << 4,
        /// <summary> The burst a hit throws off. </summary>
        DamageEffect = 1 << 5,
        /// <summary> The body tinting towards its dash colour while dashing. </summary>
        DashBody = 1 << 6,

        /// <summary> Every part. </summary>
        All = Eyes | Hands | Shadow | MoveEffect | DashEffect | DamageEffect | DashBody,

        /// <summary> The classic avatar and every fresh skin: everything but the hands, which are opt-in.
        /// </summary>
        Classic = All & ~Hands,
    }
}

using System;
using BH.SDK.Models.Enums.Values;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> The aspect ratios a level accepts, and what it plays at on a screen outside them. </summary>
    public interface IScreenLimit : IModel<IScreenLimit>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public ScreenLimitType GetModelType();

        /// <summary> True when the screen already matches, so nothing has to be letterboxed. </summary>
        public bool IsValid(float currentAspect);

        /// <summary> The aspect the level is actually framed at on this screen - the bars are whatever is left. </summary>
        public float GetValid(float currentAspect);
    }
}
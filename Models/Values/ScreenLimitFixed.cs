using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// Pins the view to exactly one aspect ratio, letterboxing everything else. The strict choice for
    /// levels whose patterns are only fair at the ratio they were authored on.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ScreenLimitFixed : IScreenLimit, IModel<ScreenLimitFixed>
    {
        /// <summary> The one ratio the level is played at, whatever the device reports. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Aspect)]
        public ScreenAspect Aspect { get; set; }
        
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public ScreenLimitType GetModelType() => ScreenLimitType.Fixed;
        /// <summary> True only on the one aspect this level accepts. </summary>
        public bool IsValid(float currentAspect) => BHSDKMath.Approximately(Aspect.GetAspect(), currentAspect);
        /// <summary> Always the authored aspect - every other screen gets bars. </summary>
        public float GetValid(float currentAspect) => Aspect.GetAspect();

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ScreenLimitFixed()
        {
            Aspect = new ScreenAspect();
        }
        /// <summary> Built from its aspect. </summary>
        public ScreenLimitFixed(ScreenAspect aspect)
        {
            Aspect = aspect;
        }
    }
}
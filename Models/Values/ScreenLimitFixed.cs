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
        
        public ScreenLimitType GetModelType() => ScreenLimitType.Fixed;
        public bool IsValid(float currentAspect) => BHSDKMath.Approximately(Aspect.GetAspect(), currentAspect);
        public float GetValid(float currentAspect) => Aspect.GetAspect();

        public ScreenLimitFixed()
        {
            Aspect = new ScreenAspect();
        }
        public ScreenLimitFixed(ScreenAspect aspect)
        {
            Aspect = aspect;
        }
    }
}
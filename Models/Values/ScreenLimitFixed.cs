using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// Pins the view to exactly one aspect ratio, letterboxing everything else. The strict choice for
    /// levels whose patterns are only fair at the ratio they were authored on.
    /// </summary>
    [RuleContainer]
    public class ScreenLimitFixed : IScreenLimit, IModel<ScreenLimitFixed>
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
        public void Reset()
        {
            Aspect.Reset();
        }

        public object Clone() => Copy();
        IScreenLimit ICopyable<IScreenLimit>.Copy() => new ScreenLimitFixed(Aspect.Copy());
        public ScreenLimitFixed Copy() => new(Aspect.Copy());
        
        public void Update(ScreenLimitFixed src)
        {
            Aspect = src.Aspect.Copy();
        }

        public void Pull(ScreenLimitFixed src)
        {
            Aspect.Pull(src.Aspect);
        }

        void IUpdatable<IScreenLimit>.Update(IScreenLimit src)
        {
            if (src is ScreenLimitFixed value) Update(value);
        }
        void IMoveable<IScreenLimit>.Pull(IScreenLimit src)
        {
            if (src is ScreenLimitFixed value) Pull(value);
        }

        public override bool Equals(object obj) => obj is ScreenLimitFixed value && Equals(value);
        public override int GetHashCode() => Aspect != null ? Aspect.GetHashCode() : 0;

        public bool Equals(IScreenLimit other) => other is ScreenLimitFixed value && Equals(value);
        public bool Equals(ScreenLimitFixed other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Aspect.Equals(other.Aspect);
            return result;
        }
    }
}
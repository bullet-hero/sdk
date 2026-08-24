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
    /// Clamps the view between two aspect ratios - the practical middle ground between None and
    /// Fixed: a level plays natively on anything from phone to ultrawide, letterboxed only outside.
    /// </summary>
    [RuleContainer]
    public class ScreenLimitBounds : IScreenLimit, IModel<ScreenLimitBounds>
    {
        /// <summary> Narrowest ratio still shown as-is; anything narrower is clamped to it. </summary>
        [RuleNotNull]
        [JsonProperty(Names.MinAspect)]
        public ScreenAspect MinAspect { get; set; }

        /// <summary> Widest ratio still shown as-is; anything wider is clamped to it. </summary>
        [RuleNotNull]
        [JsonProperty(Names.MaxAspect)]
        public ScreenAspect MaxAspect { get; set; }
        
        public ScreenLimitType GetModelType() => ScreenLimitType.Bounds;
        public bool IsValid(float currentAspect)
        {
            var minAspect = MinAspect.GetAspect();
            if (currentAspect < minAspect) return false;
            
            var maxAspect = MaxAspect.GetAspect();
            if (currentAspect > maxAspect) return false;
            
            return true;
        }
        public float GetValid(float currentAspect)
        {
            var minAspect = MinAspect.GetAspect();
            var maxAspect = MaxAspect.GetAspect();
            return BHSDKMath.Clamp(currentAspect, minAspect, maxAspect);
        }

        public ScreenLimitBounds()
        {
            MinAspect = new ScreenAspect();
            MaxAspect = new ScreenAspect();
        }
        public ScreenLimitBounds(ScreenAspect minAspect, ScreenAspect maxAspect)
        {
            MinAspect = minAspect;
            MaxAspect = maxAspect;
        }
        public void Reset()
        {
            MinAspect.Reset();
            MaxAspect.Reset();
        }

        public object Clone() => Copy();
        IScreenLimit ICopyable<IScreenLimit>.Copy() => new ScreenLimitBounds(MinAspect.Copy(), MaxAspect.Copy());
        public ScreenLimitBounds Copy() => new(MinAspect.Copy(), MaxAspect.Copy());

        public void Update(ScreenLimitBounds src)
        {
            MinAspect = src.MinAspect.Copy();
            MaxAspect = src.MaxAspect.Copy();
        }

        public void Pull(ScreenLimitBounds src)
        {
            MinAspect.Pull(src.MinAspect);
            MaxAspect.Pull(src.MaxAspect);
        }

        void IUpdatable<IScreenLimit>.Update(IScreenLimit src)
        {
            if (src is ScreenLimitBounds value) Update(value);
        }
        void IMoveable<IScreenLimit>.Pull(IScreenLimit src)
        {
            if (src is ScreenLimitBounds value) Pull(value);
        }

        public override bool Equals(object obj) => obj is ScreenLimitBounds value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(MinAspect, MaxAspect);

        public bool Equals(IScreenLimit other) => other is ScreenLimitBounds value && Equals(value);
        public bool Equals(ScreenLimitBounds other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = MinAspect.Equals(other.MinAspect)
                         && MaxAspect.Equals(other.MaxAspect);
            return result;
        }
    }
}
using System;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Rotation drawn between two bounds with an independent draw per component. Same fields as
    /// EffectAngleRandomUniform; the split exists so the pair matches the scale/color families,
    /// where per-component really does produce a different look.
    /// </summary>
    [RuleContainer]
    public class EffectAngleRandomPerComponent : IEffectAngle, IModel<EffectAngleRandomPerComponent>
    {
        /// <summary> First bound of the draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AngleA)]
        public IFloat AngleA { get; set; }

        /// <summary> Second bound of the draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AngleB)]
        public IFloat AngleB { get; set; }
        
        public EffectAngleType GetModelType() => EffectAngleType.RandomPerComponent;

        public EffectAngleRandomPerComponent()
        {
            AngleA = new FloatValue(EffectRules.Angle.A_Default);
            AngleB = new FloatValue(EffectRules.Angle.B_Default);
        }
        public EffectAngleRandomPerComponent(float angleA, float angleB)
        {
            AngleA = new FloatValue(angleA);
            AngleB = new FloatValue(angleB);
        }
        public EffectAngleRandomPerComponent(IFloat angleA, IFloat angleB)
        {
            AngleA = angleA;
            AngleB = angleB;
        }
        public void Reset()
        {
            AngleA = new FloatValue(EffectRules.Angle.A_Default);
            AngleB = new FloatValue(EffectRules.Angle.B_Default);
        }

        public object Clone() => Copy();
        IEffectAngle ICopyable<IEffectAngle>.Copy() => new EffectAngleRandomPerComponent(AngleA.Copy(), AngleB.Copy());
        public EffectAngleRandomPerComponent Copy() => new(AngleA.Copy(), AngleB.Copy());

        public void Update(EffectAngleRandomPerComponent src)
        {
            AngleA = src.AngleA.Copy();
            AngleB = src.AngleB.Copy();
        }

        public void Pull(EffectAngleRandomPerComponent src)
        {
            AngleA = AngleA.PullFrom(src.AngleA);
            AngleB = AngleB.PullFrom(src.AngleB);
        }

        void IUpdatable<IEffectAngle>.Update(IEffectAngle src)
        {
            if (src is EffectAngleRandomPerComponent value) Update(value);
        }
        void IMoveable<IEffectAngle>.Pull(IEffectAngle src)
        {
            if (src is EffectAngleRandomPerComponent value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectAngleRandomPerComponent value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(AngleA, AngleB);
        
        public bool Equals(IEffectAngle other) => other is EffectAngleRandomPerComponent value && Equals(value);
        public bool Equals(EffectAngleRandomPerComponent other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = AngleA.Equals(other.AngleA)
                         && AngleB.Equals(other.AngleB);
            return result;
        }
    }
}
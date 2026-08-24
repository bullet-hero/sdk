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
    /// Spawn point sweeps back and forth instead of wrapping - no jump discontinuity, which reads as
    /// a scanning emitter rather than a spinning one.
    /// </summary>
    [RuleContainer]
    public class EffectShapeSpreadPingPong : IEffectShapeSpread, IModel<EffectShapeSpreadPingPong>
    {
        /// <summary> Portion of the shape the sweep covers. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IFloat Spread { get; set; }

        /// <summary> How fast the sweep travels. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Speed)]
        public IFloat Speed { get; set; }

        public EffectShapeSpreadType GetModelType() => EffectShapeSpreadType.PingPong;
        
        public EffectShapeSpreadPingPong()
        {
            Spread = new FloatValue(EffectRules.ShapeSpread.Spread_Default);
            Speed = new FloatValue(EffectRules.ShapeSpread.Speed_Default);
        }
        public EffectShapeSpreadPingPong(float spread, float speed)
        {
            Spread = new FloatValue(spread);
            Speed = new FloatValue(speed);
        }
        public EffectShapeSpreadPingPong(IFloat spread, IFloat speed)
        {
            Spread = spread;
            Speed = speed;
        }
        public void Reset()
        {
            Spread = new FloatValue(EffectRules.ShapeSpread.Spread_Default);
            Speed = new FloatValue(EffectRules.ShapeSpread.Speed_Default);
        }

        public object Clone() => Copy();
        IEffectShapeSpread ICopyable<IEffectShapeSpread>.Copy() => new EffectShapeSpreadPingPong(Spread.Copy(), Speed.Copy());
        public EffectShapeSpreadPingPong Copy() => new(Spread.Copy(), Speed.Copy());

        public void Update(EffectShapeSpreadPingPong src)
        {
            Spread = src.Spread.Copy();
            Speed = src.Speed.Copy();
        }

        public void Pull(EffectShapeSpreadPingPong src)
        {
            Spread = Spread.PullFrom(src.Spread);
            Speed = Speed.PullFrom(src.Speed);
        }

        void IUpdatable<IEffectShapeSpread>.Update(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadPingPong value) Update(value);
        }
        void IMoveable<IEffectShapeSpread>.Pull(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadPingPong value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectShapeSpreadPingPong value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Spread, Speed);
        
        public bool Equals(IEffectShapeSpread other) => other is EffectShapeSpreadPingPong value && Equals(value);
        public bool Equals(EffectShapeSpreadPingPong other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Spread.Equals(other.Spread)
                         && Speed.Equals(other.Speed);
            return result;
        }
    }
}
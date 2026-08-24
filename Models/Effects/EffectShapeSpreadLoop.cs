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
    /// Spawn point walks the shape in one direction and jumps back at the end - the classic rotating
    /// emitter that always turns the same way.
    /// </summary>
    [RuleContainer]
    public class EffectShapeSpreadLoop : IEffectShapeSpread, IModel<EffectShapeSpreadLoop>
    {
        /// <summary> Portion of the shape the walk covers before wrapping. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IFloat Spread { get; set; }

        /// <summary> How fast the spawn point advances - what sets the visible rotation rate. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Speed)]
        public IFloat Speed { get; set; }
        
        public EffectShapeSpreadType GetModelType() => EffectShapeSpreadType.Loop;

        public EffectShapeSpreadLoop()
        {
            Spread = new FloatValue(EffectRules.ShapeSpread.Spread_Default);
            Speed = new FloatValue(EffectRules.ShapeSpread.Speed_Default);
        }
        public EffectShapeSpreadLoop(float spread, float speed)
        {
            Spread = new FloatValue(spread);
            Speed = new FloatValue(speed);
        }
        public EffectShapeSpreadLoop(IFloat spread, IFloat speed)
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
        IEffectShapeSpread ICopyable<IEffectShapeSpread>.Copy() => new EffectShapeSpreadLoop(Spread.Copy(), Speed.Copy());
        public EffectShapeSpreadLoop Copy() => new(Spread.Copy(), Speed.Copy());

        public void Update(EffectShapeSpreadLoop src)
        {
            Spread = src.Spread.Copy();
            Speed = src.Speed.Copy();
        }

        public void Pull(EffectShapeSpreadLoop src)
        {
            Spread = Spread.PullFrom(src.Spread);
            Speed = Speed.PullFrom(src.Speed);
        }

        void IUpdatable<IEffectShapeSpread>.Update(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadLoop value) Update(value);
        }
        void IMoveable<IEffectShapeSpread>.Pull(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadLoop value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectShapeSpreadLoop value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Spread, Speed);
        
        public bool Equals(IEffectShapeSpread other) => other is EffectShapeSpreadLoop value && Equals(value);
        public bool Equals(EffectShapeSpreadLoop other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Spread.Equals(other.Spread)
                         && Speed.Equals(other.Speed);
            return result;
        }
    }
}
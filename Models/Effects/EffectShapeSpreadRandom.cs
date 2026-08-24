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
    /// Each particle lands at a random point of the shape's arc/segment - scattered, with no
    /// relation between consecutive spawns.
    /// </summary>
    [RuleContainer]
    public class EffectShapeSpreadRandom : IEffectShapeSpread, IModel<EffectShapeSpreadRandom>
    {
        /// <summary> How much of the shape the draw may cover; smaller values keep spawns clustered. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IFloat Spread { get; set; }
        
        public EffectShapeSpreadType GetModelType() => EffectShapeSpreadType.Random;
        
        public EffectShapeSpreadRandom()
        {
            Spread = new FloatValue(EffectRules.ShapeSpread.Spread_Default);
        }
        public EffectShapeSpreadRandom(IFloat spread)
        {
            Spread = spread;
        }
        public void Reset()
        {
            Spread = new FloatValue(EffectRules.ShapeSpread.Spread_Default);
        }
        
        public object Clone() => Copy();
        IEffectShapeSpread ICopyable<IEffectShapeSpread>.Copy() => new EffectShapeSpreadRandom(Spread.Copy());
        public EffectShapeSpreadRandom Copy() => new(Spread.Copy());
        
        public void Update(EffectShapeSpreadRandom src)
        {
            Spread = src.Spread.Copy();
        }

        public void Pull(EffectShapeSpreadRandom src)
        {
            Spread = Spread.PullFrom(src.Spread);
        }

        void IUpdatable<IEffectShapeSpread>.Update(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadRandom value) Update(value);
        }
        void IMoveable<IEffectShapeSpread>.Pull(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadRandom value) Pull(value);
        }

        public bool Equals(IEffectShapeSpread other) => other is EffectShapeSpreadRandom value && Equals(value);
        public bool Equals(EffectShapeSpreadRandom other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Spread.Equals(other.Spread);
            return result;
        }

        public override bool Equals(object obj) => obj is EffectShapeSpreadRandom value && Equals(value);
        public override int GetHashCode() => Spread != null ? Spread.GetHashCode() : 0;
    }
}
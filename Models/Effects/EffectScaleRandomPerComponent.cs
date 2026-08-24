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
    /// Size drawn per axis, so particles come out stretched in random directions - the deliberately
    /// non-proportional twin of EffectScaleRandomUniform.
    /// </summary>
    [RuleContainer]
    public class EffectScaleRandomPerComponent : IEffectScale, IModel<EffectScaleRandomPerComponent>
    {
        /// <summary> Per-axis first bound of the size draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ScaleX)]
        public IVector2 ScaleA { get; set; }

        /// <summary> Per-axis second bound of the size draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ScaleY)]
        public IVector2 ScaleB { get; set; }
        
        public EffectScaleType GetModelType() => EffectScaleType.RandomPerComponent;

        public EffectScaleRandomPerComponent()
        {
            ScaleA = new Vector2Value(
                EffectRules.Scale.A_X_Default, 
                EffectRules.Scale.A_Y_Default);
            ScaleB = new Vector2Value(
                EffectRules.Scale.B_X_Default, 
                EffectRules.Scale.B_Y_Default);
        }
        public EffectScaleRandomPerComponent(IVector2 scaleA, IVector2 scaleB)
        {
            ScaleA = scaleA;
            ScaleB = scaleB;
        }
        public void Reset()
        {
            ScaleA = new Vector2Value(
                EffectRules.Scale.A_X_Default, 
                EffectRules.Scale.A_Y_Default);
            ScaleB = new Vector2Value(
                EffectRules.Scale.B_X_Default, 
                EffectRules.Scale.B_Y_Default);
        }

        public object Clone() => Copy();
        IEffectScale ICopyable<IEffectScale>.Copy() => new EffectScaleRandomPerComponent(ScaleA.Copy(), ScaleB.Copy());
        public EffectScaleRandomPerComponent Copy() => new(ScaleA.Copy(), ScaleB.Copy());

        public void Update(EffectScaleRandomPerComponent src)
        {
            ScaleA = src.ScaleA.Copy();
            ScaleB = src.ScaleB.Copy();
        }

        public void Pull(EffectScaleRandomPerComponent src)
        {
            ScaleA = ScaleA.PullFrom(src.ScaleA);
            ScaleB = ScaleB.PullFrom(src.ScaleB);
        }

        void IUpdatable<IEffectScale>.Update(IEffectScale src)
        {
            if (src is EffectScaleRandomPerComponent value) Update(value);
        }
        void IMoveable<IEffectScale>.Pull(IEffectScale src)
        {
            if (src is EffectScaleRandomPerComponent value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectScaleRandomPerComponent value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(ScaleA, ScaleB);
        
        public bool Equals(IEffectScale other) => other is EffectScaleRandomPerComponent value && Equals(value);
        public bool Equals(EffectScaleRandomPerComponent other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = ScaleA.Equals(other.ScaleA)
                         && ScaleB.Equals(other.ScaleB);
            return result;
        }
    }
}
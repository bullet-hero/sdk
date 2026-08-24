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
    /// Emitter shape spawning particles along a segment - walls and curtains of bullets, where the
    /// Spread decides whether the wall fills evenly, sweeps, or scatters.
    /// </summary>
    [RuleContainer]
    public class EffectShapeLine : IEffectShape, IModel<EffectShapeLine>
    {
        /// <summary> One end of the segment, local to the effect object. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Start)]
        public IVector2 Start { get; set; }

        /// <summary> The other end of the segment. </summary>
        [RuleNotNull]
        [JsonProperty(Names.End)]
        public IVector2 End { get; set; }

        /// <summary> How successive particles walk from Start to End. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IEffectShapeSpread Spread { get; set; }
        
        public EffectShapeType GetModelType() => EffectShapeType.Line;
        
        public EffectShapeLine()
        {
            Start = new Vector2Value(
                EffectRules.Shape.LineStart_X_Default,
                EffectRules.Shape.LineStart_Y_Default);
            End = new Vector2Value(
                EffectRules.Shape.LineEnd_X_Default,
                EffectRules.Shape.LineEnd_Y_Default);
            Spread = new EffectShapeSpreadRandom();
        }
        public EffectShapeLine(IVector2 start, IVector2 end, IEffectShapeSpread spread)
        {
            Start = start;
            End = end;
            Spread = spread;
        }
        public void Reset()
        {
            Start = new Vector2Value(
                EffectRules.Shape.LineStart_X_Default,
                EffectRules.Shape.LineStart_Y_Default);
            End = new Vector2Value(
                EffectRules.Shape.LineEnd_X_Default,
                EffectRules.Shape.LineEnd_Y_Default);
            Spread = new EffectShapeSpreadRandom();
        }

        public object Clone() => Copy();
        IEffectShape ICopyable<IEffectShape>.Copy() => new EffectShapeLine(Start.Copy(), End.Copy(), Spread.Copy());
        public EffectShapeLine Copy() => new(Start.Copy(), End.Copy(), Spread.Copy());

        public void Update(EffectShapeLine src)
        {
            Start = src.Start.Copy();
            End = src.End.Copy();
            Spread = src.Spread.Copy();
        }

        public void Pull(EffectShapeLine src)
        {
            Start = Start.PullFrom(src.Start);
            End = End.PullFrom(src.End);
            Spread = Spread.PullFrom(src.Spread);
        }

        void IUpdatable<IEffectShape>.Update(IEffectShape src)
        {
            if (src is EffectShapeLine value) Update(value);
        }
        void IMoveable<IEffectShape>.Pull(IEffectShape src)
        {
            if (src is EffectShapeLine value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectShapeLine value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Start, End, Spread);
        
        public bool Equals(IEffectShape other) => other is EffectShapeLine value && Equals(value);
        public bool Equals(EffectShapeLine other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Start.Equals(other.Start)
                         && End.Equals(other.End)
                         && Spread.Equals(other.Spread);
            return result;
        }
    }
}
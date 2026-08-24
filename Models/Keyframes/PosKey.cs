using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Position key of a RectObject or the camera. Coordinates are local to the parent, so moving a
    /// parent carries its children along without touching their own keys.
    /// </summary>
    [RuleContainer]
    public class PosKey : Keyframe, IModel<PosKey>
    {
        /// <summary> Target position at this frame. Polymorphic, so a position can be re-rolled per
        /// frame (random spawn) instead of being fixed. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinPos, ValueRules.MaxPos)]
        [JsonProperty(Names.Vector2)]
        public IVector2 Pos { get; set; }

        public PosKey()
        {
            Pos = new Vector2Value();
        }
        public PosKey(IVector2 vector2, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Pos = vector2;
        }
        public override void Reset()
        {
            base.Reset();
            Pos = new Vector2Value();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        PosKey ICopyable<PosKey>.Copy() => CopyImpl();
        
        private PosKey CopyImpl() => new(Pos.Copy(), Frame, Ease);

        public void Update(PosKey src)
        {
            base.Update(src);

            Pos = src.Pos.Copy();
        }

        public void Pull(PosKey src)
        {
            base.Pull(src);

            Pos = Pos.PullFrom(src.Pos);
        }

        public override bool Equals(object obj) => obj is PosKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Pos);

        public bool Equals(PosKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Pos.Equals(other.Pos);
            return result;
        }
    }
}
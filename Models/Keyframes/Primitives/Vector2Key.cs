using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Generic 2D track entry, for vector parameters with no dedicated key type of their own -
    /// PosKey and ScaKey exist separately only because their rules and ranges differ.
    /// </summary>
    [RuleContainer]
    public class Vector2Key : Keyframe, IModel<Vector2Key>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(Vector2Value))]
        [JsonProperty(Names.Vector2)]
        public IVector2 Value { get; set; }

        public Vector2Key()
        {
            Value = new Vector2Value();
        }
        public Vector2Key(IVector2 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
        public override void Reset()
        {
            base.Reset();
            Value = new Vector2Value();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        Vector2Key ICopyable<Vector2Key>.Copy() => CopyImpl();
        
        private Vector2Key CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(Vector2Key src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(Vector2Key src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        public override bool Equals(object obj) => obj is Vector2Key value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

        public bool Equals(Vector2Key other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
    }
}
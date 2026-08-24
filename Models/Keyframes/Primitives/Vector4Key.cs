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
    /// Generic 4-component track entry, for parameters that travel as a quadruple (rects, shader-like
    /// params). The widest of the vector key types and the least specific in meaning.
    /// </summary>
    [RuleContainer]
    public class Vector4Key : Keyframe, IModel<Vector4Key>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(Vector4Value))]
        [JsonProperty(Names.Vector4)]
        public IVector4 Value { get; set; }

        public Vector4Key()
        {
            Value = new Vector4Value();
        }
        public Vector4Key(IVector4 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
        public override void Reset()
        {
            base.Reset();
            Value = new Vector4Value();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        Vector4Key ICopyable<Vector4Key>.Copy() => CopyImpl();
        
        private Vector4Key CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(Vector4Key src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(Vector4Key src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        public override bool Equals(object obj) => obj is Vector4Key value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

        public bool Equals(Vector4Key other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
    }
}
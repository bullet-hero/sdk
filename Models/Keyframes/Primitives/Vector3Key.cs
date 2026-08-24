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
    /// Generic 3D track entry - reached where a third axis matters (effect forces), not for placing
    /// objects in the 2D scene.
    /// </summary>
    [RuleContainer]
    public class Vector3Key : Keyframe, IModel<Vector3Key>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(Vector3Value))]
        [JsonProperty(Names.Vector3)]
        public IVector3 Value { get; set; }

        public Vector3Key()
        {
            Value = new Vector3Value();
        }
        public Vector3Key(IVector3 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
        public override void Reset()
        {
            base.Reset();
            Value = new Vector3Value();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        Vector3Key ICopyable<Vector3Key>.Copy() => CopyImpl();
        
        private Vector3Key CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(Vector3Key src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(Vector3Key src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        public override bool Equals(object obj) => obj is Vector3Key value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

        public bool Equals(Vector3Key other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
    }
}
using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Animates how a texture is mapped onto a ShapeObject - scrolling and repeating the image
    /// without moving the object itself. Both fields are concrete Vector2Value, not IVector2: a
    /// randomized UV would tear the image differently every frame.
    /// </summary>
    [RuleContainer]
    public class UVKey : Keyframe, IModel<UVKey>
    {
        /// <summary> Repeat count per axis; values above 1 tile the texture. </summary>
        [RuleNotNull]
        [RuleIVector2InRange(ValueRules.MinUv, ValueRules.MaxUv)]
        [JsonProperty(Names.Tilling)]
        public Vector2Value Tilling { get; set; }

        /// <summary> Shift of the texture within the rect - animate it for a scrolling surface. </summary>
        [RuleNotNull]
        [RuleIVector2InRange(ValueRules.MinUv, ValueRules.MaxUv)]
        [JsonProperty(Names.Offset)]
        public Vector2Value Offset { get; set; }

        public UVKey()
        {
            Tilling = new Vector2Value(ValueRules.DefaultUvX, ValueRules.DefaultUvY);
            Offset = new Vector2Value(ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
        }
        public UVKey(Vector2Value tilling, Vector2Value offset, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Tilling = tilling;
            Offset = offset;
        }
        public override void Reset()
        {
            base.Reset();
            Tilling = new Vector2Value(ValueRules.DefaultUvX, ValueRules.DefaultUvY);
            Offset = new Vector2Value(ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        UVKey ICopyable<UVKey>.Copy() => CopyImpl();
        
        private UVKey CopyImpl() => new(Tilling.Copy(), Offset.Copy(), Frame, Ease);

        public void Update(UVKey src)
        {
            base.Update(src);

            Tilling = src.Tilling.Copy();
            Offset = src.Offset.Copy();
        }

        public void Pull(UVKey src)
        {
            base.Pull(src);

            Tilling.Pull(src.Tilling);
            Offset.Pull(src.Offset);
        }

        public override bool Equals(object obj) => obj is UVKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Tilling, Offset);

        public bool Equals(UVKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Tilling.Equals(other.Tilling)
                         && Offset.Equals(other.Offset);
            return result;
        }
    }
}
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
    /// Animated draw order. Interpolation is meaningless here (a layer is a discrete slot), so its
    /// Ease effectively picks when the swap happens rather than blending toward it.
    /// </summary>
    [RuleContainer]
    public class LayerKey : Keyframe, IModel<LayerKey>
    {
        /// <summary> Draw order from this frame on - higher draws in front. </summary>
        [RuleNotNull(typeof(IntValue)), RuleIIntInRange(ValueRules.MinLayer, ValueRules.MaxLayer)]
        [JsonProperty(Names.Int)]
        public IInt Layer { get; set; }

        public LayerKey()
        {
            Layer = new IntValue();
        }
        public LayerKey(IInt value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Layer = value;
        }
        public override void Reset()
        {
            base.Reset();
            Layer = new IntValue();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        LayerKey ICopyable<LayerKey>.Copy() => CopyImpl();
        
        private LayerKey CopyImpl() => new(Layer.Copy(), Frame, Ease);

        public void Update(LayerKey src)
        {
            base.Update(src);

            Layer = src.Layer.Copy();
        }

        public void Pull(LayerKey src)
        {
            base.Pull(src);

            Layer = Layer.PullFrom(src.Layer);
        }

        public override bool Equals(object obj) => obj is LayerKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Layer);

        public bool Equals(LayerKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Layer.Equals(other.Layer);
            return result;
        }
    }
}
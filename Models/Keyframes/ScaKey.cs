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
    /// Two-axis multiplier key, used for both RectObject.Scales and RectObject.Sizes - the same shape
    /// serves "stretch relative to parent" and "how big the rect is" tracks.
    /// </summary>
    [RuleContainer]
    public class ScaKey : Keyframe, IModel<ScaKey>
    {
        /// <summary> Target scale/size at this frame; X and Y are independent, so non-uniform
        /// stretching is expressible. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinSca, ValueRules.MaxSca)]
        [JsonProperty(Names.Vector2)]
        public IVector2 Scale { get; set; }
        
        public ScaKey()
        {
            Scale = new Vector2Value();
        }
        public ScaKey(IVector2 scale, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Scale = scale;
        }
        public override void Reset()
        {
            base.Reset();
            Scale = new Vector2Value();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        ScaKey ICopyable<ScaKey>.Copy() => CopyImpl();
        
        private ScaKey CopyImpl() => new(Scale.Copy(), Frame, Ease);

        public void Update(ScaKey src)
        {
            base.Update(src);

            Scale = src.Scale.Copy();
        }

        public void Pull(ScaKey src)
        {
            base.Pull(src);

            Scale = Scale.PullFrom(src.Scale);
        }

        public override bool Equals(object obj) => obj is ScaKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Scale);

        public bool Equals(ScaKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Scale.Equals(other.Scale);
            return result;
        }
    }
}
using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Opaque color track entry, used for the level background. No alpha by construction - there is
    /// nothing behind the background to blend with.
    /// </summary>
    [RuleContainer]
    public class Color3Key : Keyframe, IModel<Color3Key>
    {
        /// <summary> Color at this frame; may be a ThemeRef, which is how backgrounds follow the
        /// active theme. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.Color)]
        public IColor3 Value { get; set; }

        public Color3Key()
        {
            Value = Color3Value.white;
        }
        public Color3Key(IColor3 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
        public override void Reset()
        {
            base.Reset();
            Value = Color3Value.white;
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        Color3Key ICopyable<Color3Key>.Copy() => CopyImpl();
        
        private Color3Key CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(Color3Key src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(Color3Key src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        public override bool Equals(object obj) => obj is Color3Key value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

        public bool Equals(Color3Key other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
    }
}
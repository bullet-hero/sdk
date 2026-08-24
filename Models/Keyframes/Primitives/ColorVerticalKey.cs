using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Bottom-to-top gradient across a texture object - the vertical twin of ColorHorizontalKey.
    /// </summary>
    [RuleContainer]
    public class ColorVerticalKey : Keyframe, IColor4X4Key, IModel<ColorVerticalKey>
    {
        /// <summary> Color of both bottom corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorBottom)]
        public IColor4 Color4Bottom { get; set; }

        /// <summary> Color of both top corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorTop)]
        public IColor4 Color4Top { get; set; }

        public ColorVerticalKey()
        {
            Color4Bottom = Color4Value.white;
            Color4Top = Color4Value.white;
        }
        public ColorVerticalKey(IColor4 color4, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Bottom = color4.Copy();
            Color4Top = color4.Copy();
        }
        public ColorVerticalKey(IColor4 color4Bottom, IColor4 color4Top, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Bottom = color4Bottom;
            Color4Top = color4Top;
        }
        public override void Reset()
        {
            base.Reset();
            Color4Bottom = Color4Value.white;
            Color4Top = Color4Value.white;
        }
        
        public Color4X4KeyType GetModelType() => Color4X4KeyType.Vertical;
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        ColorVerticalKey ICopyable<ColorVerticalKey>.Copy() => CopyImpl();
        IColor4X4Key ICopyable<IColor4X4Key>.Copy() => CopyImpl();
        
        private ColorVerticalKey CopyImpl() => new(Color4Bottom.Copy(), Color4Top.Copy(), Frame, Ease);

        public void Update(ColorVerticalKey src)
        {
            base.Update(src);

            Color4Bottom = src.Color4Bottom.Copy();
            Color4Top = src.Color4Top.Copy();
        }

        public void Pull(ColorVerticalKey src)
        {
            base.Pull(src);

            Color4Bottom = Color4Bottom.PullFrom(src.Color4Bottom);
            Color4Top = Color4Top.PullFrom(src.Color4Top);
        }

        void IUpdatable<IColor4X4Key>.Update(IColor4X4Key src)
        {
            if (src is ColorVerticalKey value) Update(value);
        }
        void IMoveable<IColor4X4Key>.Pull(IColor4X4Key src)
        {
            if (src is ColorVerticalKey value) Pull(value);
        }

        public override bool Equals(object obj) => obj is ColorVerticalKey value && Equals(value);
        public bool Equals(IColor4X4Key other) => other is ColorVerticalKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Color4Bottom, Color4Top);

        public bool Equals(ColorVerticalKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Color4Bottom.Equals(other.Color4Bottom)
                         && Color4Top.Equals(other.Color4Top);
            return result;
        }
    }
}
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
    /// Left-to-right gradient across a texture object: two colors instead of four, with the top and
    /// bottom corners of each side sharing one value.
    /// </summary>
    [RuleContainer]
    public class ColorHorizontalKey : Keyframe, IColor4X4Key, IModel<ColorHorizontalKey>
    {
        /// <summary> Color of both left corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorLeft)]
        public IColor4 Color4Left { get; set; }

        /// <summary> Color of both right corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorRight)]
        public IColor4 Color4Right { get; set; }

        public ColorHorizontalKey()
        {
            Color4Left = Color4Value.white;
            Color4Right = Color4Value.white;
        }
        public ColorHorizontalKey(IColor4 color4, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Left = color4.Copy();
            Color4Right = color4.Copy();
        }
        public ColorHorizontalKey(IColor4 color4Left, IColor4 color4Right, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Left = color4Left;
            Color4Right = color4Right;
        }
        public override void Reset()
        {
            base.Reset();
            Color4Left = Color4Value.white;
            Color4Right = Color4Value.white;
        }
        
        public Color4X4KeyType GetModelType() => Color4X4KeyType.Horizontal;
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        ColorHorizontalKey ICopyable<ColorHorizontalKey>.Copy() => CopyImpl();
        IColor4X4Key ICopyable<IColor4X4Key>.Copy() => CopyImpl();
        
        private ColorHorizontalKey CopyImpl() => new(Color4Left.Copy(), Color4Right.Copy(), Frame, Ease);

        public void Update(ColorHorizontalKey src)
        {
            base.Update(src);

            Color4Left = src.Color4Left.Copy();
            Color4Right = src.Color4Right.Copy();
        }

        public void Pull(ColorHorizontalKey src)
        {
            base.Pull(src);

            Color4Left = Color4Left.PullFrom(src.Color4Left);
            Color4Right = Color4Right.PullFrom(src.Color4Right);
        }

        void IUpdatable<IColor4X4Key>.Update(IColor4X4Key src)
        {
            if (src is ColorHorizontalKey value) Update(value);
        }
        void IMoveable<IColor4X4Key>.Pull(IColor4X4Key src)
        {
            if (src is ColorHorizontalKey value) Pull(value);
        }

        public override bool Equals(object obj) => obj is ColorHorizontalKey value && Equals(value);
        public bool Equals(IColor4X4Key other) => other is ColorHorizontalKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Color4Left, Color4Right);

        public bool Equals(ColorHorizontalKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Color4Left.Equals(other.Color4Left)
                         && Color4Right.Equals(other.Color4Right);
            return result;
        }
    }
}
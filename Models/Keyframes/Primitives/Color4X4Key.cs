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
    /// Fully independent corner colors - the widest member of the four-corner family, giving a
    /// barycentric blend across the quad. The other three variants exist so the common cases
    /// (flat, horizontal, vertical) don't have to store four colors that are mostly equal.
    /// </summary>
    [RuleContainer]
    public class Color4X4Key : Keyframe, IColor4X4Key, IModel<Color4X4Key>
    {
        /// <summary> Bottom-left corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorBL)]
        public IColor4 Color4BL { get; set; }

        /// <summary> Bottom-right corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorBR)]
        public IColor4 Color4BR { get; set; }

        /// <summary> Top-left corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorTL)]
        public IColor4 Color4TL { get; set; }

        /// <summary> Top-right corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorTR)]
        public IColor4 Color4TR { get; set; }
        
        public Color4X4Key()
        {
            Color4BL = Color4Value.white;
            Color4BR = Color4Value.white;
            Color4TL = Color4Value.white;
            Color4TR = Color4Value.white;
        }
        public Color4X4Key(IColor4 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4BL = value.Copy();
            Color4BR = value.Copy();
            Color4TL = value.Copy();
            Color4TR = value.Copy();
        }
        public Color4X4Key(IColor4 color4BL, IColor4 color4BR, IColor4 color4TL, IColor4 color4TR,
            int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4BL = color4BL;
            Color4BR = color4BR;
            Color4TL = color4TL;
            Color4TR = color4TR;
        }
        public override void Reset()
        {
            base.Reset();
            Color4BL = Color4Value.white;
            Color4BR = Color4Value.white;
            Color4TL = Color4Value.white;
            Color4TR = Color4Value.white;
        }
        
        public Color4X4KeyType GetModelType() => Color4X4KeyType.BariCentrical;
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        Color4X4Key ICopyable<Color4X4Key>.Copy() => CopyImpl();
        IColor4X4Key ICopyable<IColor4X4Key>.Copy() => CopyImpl();
        
        private Color4X4Key CopyImpl() => new(Color4BL.Copy(), Color4BR.Copy(), Color4TL.Copy(), Color4TR.Copy(), Frame, Ease);
        
        public void Update(Color4X4Key src)
        {
            base.Update(src);

            Color4BL = src.Color4BL.Copy();
            Color4BR = src.Color4BR.Copy();
            Color4TL = src.Color4TL.Copy();
            Color4TR = src.Color4TR.Copy();
        }

        public void Pull(Color4X4Key src)
        {
            base.Pull(src);

            Color4BL = Color4BL.PullFrom(src.Color4BL);
            Color4BR = Color4BR.PullFrom(src.Color4BR);
            Color4TL = Color4TL.PullFrom(src.Color4TL);
            Color4TR = Color4TR.PullFrom(src.Color4TR);
        }

        void IUpdatable<IColor4X4Key>.Update(IColor4X4Key src)
        {
            if (src is Color4X4Key value) Update(value);
        }
        void IMoveable<IColor4X4Key>.Pull(IColor4X4Key src)
        {
            if (src is Color4X4Key value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Color4X4Key value && Equals(value);
        public bool Equals(IColor4X4Key other) => other is Color4X4Key value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Color4BL, Color4BR, Color4TL, Color4TR);

        public bool Equals(Color4X4Key other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Color4BL.Equals(other.Color4BL)
                         && Color4BR.Equals(other.Color4BR)
                         && Color4TL.Equals(other.Color4TL)
                         && Color4TR.Equals(other.Color4TR);
            return result;
        }
    }
}
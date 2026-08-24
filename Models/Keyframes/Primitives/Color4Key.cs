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
    /// Flat color key and the simplest member of the four-corner family: one color painted on all
    /// four corners. Doubles as the plain RGBA key for text and any single-tint track.
    /// </summary>
    [RuleContainer]
    public class Color4Key : Keyframe, IColor4X4Key, IModel<Color4Key>
    {
        /// <summary> Color at this frame, applied uniformly across the rect. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.Color)]
        public IColor4 Value { get; set; }

        public Color4Key()
        {
            Value = Color4Value.white;
        }
        public Color4Key(IColor4 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
        public override void Reset()
        {
            base.Reset();
            Value = Color4Value.white;
        }
        
        public Color4X4KeyType GetModelType() => Color4X4KeyType.Value;
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        Color4Key ICopyable<Color4Key>.Copy() => CopyImpl();
        IColor4X4Key ICopyable<IColor4X4Key>.Copy() => CopyImpl();
        
        private Color4Key CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(Color4Key src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(Color4Key src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        void IUpdatable<IColor4X4Key>.Update(IColor4X4Key src)
        {
            if (src is Color4Key value) Update(value);
        }
        void IMoveable<IColor4X4Key>.Pull(IColor4X4Key src)
        {
            if (src is Color4Key value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Color4Key value && Equals(value);
        public bool Equals(IColor4X4Key other) => other is Color4Key value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

        public bool Equals(Color4Key other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
    }
}
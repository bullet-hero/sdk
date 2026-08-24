using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Base of every animated key in a level: a point in time plus how the value gets there.
    /// Concrete subclasses add exactly one payload field each. Tracks are plain lists with unique
    /// but not necessarily sorted frames - sorting is the consumer's job, not the format's.
    /// </summary>
    [RuleContainer]
    public class Keyframe : IKeyframe, IModel<Keyframe>
    {
        public const int DefaultFrame = 0;
        public const EaseType DefaultEase = EaseType.Linear;

        /// <summary> Level frame this key sits on, bounded by LevelSettings.FrameDuration. </summary>
        [RuleLevelFrame]
        [JsonProperty(Names.FrameShort)]
        public int Frame { get; set; }

        /// <summary> Interpolation used on the way INTO this key, i.e. it shapes the segment before
        /// it. Stored per key, not per track, so a single track can mix easings freely. </summary>
        [RuleEnumValid(DefaultEase)]
        [JsonProperty(Names.Ease)]
        public EaseType Ease { get; set; }

        public Keyframe()
        {
            Frame = DefaultFrame;
            Ease = DefaultEase;
        }
        public Keyframe(int frame, EaseType ease = DefaultEase)
        {
            Frame = frame;
            Ease = ease;
        }
        public virtual void Reset()
        {
            Frame = DefaultFrame;
            Ease = DefaultEase;
        }

        public virtual object Clone() => CopyImpl();
        public virtual Keyframe Copy() => CopyImpl();
        
        private Keyframe CopyImpl() => new(Frame, Ease);

        public void Update(Keyframe src)
        {
            Frame = src.Frame;
            Ease = src.Ease;
        }

        public void Pull(Keyframe src)
        {
            Frame = src.Frame;
            Ease = src.Ease;
        }

        public override bool Equals(object obj) => obj is Keyframe value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Frame, (int)Ease);

        public bool Equals(Keyframe other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Frame.Equals(other.Frame) && Ease == other.Ease;
            return result;
        }
    }
}
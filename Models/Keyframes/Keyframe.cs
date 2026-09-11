using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Base of every animated key in a level: a point in time plus how the value gets there.
    /// Concrete subclasses add exactly one payload field each. Tracks are plain lists with unique
    /// but not necessarily sorted frames - sorting is the consumer's job, not the format's.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public partial class Keyframe : IKeyframe, IModel<Keyframe>
    {
        /// <summary> Where a keyframe sits when nothing says otherwise - the first frame of whatever it
        /// belongs to, which is <see cref="FrameRules.MinFrame"/> and not zero. </summary>
        public const int DefaultFrame = FrameRules.MinFrame;

        /// <summary> How it is blended into when nothing says otherwise. </summary>
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Keyframe()
        {
            Frame = DefaultFrame;
            Ease = DefaultEase;
        }

        /// <summary> Built from its frame and default ease. </summary>
        public Keyframe(int frame, EaseType ease = DefaultEase)
        {
            Frame = frame;
            Ease = ease;
        }
    }
}
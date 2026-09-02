using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Base of every post-processing key. Mirrors Keyframe (Frame + Ease) but implements IKeyframe
    /// directly instead of deriving from it, because it adds a per-key Active toggle no other
    /// keyframe family has - each effect can switch itself off mid-track, not just change values.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public partial class PostProcessingKeyframe : IKeyframe, IModel<PostProcessingKeyframe>
    {
        /// <summary> Whether this effect runs from this frame on. Independent of - and additional to -
        /// PostProcessingEvents.Active, which gates the whole stack. </summary>
        [JsonProperty(Names.ActiveShort)]
        public bool Active { get; set; }

        /// <summary> Level frame this key sits on. </summary>
        [RuleLevelFrame]
        [JsonProperty(Names.FrameShort)]
        public int Frame { get; set; }

        /// <summary> Interpolation used on the way into this key. </summary>
        [RuleEnumValid(Keyframe.DefaultEase)]
        [JsonProperty(Names.Ease)]
        public EaseType Ease { get; set; }

        public PostProcessingKeyframe()
        {
            Active = PostProcessingRules.ActiveDefault;
            Frame = Keyframe.DefaultFrame;
            Ease = Keyframe.DefaultEase;
        }
        public PostProcessingKeyframe(bool active, int frame, EaseType ease = Keyframe.DefaultEase)
        {
            Active = active;
            Frame = frame;
            Ease = ease;
        }
    }
}
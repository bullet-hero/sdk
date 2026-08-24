using System;
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
    public class PostProcessingKeyframe : IKeyframe, IModel<PostProcessingKeyframe>
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
        public virtual void Reset()
        {
            Frame = Keyframe.DefaultFrame;
            Ease = Keyframe.DefaultEase;
            Active = PostProcessingRules.ActiveDefault;
        }

        public virtual object Clone() => CopyImpl();
        public virtual PostProcessingKeyframe Copy() => CopyImpl();
        
        private PostProcessingKeyframe CopyImpl() => new(Active, Frame, Ease);

        public void Update(PostProcessingKeyframe src)
        {
            Active = src.Active;
            Frame = src.Frame;
            Ease = src.Ease;
        }

        public void Pull(PostProcessingKeyframe src)
        {
            Active = src.Active;
            Frame = src.Frame;
            Ease = src.Ease;
        }

        public override bool Equals(object obj) => obj is PostProcessingKeyframe value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Active, Frame, (int)Ease);

        public bool Equals(PostProcessingKeyframe other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Active == other.Active
                         && Frame.Equals(other.Frame)
                         && Ease == other.Ease;
            return result;
        }
    }
}
using System;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    /// <summary>
    /// Particle rendering options with their own framerate budget - effects can be simulated slower
    /// than the game itself, which is the main lever for keeping heavy levels playable on phones.
    /// </summary>
    [RuleContainer]
    public class EffectsGraphicsSettings : BaseGraphicsSettings, IFrameable,
        IModel<EffectsGraphicsSettings>, IMoveable<EffectsGraphicsSettings>
    {
        /// <summary> Where the effect update rate comes from - separate from the game's own, hence
        /// the duplicate of GraphicsSettings' pair of fields. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.FramerateTarget)]
        public FramerateTarget FramerateTarget { get; set; }

        /// <summary> Explicit effect update rate, used when FramerateTarget says so. Lower than the
        /// game framerate by default. </summary>
        [RuleMinValue(1)]
        [JsonProperty(Names.FixedFramerate)]
        public int FixedFramerate { get; set; }

        /// <summary> Longest effect state the player will fast-forward when seeking, before it gives
        /// up and starts the effect fresh. </summary>
        [RuleMinValue(0.2f)]
        [JsonProperty(Names.MaxScrubTime)]
        public float MaxScrubTime { get; set; }

        // The two budgets below are simulation STEPS, i.e. GPU dispatches, and they are the phone
        // lever for effects the way FixedFramerate is for the update rate. They are separate numbers
        // because they bound different things: the first is per effect and decides how a single
        // replay LOOKS, the second is per frame across the whole pool and decides what the worst
        // frame COSTS. Raising the first alone can still be absorbed by the second, which is the
        // intended relationship - quality is requested per effect and granted by the frame.
        //
        // A replay rebuilds a graph from an empty state, so each of its steps is one particle spawn
        // cohort: too few and a continuous stream comes back as that many visible packets. This is
        // NOT the budget for catching a running graph up after a frame drop - that one stays a
        // constant in GamePlayer, since a wrong value there can feed a longer frame back into an
        // even longer one.

        /// <summary> Simulation steps one effect may spend replaying itself - after a scrub
        /// backwards, an edit, or re-entering the frame. Higher looks closer to real playback and
        /// costs proportionally more GPU dispatches. </summary>
        [RuleInRange(EffectRules.ReplayStepBudget_Min, EffectRules.ReplayStepBudget_Max)]
        [JsonProperty(Names.ReplayStepBudget)]
        public int ReplayStepBudget { get; set; }

        /// <summary> Simulation steps every live effect together may spend in one frame. Past it,
        /// each effect's steps are compressed - same time span covered by fewer, longer steps - so
        /// this is the ceiling on a frame's effect cost, not on its accuracy. </summary>
        [RuleInRange(EffectRules.FrameStepBudget_Min, EffectRules.FrameStepBudget_Max)]
        [JsonProperty(Names.FrameStepBudget)]
        public int FrameStepBudget { get; set; }

        public EffectsGraphicsSettings()
        {
            Render = true;
            FramerateTarget = FramerateTarget.Fixed;
            FixedFramerate = 50;
            MaxScrubTime = 0.5f;
            ReplayStepBudget = EffectRules.ReplayStepBudget_Default;
            FrameStepBudget = EffectRules.FrameStepBudget_Default;
        }
        public EffectsGraphicsSettings(bool render, FramerateTarget framerateTarget,
            int fixedFramerate, float maxScrubTime, int replayStepBudget, int frameStepBudget) : base(render)
        {
            FramerateTarget = framerateTarget;
            FixedFramerate = fixedFramerate;
            MaxScrubTime = maxScrubTime;
            ReplayStepBudget = replayStepBudget;
            FrameStepBudget = frameStepBudget;
        }
        public override void Reset()
        {
            base.Reset();
            Render = true;
            FramerateTarget = FramerateTarget.Fixed;
            FixedFramerate = 50;
            MaxScrubTime = 0.5f;
            ReplayStepBudget = EffectRules.ReplayStepBudget_Default;
            FrameStepBudget = EffectRules.FrameStepBudget_Default;
        }

        public override object Clone() => CopyImpl();
        public override BaseGraphicsSettings Copy() => CopyImpl();
        EffectsGraphicsSettings ICopyable<EffectsGraphicsSettings>.Copy() => CopyImpl();

        private EffectsGraphicsSettings CopyImpl() => new(Render, FramerateTarget, FixedFramerate,
            MaxScrubTime, ReplayStepBudget, FrameStepBudget);

        public void Pull(EffectsGraphicsSettings source)
        {
            Render = source.Render;
            FramerateTarget = source.FramerateTarget;
            FixedFramerate = source.FixedFramerate;
            MaxScrubTime = source.MaxScrubTime;
            ReplayStepBudget = source.ReplayStepBudget;
            FrameStepBudget = source.FrameStepBudget;
        }

        public void Update(EffectsGraphicsSettings src)
        {
            base.Update(src);

            FramerateTarget = src.FramerateTarget;
            FixedFramerate = src.FixedFramerate;
            MaxScrubTime = src.MaxScrubTime;
            ReplayStepBudget = src.ReplayStepBudget;
            FrameStepBudget = src.FrameStepBudget;
        }

        public override bool Equals(object obj) => obj is EffectsGraphicsSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(),
            (int)FramerateTarget, FixedFramerate, MaxScrubTime, ReplayStepBudget, FrameStepBudget);

        public bool Equals(EffectsGraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other)
                   && FramerateTarget == other.FramerateTarget
                   && FixedFramerate == other.FixedFramerate
                   && MaxScrubTime.Equals(other.MaxScrubTime)
                   && ReplayStepBudget == other.ReplayStepBudget
                   && FrameStepBudget == other.FrameStepBudget;
        }
    }
}
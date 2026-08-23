using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    /// <summary>
    /// Audio playback options that cost performance: whether DSP effects run, and how hard the
    /// engine works to keep audio locked to the level timeline. Inherited Render mutes audio entirely.
    /// </summary>
    public class AudioGraphicsSettings : BaseGraphicsSettings,
        IModel<AudioGraphicsSettings>, IMoveable<AudioGraphicsSettings>
    {
        /// <summary> Whether a track's DSP chain is processed. Off saves CPU on weak devices, at the
        /// cost of the mapper's intended sound. </summary>
        [JsonProperty(Names.RenderEffects)]
        public bool RenderEffects { get; set; }

        // The three below are one control loop, and they only make sense read together - see
        // GamePlayer's TrackTimeControl, which is where they are actually applied. Bounds are
        // AudioRules'; nothing clamps at runtime, same as the rest of UserSettings.

        /// <summary> How far the playhead has to jump before it counts as a discontinuity - a scrub,
        /// a loop wrap - rather than as drift. Only a jump costs a hard reseek, which is audible;
        /// ordinary drift is handled by PitchCorrection instead. </summary>
        [RuleInRange(AudioRules.MinResyncJumpTime, AudioRules.MaxResyncJumpTime)]
        [JsonProperty(Names.ResyncJumpTime)]
        public float ResyncJumpTime { get; set; }

        /// <summary> Desync below which playback is left at exactly its intended rate. Not precision
        /// for its own sake: any other rate engages the engine's resampler, so this decides how much
        /// desync is worth that. </summary>
        [RuleInRange(AudioRules.MinSyncDeadZone, AudioRules.MaxSyncDeadZone)]
        [JsonProperty(Names.SyncDeadZone)]
        public float SyncDeadZone { get; set; }

        /// <summary> How far the playback rate may be bent to close a desync gradually instead of
        /// jumping. 0 turns that off and leaves the hard reseek as the only correction. </summary>
        [RuleInRange(AudioRules.MinPitchCorrection, AudioRules.MaxPitchCorrection)]
        [JsonProperty(Names.PitchCorrection)]
        public float PitchCorrection { get; set; }

        /// <summary> Whether seeking plays a short audio scrub, so scrubbing the timeline is audible
        /// rather than silent. </summary>
        [JsonProperty(Names.Scrub)]
        public bool UseScrub { get; set; }

        /// <summary> Length of that scrub burst, in seconds. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.ScrubTime)]
        public float ScrubTime { get; set; }

        public AudioGraphicsSettings()
        {
            Render = true;
            RenderEffects = true;
            ResyncJumpTime = AudioRules.ResyncJumpTimeDefault;
            SyncDeadZone = AudioRules.SyncDeadZoneDefault;
            PitchCorrection = AudioRules.PitchCorrectionDefault;
            UseScrub = true;
            ScrubTime = 0.1f;
        }
        public AudioGraphicsSettings(bool render, bool renderEffects, float resyncJumpTime,
            float syncDeadZone, float pitchCorrection, bool useScrub, float scrubTime) : base(render)
        {
            RenderEffects = renderEffects;
            ResyncJumpTime = resyncJumpTime;
            SyncDeadZone = syncDeadZone;
            PitchCorrection = pitchCorrection;
            UseScrub = useScrub;
            ScrubTime = scrubTime;
        }
        public override void Reset()
        {
            base.Reset();
            Render = true;
            RenderEffects = true;
            ResyncJumpTime = AudioRules.ResyncJumpTimeDefault;
            SyncDeadZone = AudioRules.SyncDeadZoneDefault;
            PitchCorrection = AudioRules.PitchCorrectionDefault;
            UseScrub = true;
            ScrubTime = 0.1f;
        }

        public override object Clone() => CopyImpl();
        public override BaseGraphicsSettings Copy() => CopyImpl();
        AudioGraphicsSettings ICopyable<AudioGraphicsSettings>.Copy() => CopyImpl();

        private AudioGraphicsSettings CopyImpl() => new(Render, RenderEffects, ResyncJumpTime,
            SyncDeadZone, PitchCorrection, UseScrub, ScrubTime);

        public void Pull(AudioGraphicsSettings source)
        {
            Render = source.Render;
            RenderEffects = source.RenderEffects;
            ResyncJumpTime = source.ResyncJumpTime;
            SyncDeadZone = source.SyncDeadZone;
            PitchCorrection = source.PitchCorrection;
            UseScrub = source.UseScrub;
            ScrubTime = source.ScrubTime;
        }

        public override bool Equals(object obj) => obj is AudioGraphicsSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), RenderEffects,
            ResyncJumpTime, SyncDeadZone, PitchCorrection, UseScrub, ScrubTime);

        public bool Equals(AudioGraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other)
                   && RenderEffects == other.RenderEffects
                   && ResyncJumpTime.Equals(other.ResyncJumpTime)
                   && SyncDeadZone.Equals(other.SyncDeadZone)
                   && PitchCorrection.Equals(other.PitchCorrection)
                   && UseScrub == other.UseScrub
                   && ScrubTime.Equals(other.ScrubTime);
        }
    }
}

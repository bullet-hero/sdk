using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Simulates a space: early reflections followed by a decaying tail. The largest effect here -
    /// its many fields describe a room, where AudioEcho only describes a repeat.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioReverb : AudioEffect, IModel<AudioReverb>
    {
        /// <summary> Level of the untouched signal in the output. </summary>
        [RuleInRange(AudioRules.Reverb.DryLevel_Min, AudioRules.Reverb.DryLevel_Max)]
        [JsonProperty(Names.DryLevel)]
        public float DryLevel { get; set; }

        /// <summary> Overall level of the reverberated signal - the room's presence. </summary>
        [RuleInRange(AudioRules.Reverb.Room_Min, AudioRules.Reverb.Room_Max)]
        [JsonProperty(Names.Room)]
        public float Room { get; set; }

        /// <summary> Room level at high frequencies; cutting it reads as soft, absorbent walls. </summary>
        [RuleInRange(AudioRules.Reverb.RoomHF_Min, AudioRules.Reverb.RoomHF_Max)]
        [JsonProperty(Names.RoomHF)]
        public float RoomHF { get; set; }

        /// <summary> Room level at low frequencies. </summary>
        [RuleInRange(AudioRules.Reverb.RoomLF_Min, AudioRules.Reverb.RoomLF_Max)]
        [JsonProperty(Names.RoomLF)]
        public float RoomLF { get; set; }

        /// <summary> Seconds the tail takes to fade out - the single strongest cue of room size. </summary>
        [RuleInRange(AudioRules.Reverb.DecayTime_Min, AudioRules.Reverb.DecayTime_Max)]
        [JsonProperty(Names.DecayTime)]
        public float DecayTime { get; set; }

        /// <summary> How much faster highs decay than lows; below 1 the tail darkens as it fades. </summary>
        [RuleInRange(AudioRules.Reverb.DecayHFRatio_Min, AudioRules.Reverb.DecayHFRatio_Max)]
        [JsonProperty(Names.DecayHFRatio)]
        public float DecayHFRatio { get; set; }

        /// <summary> Level of the early reflections - the distinct first bounces off nearby walls. </summary>
        [RuleInRange(AudioRules.Reverb.Reflections_Min, AudioRules.Reverb.Reflections_Max)]
        [JsonProperty(Names.Reflections)]
        public float Reflections { get; set; }

        /// <summary> Delay before those reflections arrive - how far the walls are. </summary>
        [RuleInRange(AudioRules.Reverb.ReflectDelay_Min, AudioRules.Reverb.ReflectDelay_Max)]
        [JsonProperty(Names.ReflectDelay)]
        public float ReflectDelay { get; set; }

        /// <summary> Level of the late, diffuse tail, as opposed to the early reflections. </summary>
        [RuleInRange(AudioRules.Reverb.Reverb_Min, AudioRules.Reverb.Reverb_Max)]
        [JsonProperty(Names.Reverb)]
        public float Reverb { get; set; }

        /// <summary> Delay before that tail sets in. </summary>
        [RuleInRange(AudioRules.Reverb.ReverbDelay_Min, AudioRules.Reverb.ReverbDelay_Max)]
        [JsonProperty(Names.ReverbDelay)]
        public float ReverbDelay { get; set; }

        /// <summary> Echo density over time - low values make the tail grainy and metallic. </summary>
        [RuleInRange(AudioRules.Reverb.Diffusion_Min, AudioRules.Reverb.Diffusion_Max)]
        [JsonProperty(Names.Diffusion)]
        public float Diffusion { get; set; }

        /// <summary> Modal density of the simulated space. </summary>
        [RuleInRange(AudioRules.Reverb.Density_Min, AudioRules.Reverb.Density_Max)]
        [JsonProperty(Names.Density)]
        public float Density { get; set; }

        /// <summary> Frequency the RoomHF/DecayHFRatio settings are measured at. </summary>
        [RuleInRange(AudioRules.Reverb.HFReference_Min, AudioRules.Reverb.HFReference_Max)]
        [JsonProperty(Names.HFRef)]
        public float HFReference { get; set; }

        /// <summary> Frequency the RoomLF setting is measured at. </summary>
        [RuleInRange(AudioRules.Reverb.LFReference_Min, AudioRules.Reverb.LFReference_Max)]
        [JsonProperty(Names.LFRef)]
        public float LFReference { get; set; }

        public AudioReverb()
        {
            DryLevel = AudioRules.Reverb.DryLevel_Default;
            Room = AudioRules.Reverb.Room_Default;
            RoomHF = AudioRules.Reverb.RoomHF_Default;
            RoomLF = AudioRules.Reverb.RoomLF_Default;
            DecayTime = AudioRules.Reverb.DecayTime_Default;
            DecayHFRatio = AudioRules.Reverb.DecayHFRatio_Default;
            Reflections = AudioRules.Reverb.Reflections_Default;
            ReflectDelay = AudioRules.Reverb.ReflectDelay_Default;
            Reverb = AudioRules.Reverb.Reverb_Default;
            ReverbDelay = AudioRules.Reverb.ReverbDelay_Default;
            Diffusion = AudioRules.Reverb.Diffusion_Default;
            Density = AudioRules.Reverb.Density_Default;
            HFReference = AudioRules.Reverb.HFReference_Default;
            LFReference = AudioRules.Reverb.LFReference_Default; 
        }
        public AudioReverb(float mixLevel, float dryLevel, float room, float roomHF, float roomLF, 
            float decayTime, float decayHFRatio, float reflections, float reflectDelay, float reverb,
            float reverbDelay, float diffusion, float density, float hfReference, float lfReference)
            : base(mixLevel)
        {
            DryLevel = dryLevel;
            Room = room;
            RoomHF = roomHF;
            RoomLF = roomLF;
            DecayTime = decayTime;
            DecayHFRatio = decayHFRatio;
            Reflections = reflections;
            ReflectDelay = reflectDelay;
            Reverb = reverb;
            ReverbDelay = reverbDelay;
            Diffusion = diffusion;
            Density = density;
            HFReference = hfReference;
            LFReference = lfReference;
        }
    }
}
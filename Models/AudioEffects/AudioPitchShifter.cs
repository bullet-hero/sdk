using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Transposes the track without changing its speed - the whole point, since a level's timing is
    /// locked to the song and cannot be resampled.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioPitchShifter : AudioEffect, IModel<AudioPitchShifter>
    {
        /// <summary> Pitch multiplier; 1 is unchanged, 2 an octave up. </summary>
        [RuleInRange(AudioRules.PitchShifter.Pitch_Min, AudioRules.PitchShifter.Pitch_Max)]
        [JsonProperty(Names.Pitch)]
        public float Pitch { get; set; }

        /// <summary> Analysis window size - the quality/latency dial of the algorithm. </summary>
        [RuleInRange(AudioRules.PitchShifter.FFTSize_Min, AudioRules.PitchShifter.FFTSize_Max)]
        [JsonProperty(Names.FFTSize)]
        public float FFTSize { get; set; }

        /// <summary> How much consecutive windows overlap; more overlap smooths artifacts at a cost. </summary>
        [RuleInRange(AudioRules.PitchShifter.Overlap_Min, AudioRules.PitchShifter.Overlap_Max)]
        [JsonProperty(Names.Overlap)]
        public float Overlap { get; set; }

        /// <summary> Channel cap for the processing. </summary>
        [RuleInRange(AudioRules.PitchShifter.MaxChannels_Min, AudioRules.PitchShifter.MaxChannels_Max)]
        [JsonProperty(Names.MaxChannels)]
        public float MaxChannels { get; set; }

        public AudioPitchShifter()
        {
            Pitch = AudioRules.PitchShifter.Pitch_Default;
            FFTSize = AudioRules.PitchShifter.FFTSize_Default;
            Overlap = AudioRules.PitchShifter.Overlap_Default;
            MaxChannels = AudioRules.PitchShifter.MaxChannels_Default;
        }
        public AudioPitchShifter(float mixLevel, float pitch, float fftSize,
            float overlap, float maxChannels) : base(mixLevel)
        {
            Pitch = pitch;
            FFTSize = fftSize;
            Overlap = overlap;
            MaxChannels = maxChannels;
        }
    }
}
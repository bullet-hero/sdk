using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.AudioEffects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Audio
{
    /// <summary>
    /// The mixing chain of one LevelTrack: two animated tracks plus one slot per DSP effect.
    /// Flat by design - a fixed slot each, never a list to add to or flags to keep in sync.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LevelTrackEffects : IModel<LevelTrackEffects>
    {
        /// <summary> Volume automation over the level timeline - the one place a track fades. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxAudioKeys)]
        [RuleCollectionUnique(nameof(FloatKey.Frame))]
        [JsonProperty(Names.Volume)]
        public List<FloatKey> Volumes { get; set; }

        /// <summary> Left/right placement automation. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxAudioKeys)]
        [RuleCollectionUnique(nameof(FloatKey.Frame))]
        [JsonProperty(Names.StereoPan)]
        public List<FloatKey> StereoPans { get; set; }

        // TODO add Inverse, play track in reverse

        /// <summary> Master switch for this whole DSP chain, off by default - the opposite default
        /// from PostProcessingEvents.Active, and the only explicit on/off flag here. </summary>
        [JsonProperty(Names.ActiveShort)]
        public bool Active { get; set; }

        // TODO replace float to IFloat

        /// <summary> Cuts highs above a cutoff - the muffling half of the filter pair. </summary>
        [RuleOptional]
        [JsonProperty(Names.Lowpass)]
        public AudioLowpass Lowpass { get; set; }

        /// <summary> Cuts lows below a cutoff - thins the track out, the mirror of Lowpass. </summary>
        [RuleOptional]
        [JsonProperty(Names.Highpass)]
        public AudioHighpass Highpass { get; set; }

        /// <summary> Discrete repeats of the signal. </summary>
        [RuleOptional]
        [JsonProperty(Names.Echo)]
        public AudioEcho Echo { get; set; }

        /// <summary> Simulated room tail - dense and diffuse, where Echo is countable repeats. </summary>
        [RuleOptional]
        [JsonProperty(Names.Reverb)]
        public AudioReverb Reverb { get; set; }

        /// <summary> Detuned copies layered in to thicken the sound. </summary>
        [RuleOptional]
        [JsonProperty(Names.Chorus)]
        public AudioChorus Chorus { get; set; }

        /// <summary> Changes pitch without changing playback speed - unlike a plain rate change. </summary>
        [RuleOptional]
        [JsonProperty(Names.PitchShifter)]
        public AudioPitchShifter PitchShifter { get; set; }

        /// <summary> Clipping/saturation for a dirty sound. </summary>
        [RuleOptional]
        [JsonProperty(Names.Distortion)]
        public AudioDistortion Distortion { get; set; }

        /// <summary> Sweeping comb filter - the whoosh, closely related to Chorus but modulated
        /// through a shorter delay. </summary>
        [RuleOptional]
        [JsonProperty(Names.Flange)]
        public AudioFlange Flange { get; set; }

        /// <summary> Reduces dynamic range above a threshold. </summary>
        [RuleOptional]
        [JsonProperty(Names.Compressor)]
        public AudioCompressor Compressor { get; set; }

        /// <summary> Brings the overall level to a target - loudness, where Compressor shapes
        /// dynamics. </summary>
        [RuleOptional]
        [JsonProperty(Names.Normalize)]
        public AudioNormalize Normalize { get; set; }
        
        /// <summary> Single-band parametric EQ - boosts or cuts around one frequency, where
        /// Low/Highpass can only cut past one. </summary>
        [RuleOptional]
        [JsonProperty(Names.ParamEQ)]
        public AudioParamEQ ParamEQ { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public LevelTrackEffects()
        {
            Volumes = new List<FloatKey>();
            StereoPans = new List<FloatKey>();
            Active = AudioRules.ActiveDefault;
            
            // THE ELEVEN SLOTS ARE BORN EMPTY, and null is what "this effect is not in the
            // chain" means. It used to be an always-present object at the disabled floor, which
            // wrote 1.1 KB of pure defaults into every track of every level - eleven effects with
            // every parameter, whether or not one was in use. A member that may be null must be
            // CONSTRUCTED null (docs/NAMING.md), which is what makes the writer's skip reach it.
            //
            // MixLevel stays exactly what it was: a real wet level whose floor means silence. It
            // answers "how loud", null answers "is it here at all", and the two are not the same
            // question - an effect an author dialled in and then switched off keeps its object and
            // its settings.
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public LevelTrackEffects(List<FloatKey> volumes, List<FloatKey> stereoPans, bool active, 
            AudioLowpass lowpass, AudioHighpass highpass, AudioEcho echo, AudioReverb reverb, 
            AudioChorus chorus, AudioPitchShifter pitchShifter, AudioDistortion distortion, 
            AudioFlange flange, AudioCompressor compressor, AudioNormalize normalize, AudioParamEQ paramEQ)
        {
            Volumes = volumes;
            StereoPans = stereoPans;
            Active = active;
            
            Lowpass = lowpass;
            Highpass = highpass;
            Echo = echo;
            Reverb = reverb;
            Chorus = chorus;
            PitchShifter = pitchShifter;
            Distortion = distortion;
            Flange = flange;
            Compressor = compressor;
            Normalize = normalize;
            ParamEQ = paramEQ;
        }
    }
}
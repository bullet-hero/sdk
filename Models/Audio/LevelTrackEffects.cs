using System;
using System.Collections.Generic;
using BH.SDK.Models.AudioEffects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Audio
{
    /// <summary>
    /// The mixing chain of one LevelTrack: two animated tracks plus a fixed slot per DSP effect.
    /// Flat by design - every effect object always exists, and whether it does anything is decided
    /// by its own MixLevel, so there is no list to add to or flags to keep in sync.
    /// </summary>
    [RuleContainer]
    public class LevelTrackEffects : IModel<LevelTrackEffects>
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
        [JsonProperty(Names.Active)]
        public bool Active { get; set; }

        // TODO replace float to IFloat

        /// <summary> Cuts highs above a cutoff - the muffling half of the filter pair. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Lowpass)]
        public AudioLowpass Lowpass { get; set; }

        /// <summary> Cuts lows below a cutoff - thins the track out, the mirror of Lowpass. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Highpass)]
        public AudioHighpass Highpass { get; set; }

        /// <summary> Discrete repeats of the signal. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Echo)]
        public AudioEcho Echo { get; set; }

        /// <summary> Simulated room tail - dense and diffuse, where Echo is countable repeats. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Reverb)]
        public AudioReverb Reverb { get; set; }

        /// <summary> Detuned copies layered in to thicken the sound. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Chorus)]
        public AudioChorus Chorus { get; set; }

        /// <summary> Changes pitch without changing playback speed - unlike a plain rate change. </summary>
        [RuleNotNull]
        [JsonProperty(Names.PitchShifter)]
        public AudioPitchShifter PitchShifter { get; set; }

        /// <summary> Clipping/saturation for a dirty sound. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Distortion)]
        public AudioDistortion Distortion { get; set; }

        /// <summary> Sweeping comb filter - the whoosh, closely related to Chorus but modulated
        /// through a shorter delay. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Flange)]
        public AudioFlange Flange { get; set; }

        /// <summary> Reduces dynamic range above a threshold. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Compressor)]
        public AudioCompressor Compressor { get; set; }

        /// <summary> Brings the overall level to a target - loudness, where Compressor shapes
        /// dynamics. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Normalize)]
        public AudioNormalize Normalize { get; set; }
        
        /// <summary> Single-band parametric EQ - boosts or cuts around one frequency, where
        /// Low/Highpass can only cut past one. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ParamEQ)]
        public AudioParamEQ ParamEQ { get; set; }

        public LevelTrackEffects()
        {
            Volumes = new List<FloatKey>();
            StereoPans = new List<FloatKey>();
            Active = AudioRules.ActiveDefault;
            
            Lowpass = new AudioLowpass();
            Highpass = new AudioHighpass();
            Echo = new AudioEcho();
            Reverb = new AudioReverb();
            Chorus = new AudioChorus();
            PitchShifter = new AudioPitchShifter();
            Distortion = new AudioDistortion();
            Flange = new AudioFlange();
            Compressor = new AudioCompressor();
            Normalize = new AudioNormalize();
            ParamEQ = new AudioParamEQ();
        }
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
        public void Reset()
        {
            Volumes.Clear();
            StereoPans.Clear();
            Active = AudioRules.ActiveDefault;
            
            Lowpass.Reset();
            Highpass.Reset();
            Echo.Reset();
            Reverb.Reset();
            Chorus.Reset();
            PitchShifter.Reset();
            Distortion.Reset();
            Flange.Reset();
            Compressor.Reset();
            Normalize.Reset();
            ParamEQ.Reset();
        }

        public object Clone() => Copy();
        public LevelTrackEffects Copy() => new(Volumes.CopyList(), StereoPans.CopyList(), Active,
            (AudioLowpass)Lowpass.Clone(), (AudioHighpass)Highpass.Clone(), (AudioEcho)Echo.Clone(),
            (AudioReverb)Reverb.Clone(), (AudioChorus)Chorus.Clone(), (AudioPitchShifter)PitchShifter.Clone(),
            (AudioDistortion)Distortion.Clone(), (AudioFlange)Flange.Clone(), (AudioCompressor)Compressor.Clone(),
            (AudioNormalize)Normalize.Clone(), (AudioParamEQ)ParamEQ.Clone());

        public void Update(LevelTrackEffects src)
        {
            Volumes = src.Volumes.CopyList();
            StereoPans = src.StereoPans.CopyList();
            Active = src.Active;
            Lowpass = (AudioLowpass)src.Lowpass.Clone();
            Highpass = (AudioHighpass)src.Highpass.Clone();
            Echo = (AudioEcho)src.Echo.Clone();
            Reverb = (AudioReverb)src.Reverb.Clone();
            Chorus = (AudioChorus)src.Chorus.Clone();
            PitchShifter = (AudioPitchShifter)src.PitchShifter.Clone();
            Distortion = (AudioDistortion)src.Distortion.Clone();
            Flange = (AudioFlange)src.Flange.Clone();
            Compressor = (AudioCompressor)src.Compressor.Clone();
            Normalize = (AudioNormalize)src.Normalize.Clone();
            ParamEQ = (AudioParamEQ)src.ParamEQ.Clone();
        }

        public void Pull(LevelTrackEffects src)
        {
            Volumes = src.Volumes.CopyList();
            StereoPans = src.StereoPans.CopyList();
            Active = src.Active;
            Lowpass.Pull(src.Lowpass);
            Highpass.Pull(src.Highpass);
            Echo.Pull(src.Echo);
            Reverb.Pull(src.Reverb);
            Chorus.Pull(src.Chorus);
            PitchShifter.Pull(src.PitchShifter);
            Distortion.Pull(src.Distortion);
            Flange.Pull(src.Flange);
            Compressor.Pull(src.Compressor);
            Normalize.Pull(src.Normalize);
            ParamEQ.Pull(src.ParamEQ);
        }

        public override bool Equals(object obj) => obj is LevelTrackEffects value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Volumes.GetListHashCode());
            hashCode.Add(StereoPans.GetListHashCode());
            hashCode.Add(Active);
            hashCode.Add(Lowpass);
            hashCode.Add(Highpass);
            hashCode.Add(Echo);
            hashCode.Add(Reverb);
            hashCode.Add(Chorus);
            hashCode.Add(PitchShifter);
            hashCode.Add(Distortion);
            hashCode.Add(Flange);
            hashCode.Add(Compressor);
            hashCode.Add(Normalize);
            hashCode.Add(ParamEQ);
            return hashCode.ToHashCode();
        }

        public bool Equals(LevelTrackEffects other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Volumes.ListEquals(other.Volumes)
                         && StereoPans.ListEquals(other.StereoPans)
                         && Active == other.Active
                         && Lowpass.Equals(other.Lowpass)
                         && Highpass.Equals(other.Highpass)
                         && Echo.Equals(other.Echo)
                         && Reverb.Equals(other.Reverb)
                         && Chorus.Equals(other.Chorus)
                         && PitchShifter.Equals(other.PitchShifter)
                         && Distortion.Equals(other.Distortion)
                         && Flange.Equals(other.Flange)
                         && Compressor.Equals(other.Compressor)
                         && Normalize.Equals(other.Normalize)
                         && ParamEQ.Equals(other.ParamEQ);
            return result;
        }
    }
}
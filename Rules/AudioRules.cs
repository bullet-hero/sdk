// ReSharper disable InconsistentNaming
namespace BH.SDK.Rules
{
    /// <summary> Every bound a level's audio is authored within - track count, layers, offsets, speed, and one
    /// nested class per audio effect. </summary>
    public static class AudioRules
    {
        // public static readonly float Core_PitchDefault = 1f; // 0.01f - 10f, 0.01f, %

        /// <summary> Highest frame count allowed. </summary>
        public const int MaxFrameCount = 16;
        /// <summary> Lower bound of LevelTrack.AudioLayer. </summary>
        public const int MinAudioLayer = 0;
        /// <summary> Upper bound of LevelTrack.AudioLayer. </summary>
        public const int MaxAudioLayer = MaxFrameCount - 1;
        
        // How far a track's audio is shifted against the timeline, in seconds. Bounded both ways at
        // an hour: the value is a float with no other constraint, so without this a file can hold
        // NaN or 1e30 and every time-to-frame conversion downstream inherits it.

        /// <summary> Lower bound of LevelTrack.OffsetTime. </summary>
        public const float MinOffsetTime = -3600f;
        /// <summary> Upper bound of LevelTrack.OffsetTime. </summary>
        public const float MaxOffsetTime = 3600f;
        /// <summary> The offset time used when nothing says otherwise, read by LevelTrack. </summary>
        public const float OffsetTimeDefault = 0f;

        // Same numbers as FrameRules.MinSpeed/MaxSpeed, different thing entirely: this is one
        // track's own rate, that one is the whole level's playback rate. The two multiply, so the
        // real pitch a source ends up with reaches +-4 - which Unity allows, its own +-3 limit is
        // an inspector slider, not an API bound.

        // -2f - 2f, 0.01f, x

        /// <summary> Lower bound of LevelTrack.Speed. </summary>
        public const float MinSpeed = -2f;
        /// <summary> Upper bound of LevelTrack.Speed. </summary>
        public const float MaxSpeed = 2f;
        /// <summary> The speed used when nothing says otherwise, read by AudioFileLevelGenerator, LevelTrack, LevelTrackTests. </summary>
        public const float SpeedDefault = 1f;

        // Bounds for the playback-sync settings on AudioGraphicsSettings. They are what the settings
        // screen offers and what validation checks; nothing clamps at runtime, exactly like every
        // other UserSettings value - a hand-edited file is the player's own doing.

        // How far the playhead must jump before it counts as a discontinuity rather than drift.
        // The floor is one audio buffer: below that it would fire on AudioSettings.dspTime's own
        // quantization alone and re-seek constantly, which is audible as a stutter. The ceiling is
        // where a scrub starts feeling unresponsive.

        // 0.02f - 0.5f, 0.01f, s

        /// <summary> Lower bound of AudioGraphicsSettings.ResyncJumpTime. </summary>
        public const float MinResyncJumpTime = 0.02f;
        /// <summary> Upper bound of AudioGraphicsSettings.ResyncJumpTime. </summary>
        public const float MaxResyncJumpTime = 0.5f;
        /// <summary> The resync jump time used when nothing says otherwise, read by AudioGraphicsSettings. </summary>
        public const float ResyncJumpTimeDefault = 0.05f;

        // Drift below which the playback rate is left at exactly 1. Not a precision knob: any rate
        // other than 1 puts the engine's resampler in the signal path, so this is "how much desync
        // is worth that cost". Too small and the resampler is simply always on.

        // 0.005f - 0.1f, 0.005f, s

        /// <summary> Lower bound of AudioGraphicsSettings.SyncDeadZone. </summary>
        public const float MinSyncDeadZone = 0.005f;
        /// <summary> Upper bound of AudioGraphicsSettings.SyncDeadZone. </summary>
        public const float MaxSyncDeadZone = 0.1f;
        /// <summary> The sync dead zone used when nothing says otherwise, read by AudioGraphicsSettings. </summary>
        public const float SyncDeadZoneDefault = 0.02f;

        // How far the rate correction may bend playback. 0 turns it off entirely, leaving the hard
        // resync as the only correction - a legitimate choice if the resampler is more objectionable
        // than the occasional jump. The ceiling is ~0.85 of a semitone, past which it stops reading
        // as a correction and starts reading as the music being wrong.

        // 0f - 0.05f, 0.005f, x

        /// <summary> Lower bound of AudioGraphicsSettings.PitchCorrection. </summary>
        public const float MinPitchCorrection = 0f;
        /// <summary> Upper bound of AudioGraphicsSettings.PitchCorrection. </summary>
        public const float MaxPitchCorrection = 0.05f;
        /// <summary> The pitch correction used when nothing says otherwise, read by AudioGraphicsSettings. </summary>
        public const float PitchCorrectionDefault = 0.02f;

        // -80f - 0f, 0.1f, dB;

        /// <summary> Bounds AudioEffect.MixLevel. </summary>
        public const float MixLevel_Enabled = 0f;
        /// <summary> Bounds AudioEffect.MixLevel. </summary>
        public const float MixLevel_Disabled = -80f;
        /// <summary> The mix level used when nothing says otherwise, read by AudioEffect. </summary>
        public const float MixLevel_Default = MixLevel_Disabled;

        /// <summary> True when a mix level actually mixes anything - the floor is silence, not a quiet setting. </summary>
        public static bool IsActiveMixLevel(float mixLevel) => mixLevel > MixLevel_Disabled;

        // The two keyframed per-track values. Their bounds lived only in the comment beside each
        // default until now, so a Volume of 40 or a StereoPan of -12 was legal authored data the
        // mixer then silently saturated. They are NOT reachable by a [RuleInRange]: both are stored
        // in the shared FloatKey, which every other float track uses too, so an attribute there
        // would bound all of them at once. The editor clamps against these instead.

        /// <summary> The volume used when nothing says otherwise, read by AudioFileLevelGenerator, LevelTrack, LevelTrackTests and 1 more. </summary>
        public const float VolumeDefault = 1f; // 0f - 1f, 0.01f
        /// <summary> Lower bound of LevelTrack.Volume. </summary>
        public const float MinVolume = 0f;
        /// <summary> Upper bound of LevelTrack.Volume. </summary>
        public const float MaxVolume = 1f;

        /// <summary> The stereo pan used when nothing says otherwise. </summary>
        public const float StereoPanDefault = 0f; // -1f - 1f, 0.01f
        /// <summary> Lowest stereo pan allowed. </summary>
        public const float MinStereoPan = -1f;
        /// <summary> Highest stereo pan allowed. </summary>
        public const float MaxStereoPan = 1f;
        /// <summary> The default for active, read by LevelTrackEffects. </summary>
        public const bool ActiveDefault = false;
        
        /// <summary> Bounds of the low-pass filter. </summary>
        public static class Lowpass
        {
            // 10f - 22000f, 1f, Hz

            /// <summary> Lower bound of AudioLowpass.CutoffFreq. </summary>
            public const float CutoffFreq_Min = 10f;
            /// <summary> Upper bound of AudioLowpass.CutoffFreq. </summary>
            public const float CutoffFreq_Max = 22000f;
            /// <summary> The cutoff freq used when nothing says otherwise, read by AudioLowpass. </summary>
            public const float CutoffFreq_Default = 5000f;
        }

        /// <summary> Bounds of the high-pass filter. </summary>
        public static class Highpass
        {
            // 10f - 22000f, 1f, Hz

            /// <summary> Lower bound of AudioHighpass.CutoffFreq. </summary>
            public const float CutoffFreq_Min = 10f;
            /// <summary> Upper bound of AudioHighpass.CutoffFreq. </summary>
            public const float CutoffFreq_Max = 22000f;
            /// <summary> The cutoff freq used when nothing says otherwise, read by AudioHighpass. </summary>
            public const float CutoffFreq_Default = 1000f;
        }

        /// <summary> Bounds of the echo effect. </summary>
        public static class Echo
        {
            // 1f - 5000f, 1f, ms

            /// <summary> Lower bound of AudioEcho.Delay. </summary>
            public const float Delay_Min = 1f;
            /// <summary> Upper bound of AudioEcho.Delay. </summary>
            public const float Delay_Max = 5000f;
            /// <summary> The delay used when nothing says otherwise, read by AudioEcho. </summary>
            public const float Delay_Default = 100f;
            
            // 0f - 1f, 0.01f, %

            /// <summary> Lower bound of AudioEcho.Decay. </summary>
            public const float Decay_Min = 0f;
            /// <summary> Upper bound of AudioEcho.Decay. </summary>
            public const float Decay_Max = 1f;
            /// <summary> The decay used when nothing says otherwise, read by AudioEcho. </summary>
            public const float Decay_Default = 0.8f;
            
            // 0f - 16f, 0.01f, ch

            /// <summary> Lower bound of AudioEcho.MaxChannels. </summary>
            public const float MaxChannels_Min = 0f;
            /// <summary> Upper bound of AudioEcho.MaxChannels. </summary>
            public const float MaxChannels_Max = 16f;
            /// <summary> Highest channels allowed, read by AudioEcho. </summary>
            public const float MaxChannels_Default = 0f;
            
            // 0f - 1f, 0.01f, %

            /// <summary> Lower bound of AudioEcho.DryMix. </summary>
            public const float DryMix_Min = 0f;
            /// <summary> Upper bound of AudioEcho.DryMix. </summary>
            public const float DryMix_Max = 1f;
            /// <summary> The dry mix used when nothing says otherwise, read by AudioEcho. </summary>
            public const float DryMix_Default = 1f;
            
            // 0f - 1f, 0.01f, %

            /// <summary> Lower bound of AudioEcho.WetMix. </summary>
            public const float WetMix_Min = 0f;
            /// <summary> Upper bound of AudioEcho.WetMix. </summary>
            public const float WetMix_Max = 1f;
            /// <summary> The wet mix used when nothing says otherwise, read by AudioEcho. </summary>
            public const float WetMix_Default = 1f;
        }

        /// <summary> Bounds of the reverb effect. </summary>
        public static class Reverb
        {
            // -10000f - 0f, 1f, mB

            /// <summary> Lower bound of AudioReverb.DryLevel. </summary>
            public const float DryLevel_Min = -10000f;
            /// <summary> Upper bound of AudioReverb.DryLevel. </summary>
            public const float DryLevel_Max = 0f;
            /// <summary> The dry level used when nothing says otherwise, read by AudioReverb. </summary>
            public const float DryLevel_Default = 0f;
            
            // -10000f - 0f, 1f, mB

            /// <summary> Lower bound of AudioReverb.Room. </summary>
            public const float Room_Min = -10000f;
            /// <summary> Upper bound of AudioReverb.Room. </summary>
            public const float Room_Max = 0f;
            /// <summary> The room used when nothing says otherwise, read by AudioReverb. </summary>
            public const float Room_Default = -10000f;
            
            // -10000f - 0f, 1f, mB

            /// <summary> Lower bound of AudioReverb.RoomHF. </summary>
            public const float RoomHF_Min = -10000f;
            /// <summary> Upper bound of AudioReverb.RoomHF. </summary>
            public const float RoomHF_Max = 0f;
            /// <summary> The room HF used when nothing says otherwise, read by AudioReverb. </summary>
            public const float RoomHF_Default = 0f;
            
            // -10000f - 0f, 1f, mB

            /// <summary> Lower bound of AudioReverb.RoomLF. </summary>
            public const float RoomLF_Min = -10000f;
            /// <summary> Upper bound of AudioReverb.RoomLF. </summary>
            public const float RoomLF_Max = 0f;
            /// <summary> The room LF used when nothing says otherwise, read by AudioReverb. </summary>
            public const float RoomLF_Default = 0f;
            
            // 0.1f - 20f, 0.1f, s

            /// <summary> Lower bound of AudioReverb.DecayTime. </summary>
            public const float DecayTime_Min = 0.1f;
            /// <summary> Upper bound of AudioReverb.DecayTime. </summary>
            public const float DecayTime_Max = 20f;
            /// <summary> The decay time used when nothing says otherwise, read by AudioReverb. </summary>
            public const float DecayTime_Default = 1f;
            
            // 0.1f - 2f, 0.01f

            /// <summary> Lower bound of AudioReverb.DecayHFRatio. </summary>
            public const float DecayHFRatio_Min = 0.1f;
            /// <summary> Upper bound of AudioReverb.DecayHFRatio. </summary>
            public const float DecayHFRatio_Max = 2f;
            /// <summary> The decay HF ratio used when nothing says otherwise, read by AudioReverb. </summary>
            public const float DecayHFRatio_Default = 0.5f;
            
            // -10000f - 1000f, 1f, mB

            /// <summary> Lower bound of AudioReverb.Reflections. </summary>
            public const float Reflections_Min = -10000f;
            /// <summary> Upper bound of AudioReverb.Reflections. </summary>
            public const float Reflections_Max = 1000f;
            /// <summary> The reflections used when nothing says otherwise, read by AudioReverb. </summary>
            public const float Reflections_Default = -10000f;
            
            // 0f - 0.3f, 0.01f, s

            /// <summary> Lower bound of AudioReverb.ReflectDelay. </summary>
            public const float ReflectDelay_Min = 0f;
            /// <summary> Upper bound of AudioReverb.ReflectDelay. </summary>
            public const float ReflectDelay_Max = 0.3f;
            /// <summary> The reflect delay used when nothing says otherwise, read by AudioReverb. </summary>
            public const float ReflectDelay_Default = 0.02f;
            
            // -10000f - 2000f, 1f, mB

            /// <summary> Lower bound of AudioReverb.Reverb. </summary>
            public const float Reverb_Min = -10000f;
            /// <summary> Upper bound of AudioReverb.Reverb. </summary>
            public const float Reverb_Max = 2000f;
            /// <summary> The reverb used when nothing says otherwise, read by AudioReverb. </summary>
            public const float Reverb_Default = 0f;
            
            // 0f - 0.1f, 0.01f, s

            /// <summary> Lower bound of AudioReverb.ReverbDelay. </summary>
            public const float ReverbDelay_Min = 0f;
            /// <summary> Upper bound of AudioReverb.ReverbDelay. </summary>
            public const float ReverbDelay_Max = 0.1f;
            /// <summary> The reverb delay used when nothing says otherwise, read by AudioReverb. </summary>
            public const float ReverbDelay_Default = 0.04f;
            
            // 0f - 1f, 0.01f, %

            /// <summary> Lower bound of AudioReverb.Diffusion. </summary>
            public const float Diffusion_Min = 0f;
            /// <summary> Upper bound of AudioReverb.Diffusion. </summary>
            public const float Diffusion_Max = 1f;
            /// <summary> The diffusion used when nothing says otherwise, read by AudioReverb. </summary>
            public const float Diffusion_Default = 1f;
            
            // 0f - 1f, 0.01f, %

            /// <summary> Lower bound of AudioReverb.Density. </summary>
            public const float Density_Min = 0f;
            /// <summary> Upper bound of AudioReverb.Density. </summary>
            public const float Density_Max = 1f;
            /// <summary> The density used when nothing says otherwise, read by AudioReverb. </summary>
            public const float Density_Default = 1f;
            
            // 20f - 20000f, 1f, Hz

            /// <summary> Lower bound of AudioReverb.HFReference. </summary>
            public const float HFReference_Min = 20f;
            /// <summary> Upper bound of AudioReverb.HFReference. </summary>
            public const float HFReference_Max = 20000f;
            /// <summary> The HF reference used when nothing says otherwise, read by AudioReverb. </summary>
            public const float HFReference_Default = 5000f;
            
            // 20f - 1000f, 1f, Hz

            /// <summary> Lower bound of AudioReverb.LFReference. </summary>
            public const float LFReference_Min = 20f;
            /// <summary> Upper bound of AudioReverb.LFReference. </summary>
            public const float LFReference_Max = 1000f;
            /// <summary> The LF reference used when nothing says otherwise, read by AudioReverb. </summary>
            public const float LFReference_Default = 250f;
        }

        /// <summary> Bounds of the chorus effect. </summary>
        public static class Chorus
        {
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioChorus.DryMix. </summary>
            public const float DryMix_Min = 0f;
            /// <summary> Upper bound of AudioChorus.DryMix. </summary>
            public const float DryMix_Max = 1f;
            /// <summary> The dry mix used when nothing says otherwise, read by AudioChorus. </summary>
            public const float DryMix_Default = 0.5f;
            
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioChorus.WetMixTap1. </summary>
            public const float WetMixTap1_Min = 0f;
            /// <summary> Upper bound of AudioChorus.WetMixTap1. </summary>
            public const float WetMixTap1_Max = 1f;
            /// <summary> The wet mix tap1 used when nothing says otherwise, read by AudioChorus. </summary>
            public const float WetMixTap1_Default = 0.5f;
            
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioChorus.WetMixTap2. </summary>
            public const float WetMixTap2_Min = 0f;
            /// <summary> Upper bound of AudioChorus.WetMixTap2. </summary>
            public const float WetMixTap2_Max = 1f;
            /// <summary> The wet mix tap2 used when nothing says otherwise, read by AudioChorus. </summary>
            public const float WetMixTap2_Default = 0.5f;
            
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioChorus.WetMixTap3. </summary>
            public const float WetMixTap3_Min = 0f;
            /// <summary> Upper bound of AudioChorus.WetMixTap3. </summary>
            public const float WetMixTap3_Max = 1f;
            /// <summary> The wet mix tap3 used when nothing says otherwise, read by AudioChorus. </summary>
            public const float WetMixTap3_Default = 0.5f;
            
            // 0f - 100f, 0.1f, ms

            /// <summary> Lower bound of AudioChorus.Delay. </summary>
            public const float Delay_Min = 0f;
            /// <summary> Upper bound of AudioChorus.Delay. </summary>
            public const float Delay_Max = 100f;
            /// <summary> The delay used when nothing says otherwise, read by AudioChorus. </summary>
            public const float Delay_Default = 40f;
            
            // 0f - 20f, 0.1f, Hz

            /// <summary> Lower bound of AudioChorus.Rate. </summary>
            public const float Rate_Min = 0f;
            /// <summary> Upper bound of AudioChorus.Rate. </summary>
            public const float Rate_Max = 20f;
            /// <summary> The rate used when nothing says otherwise, read by AudioChorus. </summary>
            public const float Rate_Default = 0.8f;
            
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioChorus.Depth. </summary>
            public const float Depth_Min = 0f;
            /// <summary> Upper bound of AudioChorus.Depth. </summary>
            public const float Depth_Max = 1f;
            /// <summary> The depth used when nothing says otherwise, read by AudioChorus. </summary>
            public const float Depth_Default = 0.03f;
            
            // -1f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioChorus.Feedback. </summary>
            public const float Feedback_Min = -1f;
            /// <summary> Upper bound of AudioChorus.Feedback. </summary>
            public const float Feedback_Max = 1f;
            /// <summary> The feedback used when nothing says otherwise, read by AudioChorus. </summary>
            public const float Feedback_Default = 0f;
        }

        /// <summary> Bounds of the pitch shifter. </summary>
        public static class PitchShifter
        {
            // 0.5f - 2f, 0.01f, x

            /// <summary> Lower bound of AudioPitchShifter.Pitch. </summary>
            public const float Pitch_Min = 0.5f;
            /// <summary> Upper bound of AudioPitchShifter.Pitch. </summary>
            public const float Pitch_Max = 2f;
            /// <summary> The pitch used when nothing says otherwise, read by AudioPitchShifter. </summary>
            public const float Pitch_Default = 1f;
            
            // 256f - 4096f, 1f, -

            /// <summary> Lower bound of AudioPitchShifter.FFTSize. </summary>
            public const float FFTSize_Min = 256f;
            /// <summary> Upper bound of AudioPitchShifter.FFTSize. </summary>
            public const float FFTSize_Max = 4096f;
            /// <summary> The FFT size used when nothing says otherwise, read by AudioPitchShifter. </summary>
            public const float FFTSize_Default = 1024f;
            
            // 1f - 32f, 0.1f, -

            /// <summary> Lower bound of AudioPitchShifter.Overlap. </summary>
            public const float Overlap_Min = 1f;
            /// <summary> Upper bound of AudioPitchShifter.Overlap. </summary>
            public const float Overlap_Max = 32f;
            /// <summary> The overlap used when nothing says otherwise, read by AudioPitchShifter. </summary>
            public const float Overlap_Default = 4f;
            
            // 0f - 16f, 0.01f, ch

            /// <summary> Lower bound of AudioPitchShifter.MaxChannels. </summary>
            public const float MaxChannels_Min = 0f;
            /// <summary> Upper bound of AudioPitchShifter.MaxChannels. </summary>
            public const float MaxChannels_Max = 16f;
            /// <summary> Highest channels allowed, read by AudioPitchShifter. </summary>
            public const float MaxChannels_Default = 0f;
        }

        /// <summary> Bounds of the distortion effect. </summary>
        public static class Distortion
        {
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioDistortion.Level. </summary>
            public const float Level_Min = 0f;
            /// <summary> Upper bound of AudioDistortion.Level. </summary>
            public const float Level_Max = 1f;
            /// <summary> The level used when nothing says otherwise, read by AudioDistortion. </summary>
            public const float Level_Default = 0.5f;
        }

        /// <summary> Bounds of the flanger. </summary>
        public static class Flange
        {
            // 0f - 1f, 0.01f, %

            /// <summary> Lower bound of AudioFlange.DryMix. </summary>
            public const float DryMix_Min = 0f;
            /// <summary> Upper bound of AudioFlange.DryMix. </summary>
            public const float DryMix_Max = 1f;
            /// <summary> The dry mix used when nothing says otherwise, read by AudioFlange. </summary>
            public const float DryMix_Default = 0.45f;
            
            // 0f - 1f, 0.01f, %

            /// <summary> Lower bound of AudioFlange.WetMix. </summary>
            public const float WetMix_Min = 0f;
            /// <summary> Upper bound of AudioFlange.WetMix. </summary>
            public const float WetMix_Max = 1f;
            /// <summary> The wet mix used when nothing says otherwise, read by AudioFlange. </summary>
            public const float WetMix_Default = 0.55f;
            
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioFlange.Depth. </summary>
            public const float Depth_Min = 0f;
            /// <summary> Upper bound of AudioFlange.Depth. </summary>
            public const float Depth_Max = 1f;
            /// <summary> The depth used when nothing says otherwise, read by AudioFlange. </summary>
            public const float Depth_Default = 1f;
            
            // 0f - 20f, 0.1f, Hz

            /// <summary> Lower bound of AudioFlange.Rate. </summary>
            public const float Rate_Min = 0f;
            /// <summary> Upper bound of AudioFlange.Rate. </summary>
            public const float Rate_Max = 20f;
            /// <summary> The rate used when nothing says otherwise, read by AudioFlange. </summary>
            public const float Rate_Default = 0.1f;
        }

        /// <summary> Bounds of the compressor. </summary>
        public static class Compressor
        {
            // -60f - 0f, 0.1f, dB

            /// <summary> Lower bound of AudioCompressor.Threshold. </summary>
            public const float Threshold_Min = -60f;
            /// <summary> Upper bound of AudioCompressor.Threshold. </summary>
            public const float Threshold_Max = 0f;
            /// <summary> The threshold used when nothing says otherwise, read by AudioCompressor. </summary>
            public const float Threshold_Default = 0f;
            
            // 10f - 200f, 1f, ms

            /// <summary> Lower bound of AudioCompressor.Attack. </summary>
            public const float Attack_Min = 10f;
            /// <summary> Upper bound of AudioCompressor.Attack. </summary>
            public const float Attack_Max = 200f;
            /// <summary> The attack used when nothing says otherwise, read by AudioCompressor. </summary>
            public const float Attack_Default = 50f;
            
            // 20f - 1000f, 1f, ms

            /// <summary> Lower bound of AudioCompressor.Release. </summary>
            public const float Release_Min = 20f;
            /// <summary> Upper bound of AudioCompressor.Release. </summary>
            public const float Release_Max = 1000f;
            /// <summary> The release used when nothing says otherwise, read by AudioCompressor. </summary>
            public const float Release_Default = 50f;
            
            // 0f - 30f, 0.1f, dB

            /// <summary> Lower bound of AudioCompressor.MakeUpGain. </summary>
            public const float MakeUpGain_Min = 0f;
            /// <summary> Upper bound of AudioCompressor.MakeUpGain. </summary>
            public const float MakeUpGain_Max = 30f;
            /// <summary> The make up gain used when nothing says otherwise, read by AudioCompressor. </summary>
            public const float MakeUpGain_Default = 0f;
        }

        /// <summary> Bounds of the normalizer. </summary>
        public static class Normalize
        {
            // 0f - 20000f, 1f, ms

            /// <summary> Lower bound of AudioNormalize.FadeInTime. </summary>
            public const float FadeInTime_Min = 0f;
            /// <summary> Upper bound of AudioNormalize.FadeInTime. </summary>
            public const float FadeInTime_Max = 20000f;
            /// <summary> The fade in time used when nothing says otherwise, read by AudioNormalize. </summary>
            public const float FadeInTime_Default = 5000f;
            
            // 0f - 1f, 0.01f, -

            /// <summary> Lower bound of AudioNormalize.LowestVolume. </summary>
            public const float LowestVolume_Min = 0f;
            /// <summary> Upper bound of AudioNormalize.LowestVolume. </summary>
            public const float LowestVolume_Max = 1f;
            /// <summary> The lowest volume used when nothing says otherwise, read by AudioNormalize. </summary>
            public const float LowestVolume_Default = 0.1f;
            
            // 0f - 100000f, 1f, x

            /// <summary> Lower bound of AudioNormalize.MaxAmp. </summary>
            public const float MaximumAmp_Min = 0f;
            /// <summary> Upper bound of AudioNormalize.MaxAmp. </summary>
            public const float MaximumAmp_Max = 100000f;
            /// <summary> Highest amp allowed, read by AudioNormalize. </summary>
            public const float MaximumAmp_Default = 20f;
        }

        /// <summary> Bounds of the parametric equalizer. </summary>
        public static class ParamEQ
        {
            // 20f - 22000f, 1f, Hz

            /// <summary> Lower bound of AudioParamEQ.CenterFreq. </summary>
            public const float CenterFreq_Min = 20f;
            /// <summary> Upper bound of AudioParamEQ.CenterFreq. </summary>
            public const float CenterFreq_Max = 22000f;
            /// <summary> The center freq used when nothing says otherwise, read by AudioParamEQ. </summary>
            public const float CenterFreq_Default = 5000f;
            
            // 0.2f - 5f, 0.01f, oct

            /// <summary> Lower bound of AudioParamEQ.OctaveRange. </summary>
            public const float OctaveRange_Min = 0.2f;
            /// <summary> Upper bound of AudioParamEQ.OctaveRange. </summary>
            public const float OctaveRange_Max = 5f;
            /// <summary> The octave range used when nothing says otherwise, read by AudioParamEQ. </summary>
            public const float OctaveRange_Default = 1f;
            
            // 0.05f - 3f, 0.01f, -

            /// <summary> Lower bound of AudioParamEQ.FrequencyGain. </summary>
            public const float FrequencyGain_Min = 0.05f;
            /// <summary> Upper bound of AudioParamEQ.FrequencyGain. </summary>
            public const float FrequencyGain_Max = 3f;
            /// <summary> The frequency gain used when nothing says otherwise, read by AudioParamEQ. </summary>
            public const float FrequencyGain_Default = 2f;
        }
    }
}
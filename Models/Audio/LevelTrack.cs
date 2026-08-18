using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Audio
{
    /// <summary>
    /// One scheduled playback of an audio resource inside a level - the audio counterpart of a
    /// RectObject: the clip lives in Level.Resources.Audios, this only says when/where/how it plays.
    /// Stored in AudioLevel.Tracks; several tracks may overlap, separated by AudioLayer.
    /// </summary>
    [RuleContainer]
    public class LevelTrack : IFrameBounds, INameable, IModel<LevelTrack>
    {
        // Same logic as RectObject.ObjectId, but only for audio and much simpler
        // 0 - undefined
        // 1, 2, 3... - user-defined audio
        // negative space IS BANNED for consistency

        /// <summary> Identity of this track inside the level, handed out by LevelSettings.GetNextAudioId
        /// and used as the key of AudioLevel.Tracks. </summary>
        [RuleIPrimitiveIntMin(AudioId.MinValue)]
        [JsonProperty(Names.AudioId)]
        public AudioId AudioId { get; set; }

        // 0 - Null (no audio resource assigned), 1+ - game-defined, negative - user-defined
        // more about resourceId and how it works, read in TypedResourceId.cs file

        /// <summary> Which clip to play - points at an AudioResource, either a game-defined one or a
        /// user-defined entry of Level.Resources.Audios. Null means the track is silent. </summary>
        [RuleIPrimitiveIntNotNull, RuleReferenceExists(ResourceReferenceKind.Audio)]
        [JsonProperty(Names.AudioResourceId)]
        public AudioResourceId AudioResourceId { get; set; }

        /// <summary> Half-open stretch of the level timeline the clip sounds over, cutting it off at
        /// the end even if the clip itself is longer. </summary>
        [JsonProperty(Names.SpanShort)]
        public FrameSpan Span { get; set; }

        // Offset for audio clip itself. Frames tells where boundaries of track in level,
        // OffsetTime tells from which time starts clip itself

        /// <summary> Seconds skipped inside the clip at the span's start, measured from whichever end
        /// Speed makes the track start at. Frames place the track on the level timeline, this places
        /// the playhead inside the clip - the two are independent. </summary>
        [RuleInRange(AudioRules.MinOffsetTime, AudioRules.MaxOffsetTime)]
        [JsonProperty(Names.OffsetTime)]
        public float OffsetTime { get; set; }

        // Not a keyframed track, and deliberately so: an animated rate would make the clip position
        // the integral of that curve, which nothing downstream (BuildAudioJob, the waveform drawer,
        // AudioSource.pitch) can evaluate from a single frame's worth of data.

        /// <summary> How many seconds of the clip are consumed per second of level time, i.e. a
        /// resample - faster is also higher-pitched (nightcore), slower is also lower (slowed).
        /// Negative reverses the track: it starts at the clip's END and plays back to its start,
        /// with OffsetTime skipping the tail instead of the head. 0 freezes it (silent).
        /// Multiplies with the level's own play speed. </summary>
        [RuleInRange(AudioRules.MinSpeed, AudioRules.MaxSpeed)]
        [JsonProperty(Names.Speed)]
        public float Speed { get; set; }

        // The track's own level, and the SECOND thing called Volume on it - the first being
        // Effects.Volumes, the keyframed one. They multiply rather than compete: this is the fader
        // the whole track sits behind (set once, never animated), the keyframes are what fades it in
        // and out over the level, and an author who only wants "this track a bit quieter" should not
        // have to author a flat curve to say so. A general fader is also the one thing a keyframed
        // track cannot express without rewriting every key it has.

        /// <summary> Constant level multiplied into everything this track plays, keyframed volume
        /// included. </summary>
        [RuleInRange(AudioRules.MinVolume, AudioRules.MaxVolume)]
        [JsonProperty(Names.Volume)]
        public float Volume { get; set; }

        /// <summary> Mixing slot this track occupies, so simultaneous tracks stay separable
        /// (music / sfx / voice ...). Not a render layer - unrelated to RectObject.Layer. </summary>
        [RuleInRange(AudioRules.MinAudioLayer, AudioRules.MaxAudioLayer)]
        [JsonProperty(Names.AudioLayer)]
        public int AudioLayer { get; set; }

        /// <summary> Human-readable label shown in the editor timeline. Purely cosmetic, never an
        /// identity - AudioId is. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        /// <summary> Per-track DSP chain (lowpass, echo, reverb, ...). Always present as an object;
        /// whether it does anything is decided by LevelTrackEffects.Active. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Effects)]
        public LevelTrackEffects Effects { get; set; }
        
        public LevelTrack()
        {
            AudioId = AudioId.Null;
            AudioResourceId = AudioResourceId.Null;
            Span = new FrameSpan();
            OffsetTime = AudioRules.OffsetTimeDefault;
            Speed = AudioRules.SpeedDefault;
            Volume = AudioRules.VolumeDefault;
            AudioLayer = AudioRules.MinAudioLayer;
            Name = string.Empty;
            Effects = new LevelTrackEffects();
        }
        public LevelTrack(AudioId audioId, AudioResourceId audioResourceId, FrameSpan span,
            float offsetTime, float speed, float volume, int audioLayer, string name,
            LevelTrackEffects effects)
        {
            AudioId = audioId;
            AudioResourceId = audioResourceId;
            Span = span;
            OffsetTime = offsetTime;
            Speed = speed;
            Volume = volume;
            AudioLayer = audioLayer;
            Name = name;
            Effects = effects;
        }
        public void Reset()
        {
            AudioId = AudioId.Null;
            AudioResourceId = AudioResourceId.Null;
            Span = new FrameSpan();
            OffsetTime = AudioRules.OffsetTimeDefault;
            Speed = AudioRules.SpeedDefault;
            Volume = AudioRules.VolumeDefault;
            AudioLayer = AudioRules.MinAudioLayer;
            Name = string.Empty;
            Effects.Reset();
        }

        public object Clone() => Copy();
        public LevelTrack Copy() => new(AudioId, AudioResourceId, Span,
            OffsetTime, Speed, Volume, AudioLayer, Name, Effects.Copy());

        public override bool Equals(object obj) => obj is LevelTrack value && Equals(value);
        // HashCode.Combine takes at most 8 values and this carries 9 - the tail folds into the
        // eighth slot rather than being dropped.
        public override int GetHashCode() => HashCode.Combine(AudioId,
            Span, OffsetTime, Speed, AudioResourceId, AudioLayer, Name,
            HashCode.Combine(Volume, Effects));

        public bool Equals(LevelTrack other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = AudioId.Equals(other.AudioId)
                         && Span.Equals(other.Span)
                         && OffsetTime.Equals(other.OffsetTime)
                         && Speed.Equals(other.Speed)
                         && Volume.Equals(other.Volume)
                         && AudioResourceId.Equals(other.AudioResourceId)
                         && AudioLayer.Equals(other.AudioLayer)
                         && Name.Equals(other.Name)
                         && Effects.Equals(other.Effects);
            return result;
        }
    }
}
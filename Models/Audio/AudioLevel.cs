using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Audio
{
    /// <summary>
    /// The whole audio side of a level: one flat set of tracks, no groups or buses. Layering is
    /// expressed by tracks overlapping in time and separating by AudioLayer, not by nesting.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.AudioLevel, 1, 0)]
    public class AudioLevel : IModel<AudioLevel>
    {
        // TODO add a contextual Rule validating this whole dictionary (key must equal value's own AudioId)

        /// <summary> Every scheduled clip in the level, keyed by the track's own AudioId - the audio
        /// analogue of GameLevel.Objects. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxAudioTracks)]
        [RuleDictionaryKeyMatches(nameof(LevelTrack.AudioId))]
        [JsonProperty(Names.Tracks)]
        public Dictionary<AudioId, LevelTrack> Tracks { get; set; }

        public AudioLevel()
        {
            Tracks = new Dictionary<AudioId, LevelTrack>();
        }
        public AudioLevel(Dictionary<AudioId, LevelTrack> tracks)
        {
            Tracks = tracks;
        }
        public void Reset()
        {
            Tracks.Clear();
        }

        public object Clone() => Copy();
        public AudioLevel Copy() => new(Tracks.CopyDictionary());

        public void Update(AudioLevel src)
        {
            Tracks = src.Tracks.CopyDictionary();
        }

        public void Pull(AudioLevel src)
        {
            Tracks = src.Tracks.CopyDictionary();
        }

        public bool Equals(AudioLevel other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Tracks.DictionaryEquals(other.Tracks);
            return result;
        }

        public override bool Equals(object obj) => obj is AudioLevel value && Equals(value);
        public override int GetHashCode() => Tracks != null ? Tracks.GetDictionaryHashCode() : 0;
    }
}
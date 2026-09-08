using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Audio
{
    /// <summary>
    /// The whole audio side of a level: one flat set of tracks, no groups or buses. Layering is
    /// expressed by tracks overlapping in time and separating by AudioLayer, not by nesting.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.AudioLevel, ModelGenerations.Release)]
    [GenerateModel]
    public sealed partial class AudioLevel : IModel<AudioLevel>
    {
        /// <summary> Every scheduled clip in the level, keyed by the track's own AudioId - the audio
        /// analogue of GameLevel.Objects. </summary>
        [GenerateModelKeyed(nameof(LevelTrack.AudioId))]
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxAudioTracks)]
        [RuleDictionaryKeyMatches(nameof(LevelTrack.AudioId))]
        [JsonProperty(Names.Tracks)]
        public Dictionary<AudioId, LevelTrack> Tracks { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AudioLevel()
        {
            Tracks = new Dictionary<AudioId, LevelTrack>();
        }
        /// <summary> Built from its dictionary audio id and tracks. </summary>
        public AudioLevel(Dictionary<AudioId, LevelTrack> tracks)
        {
            Tracks = tracks;
        }
    }
}
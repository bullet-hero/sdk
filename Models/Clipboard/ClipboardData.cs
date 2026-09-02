using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Game;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Clipboard
{
    // A clipboard is a PARTIAL LEVEL, not a container of its own kind. Every section below is a
    // collection type the format already owns, so this whole aggregate rides on the existing
    // converters (DictionaryObjectsConverter, DictionaryAudiosConverter, VersionedEnvelopeConverter)
    // and introduces no new polymorphism - which is the entire reason a copied selection can be
    // handed to SerializeData like any other root.
    //
    // Two consequences worth knowing before extending it:
    // - Objects and KeyObjects are SEPARATE sections holding the same value type on purpose. The
    //   first means "create these objects", the second means "add these keyframes to an object that
    //   already exists" - a copied keyframe is carried by a stripped copy of its owner, since that
    //   is the only shape the format has for "a keyframe plus which track it belongs to". One
    //   dictionary cannot express both intents.
    // - There is no anchor frame stored anywhere. Where a paste lands is derived from the section's
    //   own contents at paste time; a stored anchor is one more thing that can disagree with what it
    //   describes after a partial edit or a clear.

    /// <summary>
    /// One copied selection, split into one section per editor timeline. A consumer keeps a single
    /// instance as the backing store of all its per-timeline buffers.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.ClipboardData, 1, 0)]
    [GenerateModel]
    public sealed partial class ClipboardData : IModel<ClipboardData>
    {
        /// <summary> Which sections carry something - kept by the consumer alongside every write. </summary>
        [RuleEnumFlagsValid]
        [JsonProperty(Names.Content)]
        public ClipboardContent Content { get; set; }

        /// <summary> Whole objects copied from a level's own scope, subtrees included. </summary>
        [GenerateModelKeyed(nameof(RectObject.ObjectId))]
        [GenerateModelMerge]
        [RuleNotNull]
        [RuleDictionaryKeyMatches(nameof(RectObject.ObjectId))]
        [JsonProperty(Names.Objects)]
        public Dictionary<ObjectId, RectObject> Objects { get; set; }

        /// <summary> Whole objects copied from a Prefab template's own scope. </summary>
        [GenerateModelKeyed(nameof(RectObject.ObjectId))]
        [GenerateModelMerge]
        [RuleNotNull]
        [RuleDictionaryKeyMatches(nameof(RectObject.ObjectId))]
        [JsonProperty(Names.PrefabObjects)]
        public Dictionary<ObjectId, RectObject> PrefabObjects { get; set; }

        /// <summary> Objects stripped down to the copied keyframes alone - carriers, not content. </summary>
        [GenerateModelKeyed(nameof(RectObject.ObjectId))]
        [GenerateModelMerge]
        [RuleNotNull]
        [RuleDictionaryKeyMatches(nameof(RectObject.ObjectId))]
        [JsonProperty(Names.KeyObjects)]
        public Dictionary<ObjectId, RectObject> KeyObjects { get; set; }

        /// <summary> Audio tracks stripped down to the copied keyframes alone. </summary>
        [GenerateModelKeyed(nameof(LevelTrack.AudioId))]
        [GenerateModelMerge]
        [RuleNotNull]
        [RuleDictionaryKeyMatches(nameof(LevelTrack.AudioId))]
        [JsonProperty(Names.KeyTracks)]
        public Dictionary<AudioId, LevelTrack> KeyTracks { get; set; }

        /// <summary> Whole audio tracks. </summary>
        [GenerateModelKeyed(nameof(LevelTrack.AudioId))]
        [GenerateModelMerge]
        [RuleNotNull]
        [RuleDictionaryKeyMatches(nameof(LevelTrack.AudioId))]
        [JsonProperty(Names.AudioTracks)]
        public Dictionary<AudioId, LevelTrack> AudioTracks { get; set; }

        /// <summary> Level-global event aggregates carrying the copied keyframes alone. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GameKeys)]
        public GameEvents GameKeys { get; set; }

        /// <inheritdoc cref="GameKeys"/>
        [RuleNotNull]
        [JsonProperty(Names.CameraKeys)]
        public CameraEvents CameraKeys { get; set; }

        /// <inheritdoc cref="GameKeys"/>
        [RuleNotNull]
        [JsonProperty(Names.PostProcessingKeys)]
        public PostProcessingEvents PostProcessingKeys { get; set; }

        /// <inheritdoc cref="GameKeys"/>
        [RuleNotNull]
        [JsonProperty(Names.PlayerKeys)]
        public PlayerEvents PlayerKeys { get; set; }

        public ClipboardData()
        {
            Content = ClipboardContent.None;
            Objects = new Dictionary<ObjectId, RectObject>();
            PrefabObjects = new Dictionary<ObjectId, RectObject>();
            KeyObjects = new Dictionary<ObjectId, RectObject>();
            KeyTracks = new Dictionary<AudioId, LevelTrack>();
            AudioTracks = new Dictionary<AudioId, LevelTrack>();
            GameKeys = new GameEvents();
            CameraKeys = new CameraEvents();
            PostProcessingKeys = new PostProcessingEvents();
            PlayerKeys = new PlayerEvents();
        }
        public ClipboardData(ClipboardContent content,
            Dictionary<ObjectId, RectObject> objects, Dictionary<ObjectId, RectObject> prefabObjects,
            Dictionary<ObjectId, RectObject> keyObjects, Dictionary<AudioId, LevelTrack> keyTracks,
            Dictionary<AudioId, LevelTrack> audioTracks, GameEvents gameKeys, CameraEvents cameraKeys,
            PostProcessingEvents postProcessingKeys, PlayerEvents playerKeys)
        {
            Content = content;
            Objects = objects;
            PrefabObjects = prefabObjects;
            KeyObjects = keyObjects;
            KeyTracks = keyTracks;
            AudioTracks = audioTracks;
            GameKeys = gameKeys;
            CameraKeys = cameraKeys;
            PostProcessingKeys = postProcessingKeys;
            PlayerKeys = playerKeys;
        }
    }
}

using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    /// <summary>
    /// The level's timeline shape and its id counters. Also the level-scope IObjectIdCounter while the
    /// objects live on GameLevel, so anything creating objects must hold both.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.LevelSettings, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class LevelSettings : IObjectIdCounter, IFrameDuration, IModel<LevelSettings>
    {
        /// <summary> Frames per second the level is authored in - fixes what one frame means, and so
        /// what every keyframe's Frame refers to. Not a rendering framerate. </summary>
        [RuleInRange(FrameRules.MinFramerate, FrameRules.MaxFramerate)]
        [JsonProperty(Names.Fps)]
        public int Fps { get; set; }

        /// <summary> Total length of the level in frames; every keyframe is validated against it. </summary>
        [RuleInRange(FrameRules.MinFrameDuration, FrameRules.MaxFrameDuration)]
        [JsonProperty(Names.FrameDurationShort)]
        public int FrameDuration { get; set; }

        /// <summary> Next free object id. Only ever grows - ids of deleted objects are never reused,
        /// so a stale reference can't silently point at a different object. </summary>
        [RuleInRange(ObjectId.MinLevelValue, LevelRules.MaxObjectIds)]
        [JsonProperty(Names.ObjectIdCounter)]
        public int ObjectIdCounter { get; set; }

        /// <summary> Next free audio track id, same never-reused rule. </summary>
        [RuleMinValue(AudioId.MinValue)]
        [JsonProperty(Names.AudioIdCounter)]
        public int AudioIdCounter { get; set; }

        // NullSeed (0) means "no seed authored", the middle tier of the resolution; test IsValidSeed.

        /// <summary> Seed every random-tagged value in this level resolves against.
        /// <see cref="LevelRules.NullSeed"/> = unset. </summary>
        [RuleMinValue(LevelRules.MinSeed)]
        [JsonProperty(Names.Seed)]
        public int Seed { get; set; }

        // Horizontal is the default though NotSpecified is zero: older levels read back as they played.
        // Outranks the player's setting while running; phones only.

        /// <summary> The orientation this level is composed for. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Orientation)]
        public LevelOrientation Orientation { get; set; }

        /// <summary> The next unused id in this scope, consuming it. </summary>
        public ObjectId GetNextObjectId()
        {
            LevelRules.AssertObjectIdAvailable(ObjectIdCounter);
            return new ObjectId(ObjectIdCounter++);
        }

        /// <summary> How many more ids this level can mint. </summary>
        public long GetRemainingObjectIds() => LevelRules.RemainingObjectIds(ObjectIdCounter);

        /// <summary> Whether a bulk create of this size can be committed whole. </summary>
        public bool CanMintObjectIds(int count) => LevelRules.CanMintObjectIds(ObjectIdCounter, count);

        /// <summary> The next unused id in this scope, consuming it. </summary>
        public AudioId GetNextAudioId() => new(AudioIdCounter++);

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public LevelSettings()
        {
            Fps = 60;
            FrameDuration = Fps * 10;
            ObjectIdCounter = ObjectId.MinLevelValue;
            AudioIdCounter = AudioId.MinValue;
            Seed = LevelRules.NullSeed;
            Orientation = LevelOrientation.Horizontal;
        }
        /// <summary> Built from its framerate, duration, id counter and id counter. </summary>
        public LevelSettings(int framerate, int frameDuration, int objectIdCounter, int audioIdCounter)
        {
            Fps = framerate;
            FrameDuration = frameDuration;
            ObjectIdCounter = objectIdCounter;
            AudioIdCounter = audioIdCounter;
            Seed = LevelRules.NullSeed;
            Orientation = LevelOrientation.Horizontal;
        }

        // Seed and Orientation ride initializers rather than further constructor parameters: every
        // existing caller of this constructor authors a level without either, and NullSeed and
        // Horizontal are exactly what they should get.
    }
}
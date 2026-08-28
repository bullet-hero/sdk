using System;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.SettingGroups
{
    /// <summary>
    /// The level's timeline shape and its id counters. Also the level-scope IObjectIdCounter, while
    /// the objects those ids belong to live on GameLevel - the two halves a Prefab carries on one
    /// class are split here, so anything creating objects must hold both.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.LevelSettings, 1, 0)]
    public class LevelSettings : IObjectIdCounter, IFrameDuration, IModel<LevelSettings>
    {
        /// <summary> Frames per second the level is authored in - fixes what one frame means, and so
        /// what every keyframe's Frame refers to. Not a rendering framerate. </summary>
        [RuleInRange(FrameRules.MinFramerate, FrameRules.MaxFramerate)]
        [JsonProperty(Names.Fps)]
        public int Framerate { get; set; }

        /// <summary> Total length of the level in frames; every keyframe is validated against it. </summary>
        [RuleInRange(FrameRules.MinFrameDuration, FrameRules.MaxFrameDuration)]
        [JsonProperty(Names.FrameDurationShort)]
        public int FrameDuration { get; set; }

        /// <summary> Next free object id. Only ever grows - ids of deleted objects are never reused,
        /// so a stale reference can't silently point at a different object. </summary>
        [RuleInRange(ObjectId.MinLevelValue, LevelRules.MaxObjects)]
        [JsonProperty(Names.ObjectIdCounter)]
        public int ObjectIdCounter { get; set; }

        /// <summary> Next free audio track id, same never-reused rule. </summary>
        [RuleMinValue(AudioId.MinValue)]
        [JsonProperty(Names.AudioIdCounter)]
        public int AudioIdCounter { get; set; }

        // LevelRules.NullSeed (0) is the DEFAULT and means "no seed authored", not "seed number
        // zero": a level ships without one and the player generates a fresh seed on every load,
        // which is the normal behaviour. An author sets this only to pin a run down - and a host may
        // still override it per-launch, so this is the middle tier of a three-step resolution, never
        // the last word. Test it with LevelRules.IsValidSeed, not with a literal.

        /// <summary> Seed every random-tagged value in this level resolves against.
        /// <see cref="LevelRules.NullSeed"/> = unset. </summary>
        [RuleMinValue(LevelRules.MinSeed)]
        [JsonProperty(Names.Seed)]
        public int Seed { get; set; }

        // Horizontal is the DEFAULT even though NotSpecified is the zero value, and that is what
        // makes this field additive AND safe at once: every level authored before it existed reads
        // back as Horizontal, which is how the game already played it. A NotSpecified default would
        // have opted every existing level into a portrait screen its content was never composed for.
        //
        // It outranks the player's own UserSettings.Interface.ScreenOrientation for as long as this
        // level is running, and it is honoured on phones only - nothing rotates a monitor, so on
        // desktop a vertical level simply plays inside pillarbox bars.

        /// <summary> The orientation this level is composed for. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Orientation)]
        public LevelOrientation Orientation { get; set; }

        public ObjectId GetNextObjectId() => new(ObjectIdCounter++);
        public AudioId GetNextAudioId() => new(AudioIdCounter++);

        public LevelSettings()
        {
            Framerate = 60;
            FrameDuration = Framerate * 10;
            ObjectIdCounter = ObjectId.MinLevelValue;
            AudioIdCounter = AudioId.MinValue;
            Seed = LevelRules.NullSeed;
            Orientation = LevelOrientation.Horizontal;
        }
        public LevelSettings(int framerate, int frameDuration, int objectIdCounter, int audioIdCounter)
        {
            Framerate = framerate;
            FrameDuration = frameDuration;
            ObjectIdCounter = objectIdCounter;
            AudioIdCounter = audioIdCounter;
            Seed = LevelRules.NullSeed;
            Orientation = LevelOrientation.Horizontal;
        }

        public object Clone() => Copy();

        // Seed and Orientation ride initializers rather than further constructor parameters: every
        // existing caller of this constructor authors a level without either, and NullSeed and
        // Horizontal are exactly what they should get.
        public LevelSettings Copy() => new(Framerate, FrameDuration, ObjectIdCounter, AudioIdCounter)
        {
            Seed = Seed,
            Orientation = Orientation,
        };

        public void Update(LevelSettings src)
        {
            Framerate = src.Framerate;
            FrameDuration = src.FrameDuration;
            ObjectIdCounter = src.ObjectIdCounter;
            AudioIdCounter = src.AudioIdCounter;
            Seed = src.Seed;
            Orientation = src.Orientation;
        }

        public void Pull(LevelSettings src)
        {
            Framerate = src.Framerate;
            FrameDuration = src.FrameDuration;
            ObjectIdCounter = src.ObjectIdCounter;
            AudioIdCounter = src.AudioIdCounter;
            Seed = src.Seed;
            Orientation = src.Orientation;
        }

        public override bool Equals(object obj) => obj is LevelSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Framerate, FrameDuration, ObjectIdCounter, AudioIdCounter, Seed, Orientation);

        public void Reset()
        {
            Framerate = 60;
            FrameDuration = Framerate * 10;
            ObjectIdCounter = ObjectId.MinLevelValue;
            AudioIdCounter = AudioId.MinValue;
            Seed = LevelRules.NullSeed;
            Orientation = LevelOrientation.Horizontal;
        }

        public bool Equals(LevelSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Framerate.Equals(other.Framerate)
                          && FrameDuration.Equals(other.FrameDuration)
                          && ObjectIdCounter.Equals(other.ObjectIdCounter)
                          && AudioIdCounter.Equals(other.AudioIdCounter)
                          && Seed.Equals(other.Seed)
                          && Orientation == other.Orientation;
            return result;
        }
    }
}
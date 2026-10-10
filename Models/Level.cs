using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Game;
using BH.SDK.Models.Hints;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Resources;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    /// <summary>
    /// The level itself - the root of level.json/.blob: five versioned aggregates, four authored plus Hints
    /// (measured from them). Name, authors and licensing are in LevelMeta, so listing never loads levels.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.Level, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class Level : IModel<Level>
    {
        /// <summary> Timeline shape and id counters - framerate, length, and the next free object
        /// and audio id. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Settings)]
        public LevelSettings Settings { get; set; }

        /// <summary> Objects and the level-global event tracks: what is seen and what happens. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Game)]
        public GameLevel Game { get; set; }

        /// <summary> Scheduled audio tracks - separate from Game because sound is placed on the
        /// timeline, not in the scene. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Audio)]
        public AudioLevel Audio { get; set; }

        /// <summary> Everything the above two reference by id: textures, fonts, clips, colliders,
        /// themes, effects, prefabs. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Resources)]
        public LevelResources Resources { get; set; }

        // The only aggregate holding nothing an author wrote: measured from the four above and droppable.
        // Membership test: LevelHints.

        /// <summary> Advisory measurements about the four above - preallocation sizes, warm-up sets.
        /// Never authoritative: a hint may be stale, foreign or absent. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Hints)]
        public LevelHints Hints { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Level()
        {
            Settings = new LevelSettings();
            Game = new GameLevel();
            Audio = new AudioLevel();
            Resources = new LevelResources();
            Hints = new LevelHints();
        }

        /// <summary> Built from its settings, game, audio and resources. </summary>
        public Level(LevelSettings settings, GameLevel game, AudioLevel audio, LevelResources resources)
            : this(settings, game, audio, resources, new LevelHints())
        {
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public Level(LevelSettings settings, GameLevel game, AudioLevel audio, LevelResources resources,
            LevelHints hints)
        {
            Settings = settings;
            Game = game;
            Audio = audio;
            Resources = resources;
            Hints = hints;
        }

        // A reference-sharing copy for PrefabVirtualizationUtils.Thin only: Copy() walks the whole graph,
        // too slow per autosave. MemberwiseClone so a later member joins by itself; the writer only reads.

        /// <summary> A copy sharing every member instance, for replacing one or two of them. </summary>
        internal Level ShallowClone() => (Level)MemberwiseClone();
    }
}
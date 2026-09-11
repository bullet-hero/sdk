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
    /// The level itself - the root of level.json/.blob. Five independent aggregates, each its own
    /// versioned domain: four authored, plus Hints, which is measured from them. Note what is NOT
    /// here: the level's name, authors and licensing live in a separate LevelMeta file, so listing
    /// levels never means loading them.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.Level, ModelGenerations.Release)]
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

        // The only aggregate here that holds nothing an author wrote, which is why it is separate
        // from the four above rather than a few fields spread across them: everything inside is
        // measured from those four and may be dropped without changing what the level is. See
        // LevelHints' own header for the membership test.

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

        // A REFERENCE-SHARING COPY, AND THE ONLY CALLER IS PrefabVirtualizationUtils.Thin. Thinning
        // a level for a write has to replace exactly two collections (a scope's Objects, and the
        // prefab table holding scopes of their own) without touching the level anyone else is
        // holding - the editor keeps editing straight through a save. Copy() is the wrong tool: it
        // walks the whole graph, which is 124 ms on the largest level here and would be paid on
        // every autosave. MemberwiseClone is used rather than a hand-written member list precisely
        // because a member added later joins it by itself; a hand-written one would silently drop
        // the new member from every written file. Shared instances are safe here because the writer
        // only reads - it is NOT a substitute for the snapshot a save already takes.

        /// <summary> A copy sharing every member instance, for replacing one or two of them. </summary>
        internal Level ShallowClone() => (Level)MemberwiseClone();
    }
}
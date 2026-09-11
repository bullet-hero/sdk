using BH.SDK.Models.Attributes;
using BH.SDK.Models.Game;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Resources;
using BH.SDK.Models.SettingGroups;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V0
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Generation 0 of the level domain. A frozen snapshot - never edit it to match today's shape. </summary>
    [ModelGeneration(ModelDomains.Level, ModelGenerations.Test)]
    [GenerateModel]
    public sealed partial class LevelV0 : IModel<LevelV0>
    {
        // Settings/Game/Resources are independently-versioned domains ([ModelGeneration] on
        // LevelSettings/GameLevel/LevelResources) - a nested envelope always resolves its OWN
        // generation tag and upgrades it to the domain's current type before returning, regardless of
        // which generation *this* container (Level) itself is. So these properties must be typed
        // using the CURRENT classes, not LevelSettingsV0/GameLevelV0/LevelResourcesV0 - those still
        // exist and are still registered at generation Test, they're just never anyone's actual
        // field type, only VersionedTypeRegistry's resolve target.
        //
        // THIS IS THE RULE MOST EASILY BROKEN NOW THAT A SNAPSHOT IS A GENERATED MODEL. Reading this
        // class through its own codec emits ReadEnveloped<LevelSettings>, which resolves that nested
        // envelope independently; typing it as LevelSettingsV0 would migrate the same payload twice.

        /// <summary> Its own versioned domain, typed as the CURRENT class for the same reason. </summary>
        [JsonProperty(NamesV0.Settings)]
        public LevelSettings Settings { get; set; }

        /// <summary> The same. </summary>
        [JsonProperty("test_game")]
        public GameLevel Game { get; set; }

        // Audio intentionally has NO [ModelGeneration] at this generation - AudioLevel wasn't an
        // independently-versioned domain yet at Level generation 0, so this is deserialized as a plain,
        // un-enveloped nested object and must be migrated by hand in LevelV0ToLevel.

        /// <summary> NOT a versioned domain at this generation, so it is read as a plain nested object and migrated by hand. </summary>
        [JsonProperty("test_audio")]
        public AudioLevelV0 Audio { get; set; }

        /// <summary> Its own versioned domain, typed as the CURRENT class. </summary>
        [JsonProperty("test_resources")]
        public LevelResources Resources { get; set; }

        /// <summary> Every member at the value generation 0 would have read into it. </summary>
        public LevelV0()
        {
            Settings = new LevelSettings();
            Game = new GameLevel();
            Audio = new AudioLevelV0();
            Resources = new LevelResources();
        }
    }
}

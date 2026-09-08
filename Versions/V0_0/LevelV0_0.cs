using BH.SDK.Models.Audio;
using BH.SDK.Models.Game;
using BH.SDK.Models.Resources;
using BH.SDK.Models.SettingGroups;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V0_0
{
    // ReSharper disable once InconsistentNaming


    /// <summary> The v0.0 generation of the level domain. A frozen snapshot - never edit it to match today's shape. </summary>
    [DataVersion(DataDomains.Level, 0, 0)]
    public class LevelV0_0
    {
        // Settings/Game/Resources are independently-versioned domains ([DataVersion] on
        // LevelSettings/GameLevel/LevelResources) - VersionedEnvelopeConverter always resolves a
        // nested envelope's own version tag and upgrades it to the domain's current type before
        // returning, regardless of what version *this* container (Level) itself is. So these
        // properties must be typed using the CURRENT classes, not LevelSettingsV0_0/GameLevelV0_0/
        // LevelResourcesV0_0 - those still exist and are still registered via [DataVersion(...,0,0)],
        // they're just never anyone's actual field type, only VersionedTypeRegistry's resolve target.

        /// <summary> Its own versioned domain, typed as the CURRENT class for the same reason. </summary>
        [JsonProperty(NamesV0_0.Settings)]
        public LevelSettings Settings { get; set; }

        /// <summary> The same. </summary>
        [JsonProperty("test_game")]
        public GameLevel Game { get; set; }

        // Audio intentionally has NO [DataVersion] at this generation - AudioLevel wasn't an
        // independently-versioned domain yet at Level v0.0, so this is deserialized as a plain,
        // un-enveloped nested object and must be migrated by hand in LevelV0_0ToLevel.

        /// <summary> NOT a versioned domain at this generation, so it is read as a plain nested object and migrated by hand. </summary>
        [JsonProperty("test_audio")]
        public AudioLevelV0_0 Audio { get; set; }

        /// <summary> Its own versioned domain, typed as the CURRENT class. </summary>
        [JsonProperty("test_resources")]
        public LevelResources Resources { get; set; }
    }
}
using BH.SDK.Models;
using BH.SDK.Models.Audio;

namespace BH.SDK.Versions.V0_0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> The level container, v0.0 to v1.0. Its three versioned halves arrive already upgraded; only
    /// audio, which was not a domain yet, is carried by hand. </summary>
    public class LevelV0_0ToV1_0 : DataMigration<LevelV0_0, Level>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override Level Migrate(LevelV0_0 from) => new(
            from.Settings, // already upgraded to current LevelSettings by VersionedEnvelopeConverter
            from.Game, // already upgraded to current GameLevel by VersionedEnvelopeConverter
            new AudioLevel(), // Audio wasn't an independently-versioned domain at v0.0 (see AudioLevelV0_0 - no fields, no [DataVersion]), nothing to carry over
            from.Resources); // already upgraded to current LevelResources by VersionedEnvelopeConverter
    }
}

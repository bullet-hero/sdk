using BH.SDK.Models;
using BH.SDK.Models.Audio;

namespace BH.SDK.Versions.V0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> The level container, generation 0 to generation 1. Its three versioned halves arrive already upgraded; only
    /// audio, which was not a domain yet, is carried by hand. </summary>
    public class LevelV0ToV1 : ModelMigration<LevelV0, Level>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override Level Migrate(LevelV0 from) => new(
            from.Settings, // already upgraded to current LevelSettings by VersionedEnvelopeConverter
            from.Game, // already upgraded to current GameLevel by VersionedEnvelopeConverter
            new AudioLevel(), // Audio wasn't an independently-versioned domain at generation 0 (see AudioLevelV0 - no fields, no [ModelGeneration]), nothing to carry over
            from.Resources); // already upgraded to current LevelResources by VersionedEnvelopeConverter
    }
}

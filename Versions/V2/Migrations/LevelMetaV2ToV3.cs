using BH.SDK.Models;
using BH.SDK.Rules;

namespace BH.SDK.Versions.V2.Migrations
{
    // ReSharper disable once InconsistentNaming

    // Undefined and unmeasured, never a guess: a migrator can neither judge how hard a level is nor measure
    // its demand without the level.

    /// <summary> Level metadata, generation 2 to generation 3 - every member carried as it is, the
    /// difficulty left unrated and the demand unmeasured. </summary>
    public class LevelMetaV2ToV3 : ModelMigration<LevelMetaV2, LevelMeta>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override LevelMeta Migrate(LevelMetaV2 from)
            => new(from.LevelId, from.LevelName, from.LevelDescription, from.LevelLogo, from.LevelVersion,
                from.LevelLicense, from.LevelAuthors, from.ResourcesMeta, from.LevelAgeRating)
            {
                LevelTags = from.LevelTags,
                LevelDuration = from.LevelDuration,
                MinGeneration = from.MinGeneration,
                LevelAiGenerated = from.LevelAiGenerated,
                LevelDifficulty = DifficultyRules.Undefined,
                LevelDemand = null,
            };
    }
}

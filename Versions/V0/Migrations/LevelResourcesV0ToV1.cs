using BH.SDK.Models.Resources;

namespace BH.SDK.Versions.V0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Level resources, generation 0 to generation 1. </summary>
    public class LevelResourcesV0ToV1 : ModelMigration<LevelResourcesV0, LevelResources>
    {
        // LevelResourcesV0.Resources was placeholder test data with no correspondence to the
        // current Textures/Fonts/Audios/CompositeShapes/Themes/Prefabs shape - nothing to map.

        /// <summary> Builds the newer shape out of the older one. </summary>
        public override LevelResources Migrate(LevelResourcesV0 from) => new();
    }
}

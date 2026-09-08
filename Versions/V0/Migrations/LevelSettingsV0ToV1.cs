using BH.SDK.Models.Primitives;
using BH.SDK.Models.SettingGroups;

namespace BH.SDK.Versions.V0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Level settings, generation 0 to generation 1 - everything but the framerate is filled from today's defaults. </summary>
    public class LevelSettingsV0ToV1 : ModelMigration<LevelSettingsV0, LevelSettings>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override LevelSettings Migrate(LevelSettingsV0 from) => new(
            from.Framerate,
            from.Framerate * 10, // FrameDuration didn't exist at generation 0 - same derivation as LevelSettings' own default ctor
            ObjectId.MinLevelValue, // ObjectIdCounter didn't exist at generation 0
            AudioId.MinValue); // AudioIdCounter didn't exist at generation 0
    }
}

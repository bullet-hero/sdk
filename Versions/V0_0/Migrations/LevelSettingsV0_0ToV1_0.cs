using BH.SDK.Models.Primitives;
using BH.SDK.Models.SettingGroups;

namespace BH.SDK.Versions.V0_0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Level settings, v0.0 to v1.0 - everything but the framerate is filled from today's defaults. </summary>
    public class LevelSettingsV0_0ToV1_0 : DataMigration<LevelSettingsV0_0, LevelSettings>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override LevelSettings Migrate(LevelSettingsV0_0 from) => new(
            from.Framerate,
            from.Framerate * 10, // FrameDuration didn't exist at v0.0 - same derivation as LevelSettings' own default ctor
            ObjectId.MinLevelValue, // ObjectIdCounter didn't exist at v0.0
            AudioId.MinValue); // AudioIdCounter didn't exist at v0.0
    }
}

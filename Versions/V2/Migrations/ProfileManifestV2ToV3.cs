using BH.SDK.Models.Profile;

namespace BH.SDK.Versions.V2.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> The profile-archive manifest, generation 2 to generation 3 - every member carried as it
    /// is; a generation 2 archive never carries skins. </summary>
    public class ProfileManifestV2ToV3 : ModelMigration<ProfileManifestV2, ProfileManifest>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override ProfileManifest Migrate(ProfileManifestV2 from) => new()
        {
            CreatedUtc = from.CreatedUtc,
            GameVersion = from.GameVersion,
            ModelGeneration = from.ModelGeneration,
            Platform = from.Platform,
            Categories = from.Categories,
            Levels = from.Levels,
        };
    }
}

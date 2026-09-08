using BH.SDK.Models.Game;

namespace BH.SDK.Versions.V0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> The game domain, generation 0 to generation 1 - the three event tracks that did not exist yet come in empty. </summary>
    public class GameLevelV0ToV1 : ModelMigration<GameLevelV0, GameLevel>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override GameLevel Migrate(GameLevelV0 from) => new(
            from.GameEvents, // already upgraded to current GameEvents by VersionedEnvelopeConverter
            new CameraEvents(), // didn't exist at generation 0
            new PostProcessingEvents(), // didn't exist at generation 0
            new PlayerEvents(), // didn't exist at generation 0
            from.Objects);
    }
}

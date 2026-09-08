using BH.SDK.Models.Game;

namespace BH.SDK.Versions.V0_0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Game events, v0.0 to v1.0. </summary>
    public class GameEventsV0_0ToV1_0 : DataMigration<GameEventsV0_0, GameEvents>
    {
        // GameEventsV0_0 has no fields - nothing to carry over, just the current defaults.

        /// <summary> Builds the newer shape out of the older one. </summary>
        public override GameEvents Migrate(GameEventsV0_0 from) => new();
    }
}

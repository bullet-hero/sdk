using BH.SDK.Models.Game;

namespace BH.SDK.Versions.V0.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Game events, generation 0 to generation 1. </summary>
    public class GameEventsV0ToV1 : ModelMigration<GameEventsV0, GameEvents>
    {
        // GameEventsV0 has no fields - nothing to carry over, just the current defaults.

        /// <summary> Builds the newer shape out of the older one. </summary>
        public override GameEvents Migrate(GameEventsV0 from) => new();
    }
}

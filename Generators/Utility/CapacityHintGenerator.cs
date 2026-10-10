using BH.SDK.Utils;

namespace BH.SDK.Generators.Utility
{
    // LevelHints.Limits is peak simultaneous objects per type, for a player to preallocate. The editor
    // refreshes it on every save; this makes the number visible on demand. Neither creates nor edits.

    /// <summary>
    /// Recomputes the level's advisory capacity hint from what the level currently contains.
    /// </summary>
    public class CapacityHintGenerator : BaseContentGenerator<CapacityHintGenerator.Parameters>
    {
        /// <summary> <c>"gen_capacity_hint"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_capacity_hint";

        /// <summary> What must be true before a host offers this run. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.LevelScope;

        /// <summary> No hints - the form takes the fields in declaration order. </summary>
        public override GeneratorHints Hints => GeneratorHints.Empty;

        // Limits is a bare field, nothing for GeneratorCost to count: the estimate is permanently Zero and
        // the host's zero-estimate refusal must not apply.

        /// <summary> True: this run writes something GeneratorCost cannot measure, so a zero estimate is not an empty run. </summary>
        public override bool AllowsEmptyRun => true;

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            if (context?.Hints == null) return;

            var level = LevelOf(context);
            if (level == null) return;

            // Written straight onto Hints: no journal entry shape for a bare field. Undo leaves the
            // refreshed hint, harmless because it is advisory and recomputed on every save.
            context.Hints.Limits = LevelCapacityUtils.GetPeakUsage(level);
        }

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        /// <summary> Rebuilds the owning Level from the context's own parts. The context carries the
        /// pieces rather than the Level itself (a Prefab-scoped run has no level), and LevelScope
        /// guarantees Game/Audio are present here. </summary>
        private static Models.Level LevelOf(GeneratorContext context)
        {
            if (context?.Game == null || context.Audio == null) return null;
            return new Models.Level(context.Settings, context.Game, context.Audio, context.Resources,
                context.Hints);
        }

        /// <summary> No parameters - the level itself is the whole input. </summary>
        public class Parameters
        {
        }
    }
}

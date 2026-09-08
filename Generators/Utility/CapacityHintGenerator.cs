using BH.SDK.Utils;

namespace BH.SDK.Generators.Utility
{
    // LevelHints.Limits is peak simultaneous objects per type, written so a player can preallocate
    // its per-frame buffers instead of growing them mid-level. The editor already refreshes it on
    // every save (EditorService.Save), so this is not
    // about correctness - it is about being able to SEE the number, on demand, while deciding
    // whether a section is too heavy. That is why it is a generator rather than a hidden step: the
    // estimate line reports what the level currently peaks at, before anything is written.
    //
    // The one generator that neither creates nor edits objects. It exists partly to prove the
    // contract stretches that far.

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

        // Limits is a bare field on LevelHints, so there is nothing here for GeneratorCost to
        // count and the estimate is permanently Zero. Without this the host's "a run that would add
        // nothing is refused" rule leaves the button disabled forever - the one generator whose whole
        // job is invisible to the cost model.

        /// <summary> True: this run writes something GeneratorCost cannot measure, so a zero estimate is not an empty run. </summary>
        public override bool AllowsEmptyRun => true;

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            if (context?.Hints == null) return;

            var level = LevelOf(context);
            if (level == null) return;

            // Written straight onto Hints rather than through the context: Limits is not an
            // object, a resource or a level-global track, so there is no journal entry shape for it.
            // The consequence is deliberate and bounded - undoing this run leaves the refreshed
            // hint in place, which is harmless because the value is advisory and is recomputed on
            // every save anyway.
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

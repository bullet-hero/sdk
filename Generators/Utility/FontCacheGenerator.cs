using System.Collections.Generic;
using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Services;

namespace BH.SDK.Generators.Utility
{
    // The sibling of CapacityHintGenerator, and for the same reason: LevelHints.FontCharacters is
    // advisory, a host editor already refreshes it on every save, and this exists so an author can
    // rebuild the whole thing on demand without saving - after importing a foreign level, after
    // editing the file by hand, or whenever the saved set is suspected of having drifted.
    //
    // Unlike CapacityHintGenerator it is fully undoable. That one writes Hints.Limits
    // directly because the journal has no entry shape for a bare field; FontCharacters is a
    // keyed dictionary, which AddResource/RemoveResource already cover whatever it is keyed by.
    // Every write
    // below therefore goes through the context, and an existing entry is REMOVED before the new one
    // is added rather than overwritten - ResourceAdded.Revert removes the key it added, so
    // overwriting in place would make undo delete an entry that existed before the run.

    /// <summary>
    /// Rebuilds every per-font character set in the level from scratch, out of the text the level
    /// currently contains.
    /// </summary>
    public class FontCacheGenerator : BaseContentGenerator<FontCacheGenerator.Parameters>
    {
        /// <summary> <c>"gen_font_cache"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_font_cache";

        /// <summary> What must be true before a host offers this run. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.LevelScope;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.RemoveUnused))
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var cache = context.Hints?.FontCharacters;
            if (cache == null) return;

            var built = FontCharacterService.BuildAll(context.Scope);

            foreach (var (fontResourceId, cached) in built)
                Write(context, cache, fontResourceId, cached);

            if (!parameters.RemoveUnused) return;

            // Snapshotted before removing: RemoveResource mutates the very dictionary being walked.
            var stale = new List<FontResourceId>();
            foreach (var (fontResourceId, _) in cache)
                if (!built.ContainsKey(fontResourceId))
                    stale.Add(fontResourceId);

            foreach (var fontResourceId in stale)
                context.RemoveResource(cache, fontResourceId);
        }

        // Reported as Resources, and not left at Zero the way CapacityHintGenerator leaves it: a host
        // refuses a Content run whose whole estimate is zero, on the grounds that a button which
        // silently does nothing is worse than a refusal (see the editor's GeneratorsView). Counting
        // the entries this would write and drop makes the estimate both honest and non-zero whenever
        // there is anything to do - and correctly zero, hence correctly refused, on a level with no
        // text and no stale entries.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var cache = context?.Hints?.FontCharacters;
            if (cache == null) return GeneratorCost.Zero;

            var built = FontCharacterService.BuildAll(context.Scope);
            var touched = built.Count;

            if (parameters.RemoveUnused)
                foreach (var (fontResourceId, _) in cache)
                    if (!built.ContainsKey(fontResourceId))
                        touched++;

            return new GeneratorCost(0, 0, touched);
        }

        /// <summary> Replaces one entry as a remove-then-add pair, so undo restores what was there
        /// instead of deleting it. </summary>
        private static void Write(GeneratorContext context, Dictionary<FontResourceId, CachedFontText> target,
            FontResourceId fontResourceId, CachedFontText cached)
        {
            if (target.ContainsKey(fontResourceId))
                context.RemoveResource(target, fontResourceId);
            context.AddResource(target, fontResourceId, cached);
        }

        /// <summary> Which fonts are rebuilt. Public mutable fields, like every parameters class here - a form
        /// binds to them and a preset serializes from them. </summary>
        public class Parameters
        {
            /// <summary> Drop entries for fonts no text object references any more. On by default -
            /// a stale entry warms glyphs nothing will draw, and "from scratch" means the result
            /// matches the level exactly. </summary>
            public bool RemoveUnused = true;
        }
    }
}

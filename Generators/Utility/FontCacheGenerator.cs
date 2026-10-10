using System.Collections.Generic;
using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Services;

namespace BH.SDK.Generators.Utility
{
    // The sibling of CapacityHintGenerator, but fully undoable (a keyed dictionary, covered by
    // AddResource/RemoveResource). An existing entry is REMOVED before the new one is added: undo of an
    // overwrite would delete an entry that existed before the run. Docs/GENERATORS.md.

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

        // Reported as Resources (entries written and dropped), so it is non-zero whenever there is
        // anything to do and a host does not refuse the run.

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

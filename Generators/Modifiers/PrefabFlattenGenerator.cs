using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;

namespace BH.SDK.Generators.Modifiers
{
    // The mass form of one placement's Flatten (PrefabFlattenUtils): nothing materializes or moves.
    // ORDER IS LOAD-BEARING with templates included: the active scope first, templates second, or the
    // level shows content its templates no longer describe. Docs/GENERATORS.md.

    /// <summary>
    /// Turns prefab placements back into ordinary objects: the link to the template goes, the objects
    /// it materialized stay exactly where they are. Templates themselves are kept.
    /// </summary>
    public class PrefabFlattenGenerator : BaseModifier<PrefabFlattenGenerator.Parameters>
    {
        /// <summary> <c>"mod_prefab_flatten"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "mod_prefab_flatten";

        // Whole-scope, not the selection (mod_content_remover and mod_span_fit say why). Per-placement
        // flattening is the editor's own operation.

        /// <summary> Nothing beyond a scope to run against. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.None;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.IncludeTemplates),
                nameof(Parameters.RemoveUnusedTemplates))
            .Build();

        /// <summary> Applies this run's edit. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            FlattenScope(context, context.Scope);

            // Both switches need the WHOLE level; Game is null while a Prefab template is the scope, and
            // half an answer there is worse than none.
            if (context.Game == null) return;

            if (parameters.IncludeTemplates) FlattenTemplates(context);
            if (parameters.RemoveUnusedTemplates) RemoveUnusedTemplates(context);
        }

        // Edits and deletes only. GeneratorCost describes what a run ADDS, and reporting the
        // placements it touches would read as "this will add N", which is the opposite of what
        // happens - mod_content_remover answers the same way for the same reason.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        // Flattening destroys no content (one undo restores every link); removing templates takes
        // authored content out, even one nothing references today.

        /// <summary> True where these parameters would destroy content the author did not point at. </summary>
        protected override bool IsDangerousTyped(GeneratorContext context, Parameters parameters)
            => parameters.RemoveUnusedTemplates;

        private static void FlattenScope(GeneratorContext context, IObjectScope scope)
        {
            // Collected first: Replace writes through the very dictionary being enumerated.
            var placements = new List<ObjectId>();
            PrefabFlattenUtils.CollectAll(scope, placements);

            foreach (var id in placements) context.Replace<RectObject>(scope, id);
        }

        private static void FlattenTemplates(GeneratorContext context)
        {
            if (context.Resources?.Prefabs == null) return;

            foreach (var template in context.Resources.Prefabs.Values)
                FlattenScope(context, template);
        }

        // Swept to a FIXED POINT: a template kept by another template that is itself removed would
        // survive a single pass. Also makes the result independent of dictionary order.
        private static void RemoveUnusedTemplates(GeneratorContext context)
        {
            if (context.Resources?.Prefabs == null) return;

            var referenced = new HashSet<PrefabId>();
            var doomed = new List<PrefabId>();

            while (true)
            {
                referenced.Clear();
                PrefabFlattenUtils.CollectReferencedPrefabs(context.Game, referenced);
                foreach (var template in context.Resources.Prefabs.Values)
                    PrefabFlattenUtils.CollectReferencedPrefabs(template, referenced);

                doomed.Clear();
                foreach (var id in context.Resources.Prefabs.Keys)
                    if (!referenced.Contains(id))
                        doomed.Add(id);

                if (doomed.Count == 0) return;
                foreach (var id in doomed) context.RemoveResource(context.Resources.Prefabs, id);
            }
        }

        /// <summary> What one flatten run is configured to do. </summary>
        public class Parameters
        {
            /// <summary> Also flatten the placements sitting inside the level's own prefab templates,
            /// so no placement survives anywhere. Level scope only - the whole level has to be
            /// reachable for the templates to be flattened in the right order. </summary>
            public bool IncludeTemplates;

            /// <summary> Delete templates nothing references any more. Off by default: a flatten
            /// keeps the level's prefab library intact, and this is a second decision. Level scope
            /// only - which templates are still referenced cannot be answered from inside one. </summary>
            public bool RemoveUnusedTemplates;
        }
    }
}
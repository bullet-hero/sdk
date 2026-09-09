using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;

namespace BH.SDK.Generators.Modifiers
{
    // The mass form of what one placement's own Flatten does - see PrefabFlattenUtils for why a
    // flatten is bookkeeping removal rather than a rebuild, and why it always takes a whole chain.
    // Nothing here materializes, resyncs or moves anything: every copy a placement owns is already an
    // ordinary object, so the level looks identical the instant after a run.
    //
    // ORDER IS LOAD-BEARING WHEN TEMPLATES ARE INCLUDED: the active scope goes first, templates
    // second. A template edit is normally propagated to every placement of it, so flattening
    // templates while placements still stand would leave the level showing content the templates no
    // longer describe until something happens to resync them. Once the level holds no placement at
    // all, a template reaches nobody and the order stops mattering - which is exactly the state the
    // first half produces.

    /// <summary>
    /// Turns prefab placements back into ordinary objects: the link to the template goes, the objects
    /// it materialized stay exactly where they are. Templates themselves are kept.
    /// </summary>
    public class PrefabFlattenGenerator : BaseModifier<PrefabFlattenGenerator.Parameters>
    {
        /// <summary> <c>"mod_prefab_flatten"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "mod_prefab_flatten";

        // Whole-scope rather than the selection, the same reasoning mod_content_remover and
        // mod_span_fit record: "leave no placements here" is a statement about the scope, and
        // quietly limiting it to what happens to be selected would leave the rest linked while
        // reporting success. Per-placement flattening is the editor's own operation, not this.

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

            // Both switches need to see the WHOLE level - one to reach the level's placements before
            // the templates they instantiate, the other to know which templates are still referenced
            // at all. Game is null while a Prefab template is the active scope, and half an answer
            // there is worse than none: see each field's own summary.
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

        // Flattening itself is never dangerous: it destroys no content, and the templates stay in the
        // level exactly as they were, so one undo puts every link back. Removing templates is the
        // opposite - it takes authored content out of the level, and a template nothing references
        // today is still the thing an author placed yesterday.

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

        // SWEPT TO A FIXED POINT, NOT IN ONE PASS. A template is kept by any surviving reference,
        // including one from inside another template - so removing an unreferenced template can leave
        // whatever IT named referenced by nothing, and a single pass would keep that one for a reason
        // it no longer has. Repeating until nothing moves is also what makes the result independent
        // of dictionary order. The loop always terminates: every round either removes at least one
        // template or stops.
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
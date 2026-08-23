using System;

namespace BH.SDK.Generators
{
    // The `where TParams : class, new()` constraint is what replaced the old CRTP one. It is the
    // whole reason IGenerator can be non-generic, and therefore the whole reason a registry and an
    // auto-built form are possible at all - see IGenerator's header.

    /// <summary>
    /// Shared plumbing for Content and Modifier generators: implements the untyped IScopeGenerator
    /// surface once, so a concrete generator only writes typed code.
    /// </summary>
    public abstract class BaseScopeGenerator<TParams> : IScopeGenerator where TParams : class, new()
    {
        public abstract string NameKey { get; }
        public abstract GeneratorKind Kind { get; }

        /// <inheritdoc/>
        public virtual int ListOrder => 0;

        public virtual GeneratorRequirements Requirements => GeneratorRequirements.None;
        public virtual GeneratorHints Hints => GeneratorHints.Empty;

        /// <summary> True for every Modifier by default - editing and deleting what already exists is
        /// exactly a zero-cost run. A Content generator that writes something GeneratorCost cannot
        /// measure overrides this; one that simply produced nothing this time must not. </summary>
        public virtual bool AllowsEmptyRun => Kind == GeneratorKind.Modifier;

        public Type ParametersType => typeof(TParams);
        public object CreateDefaultParameters() => CreateDefaults();

        /// <summary> Override to seed non-default values; the field initializers of TParams cover
        /// most cases on their own. </summary>
        protected virtual TParams CreateDefaults() => new();

        // The grouping container is the context's object, not the generator's, so it is added here
        // rather than in every EstimateTyped - and only when the run produces something, matching
        // GeneratorContext's lazy creation (a run that creates nothing creates no container either).
        public GeneratorCost Estimate(GeneratorContext context, object parameters)
        {
            var cost = EstimateTyped(context, Cast(parameters));
            return context is { IsGrouping: true } && cost.Objects > 0
                ? cost + new GeneratorCost(1)
                : cost;
        }

        public bool IsDangerous(GeneratorContext context, object parameters)
            => IsDangerousTyped(context, Cast(parameters));

        /// <summary> Override only where a parameter combination can destroy or rewrite content the
        /// author didn't point at - the host turns a true into an explicit confirmation step. Adding
        /// objects is never dangerous on its own: it is one undo away. </summary>
        protected virtual bool IsDangerousTyped(GeneratorContext context, TParams parameters) => false;

        public GeneratorResult Run(GeneratorContext context, object parameters)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            Generate(context, Cast(parameters));

            // After Generate, never inside it: layer splitting numbers the run's objects in creation
            // order, and a generator writing obj.Layer = context.Layer would overwrite it otherwise.
            context.ApplyLayerSplit();

            return new GeneratorResult(context.Log.GetCreatedIds(), context.Log);
        }

        /// <summary> Do the work. Mutate ONLY through the context - see GeneratorChangeLog's header
        /// for what happens otherwise. </summary>
        protected abstract void Generate(GeneratorContext context, TParams parameters);

        /// <summary> Must match what Generate actually produces; a test enforces the equality. </summary>
        protected abstract GeneratorCost EstimateTyped(GeneratorContext context, TParams parameters);

        private static TParams Cast(object parameters)
        {
            if (parameters is TParams typed) return typed;
            throw new ArgumentException(
                $"Expected {typeof(TParams).Name}, got {parameters?.GetType().Name ?? "null"}", nameof(parameters));
        }
    }
}

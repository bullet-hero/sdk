using System;

namespace BH.SDK.Generators
{
    /// <summary>
    /// Base for a generator that builds a whole level. No GeneratorContext and no change log: there
    /// is nothing to mutate yet, and the level either gets created or it doesn't.
    /// </summary>
    public abstract class BaseLevelGenerator<TParams> : ILevelGenerator where TParams : class, new()
    {
        public abstract string NameKey { get; }

        public GeneratorKind Kind => GeneratorKind.Level;

        /// <inheritdoc/>
        public virtual int ListOrder => 0;

        public virtual GeneratorRequirements Requirements => GeneratorRequirements.None;
        public virtual GeneratorHints Hints => GeneratorHints.Empty;

        public Type ParametersType => typeof(TParams);
        public object CreateDefaultParameters() => CreateDefaults();

        protected virtual TParams CreateDefaults() => new();

        // Context is always null here - a level generator runs before any level exists. Estimating
        // is still worth offering: gen_level_audio_file's frame length follows the song's duration,
        // and a host shows that number before the author commits to it.
        public GeneratorCost Estimate(GeneratorContext context, object parameters)
            => EstimateTyped(Cast(parameters));

        // A level generator builds a NEW level from nothing - there is no existing content for a
        // parameter combination to destroy, so this stays false for the whole family rather than
        // being an override point like BaseScopeGenerator's.
        public bool IsDangerous(GeneratorContext context, object parameters) => false;

        public GeneratedLevel Create(object parameters) => CreateTyped(Cast(parameters));

        /// <summary> Build the level and its metadata together, so the two can't disagree. </summary>
        protected abstract GeneratedLevel CreateTyped(TParams parameters);

        protected virtual GeneratorCost EstimateTyped(TParams parameters) => GeneratorCost.Zero;

        private static TParams Cast(object parameters)
        {
            if (parameters is TParams typed) return typed;
            throw new ArgumentException(
                $"Expected {typeof(TParams).Name}, got {parameters?.GetType().Name ?? "null"}", nameof(parameters));
        }
    }
}

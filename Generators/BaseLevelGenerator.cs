using System;
using BH.SDK.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Generators
{
    /// <summary>
    /// Base for a generator that builds a whole level. No GeneratorContext and no change log: there
    /// is nothing to mutate yet, and the level either gets created or it doesn't.
    /// </summary>
    public abstract class BaseLevelGenerator<TParams> : ILevelGenerator where TParams : class, new()
    {
        /// <summary> Stable identifier of this generator, shaped as a localization key. </summary>
        public abstract string NameKey { get; }

        /// <summary> Fixed here, so a level generator cannot advertise itself as anything else. </summary>
        public GeneratorKind Kind => GeneratorKind.Level;

        /// <inheritdoc/>
        public virtual int ListOrder => 0;

        /// <summary> What must be true before a host offers this run. Nothing, by default. </summary>
        public virtual GeneratorRequirements Requirements => GeneratorRequirements.None;

        /// <summary> How a host should lay the parameters out. Empty means declaration order. </summary>
        public virtual GeneratorHints Hints => GeneratorHints.Empty;

        /// <summary> The parameters class a host builds its form out of, by reflection. </summary>
        public Type ParametersType => typeof(TParams);

        /// <summary> A fresh parameters object at its defaults. </summary>
        public object CreateDefaultParameters() => CreateDefaults();

        /// <summary> Override to seed non-default values; the field initializers cover most cases on their own. </summary>
        protected virtual TParams CreateDefaults() => new();

        // Context is always null here - a level generator runs before any level exists. Estimating
        // is still worth offering: gen_level_audio_file's frame length follows the song's duration,
        // and a host shows that number before the author commits to it.

        /// <summary> What the run would add, before it runs - which is what a host refuses on. </summary>
        public GeneratorCost Estimate(GeneratorContext context, object parameters)
            => EstimateTyped(Cast(parameters));

        // A level generator builds a NEW level from nothing - there is no existing content for a
        // parameter combination to destroy, so this stays false for the whole family rather than
        // being an override point like BaseScopeGenerator's.

        /// <summary> Never: a level generator builds a new level, so there is nothing of the author's to destroy. </summary>
        public bool IsDangerous(GeneratorContext context, object parameters) => false;

        /// <summary> Builds the level, casting the parameters on the way in. </summary>
        public GeneratedLevel Create(object parameters) => CreateTyped(Cast(parameters));

        /// <summary> Build the level and its metadata together, so the two can't disagree. </summary>
        protected abstract GeneratedLevel CreateTyped(TParams parameters);

        /// <summary> Override where the run's size is knowable in advance. </summary>
        protected virtual GeneratorCost EstimateTyped(TParams parameters) => GeneratorCost.Zero;

        // A level with no screen limit plays at the device's aspect, so a level built from nothing offers
        // a 16:9 pin on its first frame as a KEYFRAME (visible on the Events timeline, deletable). An
        // import offers none: it keeps its source's limit.

        /// <summary> Pins a level built from nothing to the default aspect (16:9) from its first
        /// frame, unless it already has a limit of its own. </summary>
        protected static void SeedScreenLimit(Level level)
        {
            if (level.Game.Events.ScreenLimits.Count > 0) return;
            level.Game.Events.ScreenLimits.Add(new ScreenLimitKey(
                new ScreenLimitFixed(new ScreenAspect()), FrameRules.MinFrame));
        }

        private static TParams Cast(object parameters)
        {
            if (parameters is TParams typed) return typed;
            throw new ArgumentException(
                $"Expected {typeof(TParams).Name}, got {parameters?.GetType().Name ?? "null"}", nameof(parameters));
        }
    }
}
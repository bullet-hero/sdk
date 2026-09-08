using System;

namespace BH.SDK.Generators
{
    // Estimate() has to agree with what Run() actually produces - that equality is covered by a test
    // rather than left as a promise, because the whole point of the estimate is that a host shows it
    // BEFORE running and refuses when it would push the scope past LevelRules.MaxObjects. An
    // estimate that quietly drifts from reality is worse than no estimate at all.

    /// <summary>
    /// How much a generator run would add, measured before running it.
    /// </summary>
    public readonly struct GeneratorCost : IEquatable<GeneratorCost>
    {
        /// <summary> Objects the run would create. </summary>
        public readonly int Objects;

        /// <summary> Keyframes the run would add, across every track it touches. </summary>
        public readonly int Keyframes;

        /// <summary> Level resources (textures, audio, themes, ...) the run would add. </summary>
        public readonly int Resources;

        /// <summary> One estimate; anything not given is zero. </summary>
        public GeneratorCost(int objects, int keyframes = 0, int resources = 0)
        {
            Objects = objects;
            Keyframes = keyframes;
            Resources = resources;
        }

        /// <summary> A run that adds nothing. </summary>
        public static readonly GeneratorCost Zero = new(0);

        /// <summary> Two estimates added component-wise, which is how a run of runs is measured. </summary>
        public static GeneratorCost operator +(GeneratorCost a, GeneratorCost b) =>
            new(a.Objects + b.Objects, a.Keyframes + b.Keyframes, a.Resources + b.Resources);

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"{Objects} object(s), {Keyframes} key(s), {Resources} resource(s)";

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is GeneratorCost value && Equals(value);
        /// <summary> Matches the equality above. </summary>
        public override int GetHashCode() => HashCode.Combine(Objects, Keyframes, Resources);

        /// <summary> Component by component. </summary>
        public bool Equals(GeneratorCost other) => Objects == other.Objects
                                                   && Keyframes == other.Keyframes
                                                   && Resources == other.Resources;
    }
}

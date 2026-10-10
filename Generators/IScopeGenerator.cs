namespace BH.SDK.Generators
{
    /// <summary> A generator that writes into an existing scope, Content and Modifier alike: they differ
    /// in intent and Requirements, not in mechanism. </summary>
    public interface IScopeGenerator : IGenerator
    {
        GeneratorResult Run(GeneratorContext context, object parameters);

        // A host refuses a run whose estimate is zero. A generator whose output GeneratorCost cannot
        // express (a bare advisory field) says so here instead of the host guessing from Kind.

        /// <summary> Whether an all-zero <see cref="GeneratorCost"/> is this generator's normal state
        /// rather than "these parameters produce nothing". </summary>
        bool AllowsEmptyRun { get; }
    }
}

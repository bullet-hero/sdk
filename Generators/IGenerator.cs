using System;

namespace BH.SDK.Generators
{
    // This exists to be non-generic. The previous design's CRTP constraint
    // (where TParams : BaseGenerator<TParams>.BaseInputParameters) made a common root impossible,
    // which made a registry impossible, which forced every host to hardcode every generator by
    // hand - the single reason adding one was expensive. Everything a host needs to LIST, LABEL and
    // build a FORM for a generator is on this interface; running it needs one of the two
    // sub-interfaces, picked by Kind.

    /// <summary>
    /// What every generator is, independent of what it generates.
    /// </summary>
    public interface IGenerator
    {
        /// <summary> Stable snake_case identity, mirroring LevelEditorOperation.OperationNameKey:
        /// "gen_level_*" for level generators, "gen_*" for content, "mod_*" for modifiers. Also the
        /// root of every label key ("{NameKey}_{field}"), and the only identity a user ever sees -
        /// class names are never shown. </summary>
        string NameKey { get; }

        GeneratorKind Kind { get; }

        // A HOST LISTS GENERATORS IN ONE ORDER AND IT HAS TO MEAN SOMETHING. Alphabetical by NameKey
        // is stable, which is all the registry needed while every generator was equal - but the
        // level-creation list is a first impression, and there "Afterbeat import" cannot come before
        // "Empty". Sorting by a declared rank rather than by a list kept in the UI is what keeps a
        // generator's place a property OF THE GENERATOR: adding one means writing one number in it,
        // not editing a screen that has never heard of it.
        //
        // Rank first, NameKey second, so everything at the default is still alphabetical and still
        // stable. Negative pulls forward (the obvious starting points), positive pushes back (an
        // import from another game's format, which is nobody's first answer to "make a level").

        /// <summary> Where this sits in a host's list. Lower is earlier; 0 is the default and sorts
        /// alphabetically among its peers. </summary>
        int ListOrder { get; }

        /// <summary> What the host must provide before this can run. </summary>
        GeneratorRequirements Requirements { get; }

        /// <summary> The class a form is reflected out of. Its public instance fields ARE the
        /// parameters. </summary>
        Type ParametersType { get; }

        /// <summary> A fresh parameters instance with this generator's defaults. Constructed inside
        /// the SDK on purpose - the host never calls Activator over ParametersType, so the AOT
        /// compiler sees the instantiation and the stripper keeps it. </summary>
        object CreateDefaultParameters();

        /// <summary> Presentation facts reflection can't supply. Never null; GeneratorHints.Empty
        /// for a parameterless generator. </summary>
        GeneratorHints Hints { get; }

        /// <summary> What running this would add, without running it. Context is null for a Level
        /// generator (nothing exists yet to run against), so implementations must tolerate that. </summary>
        GeneratorCost Estimate(GeneratorContext context, object parameters);

        /// <summary> Whether THIS configuration destroys or rewrites more than a run normally does,
        /// so a host should make the author confirm it explicitly. False by default: it is about a
        /// specific set of parameters, not about the generator - the same generator answers both
        /// ways depending on what is filled in. Same null-context rule as Estimate. </summary>
        bool IsDangerous(GeneratorContext context, object parameters);
    }
}
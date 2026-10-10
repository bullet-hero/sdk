using System;

namespace BH.SDK.Generators
{
    // Non-generic on purpose: a CRTP constraint on the parameters made a common root, hence a registry,
    // impossible. Everything a host needs to LIST, LABEL and build a FORM for is here; running needs one
    // of the two sub-interfaces, picked by Kind.

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

        // Rank first, NameKey second (stable and alphabetical at the default). Negative pulls forward,
        // positive pushes back (an import). The place is a property of the generator, not of a screen.

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
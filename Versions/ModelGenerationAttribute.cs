using System;

namespace BH.SDK.Versions
{
    // Marks a class as an aggregate root for the versioning system: a boundary that gets its own
    // envelope ({"g": ..., "v": ...}) and is migrated as one unit. Most model classes never carry
    // this - only the aggregate roots, and optionally deeper internal aggregates later, introduced
    // lazily. See Docs/VERSIONING.md for the full rationale.
    //
    // ONE NUMBER, NOT TWO, and the reason is that the second one never had anything to say: a
    // change to the shape of a file either needs a migration or does not, and there is no
    // intermediate grade a minor could express. What it did instead was invite a bump nobody
    // migrated. Every historical frozen snapshot of an aggregate root also carries this attribute,
    // with its own (old) generation - VersionedTypeRegistry needs it to resolve generation -> Type.
    // Classes that exist only as another snapshot's implementation detail (a nested leaf type frozen
    // alongside its container) do NOT need this attribute themselves.
    //
    // MATCHED BY SIMPLE NAME by BH.SDK.Roslyn's ModelSpecFactory.ResolveDomain, which sees the
    // user's source through symbols and references nothing here. Renaming this type without
    // renaming that literal compiles clean and changes the format.

    /// <summary> Marks a class as a versioning boundary: it gets its own <c>{g, v}</c> envelope and is
    /// migrated as one unit. Every frozen historical snapshot carries it too, with its own old generation -
    /// that is how a generation resolves back to a type. </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModelGenerationAttribute : Attribute
    {
        /// <summary> Which domain this class is a generation of. </summary>
        public string Domain { get; }

        /// <summary> Which generation of that domain it is. Assigned forward and never reused;
        /// <see cref="ModelGenerations"/> names the ones that exist. </summary>
        public int Generation { get; }

        /// <summary> Built from its domain and generation. </summary>
        public ModelGenerationAttribute(string domain, int generation)
        {
            Domain = domain;
            Generation = generation;
        }
    }
}

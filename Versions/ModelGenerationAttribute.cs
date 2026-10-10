using System;

namespace BH.SDK.Versions
{
    // Marks an aggregate root: a boundary with its own envelope ({"g": ..., "v": ...}) migrated as one
    // unit. Every frozen snapshot of a root carries it too, at its own generation (the registry's key).
    // MATCHED BY SIMPLE NAME in BH.SDK.Roslyn's ModelSpecFactory.ResolveDomain: rename both or neither.

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

using System;

namespace BH.SDK.Versions
{
    // Marks a class as an aggregate root for the versioning system: a boundary that gets its own
    // envelope ({"version": ..., "value": ...}) and is migrated as one unit. Most model classes
    // never carry this - only the six SaveData kinds today, and optionally deeper internal
    // aggregates later, introduced lazily. See VERSION-UPDATE.md for the full rationale.
    //
    // Every historical frozen snapshot of an aggregate root also carries this attribute, with its
    // own (old) Major/Minor - VersionedTypeRegistry needs it to resolve version -> Type. Classes
    // that exist only as another snapshot's implementation detail (a nested leaf type frozen
    // alongside its container) do NOT need this attribute themselves.

    /// <summary> Marks a class as a versioning boundary: it gets its own <c>{version, value}</c> envelope and is
    /// migrated as one unit. Every frozen historical snapshot carries it too, with its own old numbers -
    /// that is how a version resolves back to a type. </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataVersionAttribute : Attribute
    {
        /// <summary> Which domain this class is a generation of. </summary>
        public string Domain { get; }

        /// <summary> Breaking generation - a reader of an older one cannot be trusted with it. </summary>
        public int Major { get; }

        /// <summary> Additive generation within a major. </summary>
        public int Minor { get; }

        /// <summary> Built from its domain, major and minor. </summary>
        public DataVersionAttribute(string domain, int major, int minor)
        {
            Domain = domain;
            Major = major;
            Minor = minor;
        }

        /// <summary> The two numbers as one comparable value. </summary>
        public Version Version => new(Major, Minor);
    }
}

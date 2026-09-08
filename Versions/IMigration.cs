using System;

namespace BH.SDK.Versions
{
    // Untyped handle so VersionedTypeRegistry can hold and chain migrators for any (TFrom, TTo)
    // pair without knowing every pair at compile time.

    /// <summary> A migration step seen without its types, so the registry can chain any pair of them. </summary>
    public interface IMigration
    {
        /// <summary> The type this step reads. </summary>
        Type FromType { get; }

        /// <summary> The type this step produces. </summary>
        Type ToType { get; }

        /// <summary> Run the step, casting on the way in. </summary>
        object MigrateUntyped(object from);
    }

    /// <summary> The same step with its two types named. </summary>
    public interface IMigration<in TFrom, out TTo> : IMigration
    {
        /// <summary> Build the newer shape out of the older one. </summary>
        TTo Migrate(TFrom from);
    }
}

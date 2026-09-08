using System;

namespace BH.SDK.Versions
{
    // Convenience base for a single migration step between two adjacent generations of one domain's
    // aggregate root (e.g. GameLevelV0 -> GameLevel). Implementers only need to write Migrate.

    /// <summary> One step between two adjacent generations of a domain. Write <see cref="Migrate"/> and nothing else. </summary>
    public abstract class ModelMigration<TFrom, TTo> : IMigration<TFrom, TTo>
    {
        /// <summary> The snapshot type this step reads. </summary>
        public Type FromType => typeof(TFrom);

        /// <summary> The type this step produces - the next generation, not necessarily the current one. </summary>
        public Type ToType => typeof(TTo);

        /// <summary> Build the newer shape out of the older one. </summary>
        public abstract TTo Migrate(TFrom from);

        object IMigration.MigrateUntyped(object from) => Migrate((TFrom)from);
    }
}

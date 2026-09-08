using System;
using System.Collections.Generic;
using System.Reflection;

namespace BH.SDK.Versions
{
    // Scans this assembly once for every [ModelGeneration]-tagged type and every IMigration implementer,
    // then answers the two questions the old service never solved together: generation -> concrete Type,
    // and old instance -> latest instance (by walking registered migration steps)

    /// <summary> Answers the two questions a versioned format asks: which type a generation names, and how an old
    /// instance walks up to today's. Built once, by scanning this assembly for <c>[ModelGeneration]</c> and every
    /// <see cref="IMigration"/>, so registering either is declaring it and nothing more. </summary>
    public static class VersionedTypeRegistry
    {
        private static readonly Dictionary<string, Dictionary<int, Type>> Types = new();
        private static readonly Dictionary<string, ModelGenerationAttribute> LatestAttributes = new();
        private static readonly Dictionary<Type, IMigration> MigrationsByFromType = new();

        static VersionedTypeRegistry()
        {
            var types = typeof(VersionedTypeRegistry).Assembly.GetTypes();

            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<ModelGenerationAttribute>();
                if (attribute == null) continue;

                if (!Types.TryGetValue(attribute.Domain, out var generations))
                {
                    generations = new Dictionary<int, Type>();
                    Types[attribute.Domain] = generations;
                }
                generations[attribute.Generation] = type;

                if (!LatestAttributes.TryGetValue(attribute.Domain, out var latest)
                    || attribute.Generation > latest.Generation)
                {
                    LatestAttributes[attribute.Domain] = attribute;
                }
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (!typeof(IMigration).IsAssignableFrom(type)) continue;

                var migration = (IMigration)Activator.CreateInstance(type);
                MigrationsByFromType[migration.FromType] = migration;
            }
        }

        /// <summary> True when a type is a versioning boundary and therefore gets its own envelope. </summary>
        public static bool CanConvert(Type type) => type.GetCustomAttribute<ModelGenerationAttribute>() != null;

        /// <summary> Which domain a type belongs to; throws when it is not a boundary at all. </summary>
        public static string GetDomain(Type type)
        {
            var attribute = type.GetCustomAttribute<ModelGenerationAttribute>();
            if (attribute == null)
                throw new ArgumentException($"Type '{type}' has no [ModelGeneration] attribute", nameof(type));
            return attribute.Domain;
        }

        /// <summary> Refuses a type that carries no <c>[ModelGeneration]</c>, before anything is read on its behalf. </summary>
        public static void ThrowIfNoDomain(Type type)
        {
            var attribute = type.GetCustomAttribute<ModelGenerationAttribute>();
            if (attribute == null)
                throw new ArgumentException($"Type '{type}' has no [ModelGeneration] attribute", nameof(type));
        }

        /// <summary> The newest generation this build knows for a domain - what everything is migrated towards. </summary>
        public static ModelGenerationAttribute GetLatestAttribute(string domain)
        {
            if (LatestAttributes.TryGetValue(domain, out var attribute)) return attribute;
            throw new NotSupportedException($"Unknown data domain: '{domain}'");
        }

        /// <summary> The snapshot class one generation names, so a file can be read as the shape it was written in. </summary>
        public static Type Resolve(string domain, int generation)
        {
            if (Types.TryGetValue(domain, out var generations) && generations.TryGetValue(generation, out var type))
                return type;
            throw new NotSupportedException($"Unsupported generation {generation} for domain '{domain}'");
        }

        /// <summary> Walks the registered steps from the generation a file claimed up to today's, one at a time.
        /// A missing step throws rather than being skipped - a half-migrated document is worse than a refusal. </summary>
        public static object UpgradeToLatest(string domain, object instance, int fromGeneration)
        {
            if (instance == null) return null;

            var latest = GetLatestAttribute(domain);
            var current = instance;
            var currentGeneration = fromGeneration;

            while (currentGeneration != latest.Generation)
            {
                var currentType = current.GetType();
                if (!MigrationsByFromType.TryGetValue(currentType, out var migration))
                    throw new NotSupportedException(
                        $"No migration registered from '{currentType}' towards domain '{domain}' generation {latest.Generation}");

                current = migration.MigrateUntyped(current);

                var toAttribute = migration.ToType.GetCustomAttribute<ModelGenerationAttribute>();
                if (toAttribute == null)
                    throw new InvalidOperationException(
                        $"Migration target '{migration.ToType}' must carry a [ModelGeneration] attribute");

                currentGeneration = toAttribute.Generation;
            }

            return current;
        }
    }
}

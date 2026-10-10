using System;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Serialization;

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

        /// <summary> Refuses a generation newer than this build's latest for <paramref name="domain"/>. Every read
        /// site that sees a generation calls it BEFORE any fallback, so a newer file never degrades. </summary>
        public static void ThrowIfNewer(string domain, int generation)
        {
            var latest = GetLatestAttribute(domain).Generation;
            if (generation > latest) throw new NewerGenerationException(domain, generation, latest);
        }

        /// <summary> The snapshot class one generation names, or null when nothing does - for the callers that
        /// must BRANCH on the answer rather than be handed a substitute they would then read a file into. </summary>
        public static Type TryResolve(string domain, int generation)
        {
            if (Types.TryGetValue(domain, out var generations) && generations.TryGetValue(generation, out var type))
                return type;
            return null;
        }

        // An unknown generation of a known domain falls back to the tolerant read, and every
        // substitution is reported (Docs/Issues/FORWARD_COMPATIBILITY_HISTORY.md). An unknown DOMAIN
        // still throws: there is no current shape to fall back to.

        /// <summary> The snapshot class one generation names. An unknown generation falls back to the domain's
        /// CURRENT shape and reports the substitution; an unknown domain has nothing to fall back to. </summary>
        public static Type Resolve(string domain, int generation)
        {
            var type = TryResolve(domain, generation);
            if (type != null) return type;

            var latest = GetLatestAttribute(domain);
            var current = Types[domain][latest.Generation];

            // Word it exactly as IJsonModel.ReadOtherGeneration does: JsonParityTests compares the two
            // stacks' REPORTS, since identical defaults can come from opposite reasons.
            SerializationReport.Report(domain, current.Name, "read into today's shape by property name",
                generation, SubstitutionKind.UnknownGeneration);
            return current;
        }

        /// <summary> Walks the registered steps from the generation a file claimed up to today's, one at a time,
        /// and returns whatever it reached - which is the instance unchanged when the chain is missing a step. </summary>
        public static object UpgradeToLatest(string domain, object instance, int fromGeneration)
        {
            TryUpgradeToLatest(domain, instance, fromGeneration, out var upgraded);
            return upgraded;
        }

        // The walk stops instead of throwing: a missing step is a version problem, not a damaged file.
        // The caller must check the result is today's type and fall back to a default instance.

        /// <summary> The same walk, saying whether it actually arrived. False leaves <paramref name="result"/>
        /// holding the furthest instance the chain reached, which may still be the snapshot itself. </summary>
        public static bool TryUpgradeToLatest(string domain, object instance, int fromGeneration, out object result)
        {
            result = instance;
            if (instance == null) return true;

            var latest = GetLatestAttribute(domain);
            var currentGeneration = fromGeneration;
            HashSet<Type> visited = null;

            while (currentGeneration != latest.Generation)
            {
                var currentType = result.GetType();

                if (!MigrationsByFromType.TryGetValue(currentType, out var migration))
                {
                    SerializationReport.Report(domain, currentType.Name,
                        $"no migration towards generation {latest.Generation}", currentGeneration,
                        SubstitutionKind.IncompleteChain);
                    return false;
                }

                // A cycle is a registration bug rather than a file's fault, but it is the one shape
                // that would hang the read instead of failing it, so it is answered here too.
                visited ??= new HashSet<Type>();
                if (!visited.Add(currentType))
                {
                    SerializationReport.Report(domain, currentType.Name,
                        "the migration chain loops back on itself", currentGeneration,
                        SubstitutionKind.IncompleteChain);
                    return false;
                }

                var toAttribute = migration.ToType.GetCustomAttribute<ModelGenerationAttribute>();
                if (toAttribute == null)
                    throw new InvalidOperationException(
                        $"Migration target '{migration.ToType}' must carry a [ModelGeneration] attribute");

                result = migration.MigrateUntyped(result);
                currentGeneration = toAttribute.Generation;
            }

            return true;
        }
    }
}
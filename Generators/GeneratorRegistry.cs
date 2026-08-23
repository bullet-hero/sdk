using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BH.SDK.Generators
{
    // One-time reflection scan in a static constructor, the same shape as VersionedTypeRegistry's -
    // already proven to survive IL2CPP stripping in the consuming project, so it needs no new
    // link.xml entry of its own beyond the SDK-assembly-wide preserve.
    //
    // Only this assembly is scanned. A generator living in a host assembly would not be found, and
    // that is intentional for now: generators are format-level content, they belong next to the
    // format. Widening this to other assemblies means deciding which ones, and there is no consumer
    // asking for it yet.

    /// <summary>
    /// Every generator the SDK ships, discovered by reflection so adding one means adding a class
    /// and nothing else.
    /// </summary>
    public static class GeneratorRegistry
    {
        private static readonly Dictionary<string, IGenerator> ByKey;
        private static readonly IGenerator[] Ordered;

        static GeneratorRegistry()
        {
            var types = typeof(GeneratorRegistry).Assembly.GetTypes();
            var found = new List<IGenerator>();

            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) continue;
                if (!typeof(IGenerator).IsAssignableFrom(type)) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                found.Add((IGenerator)Activator.CreateInstance(type));
            }

            found.Sort(static (a, b) =>
            {
                var byOrder = a.ListOrder.CompareTo(b.ListOrder);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.NameKey, b.NameKey);
            });

            ByKey = new Dictionary<string, IGenerator>(found.Count);
            foreach (var generator in found)
            {
                // A duplicate key would make "which generator is gen_radial" depend on reflection
                // order - fail at static init, where it is one obvious exception, rather than at
                // first UI open, where it looks like a missing generator.
                if (ByKey.TryGetValue(generator.NameKey, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Duplicate generator NameKey '{generator.NameKey}': " +
                        $"{existing.GetType().Name} and {generator.GetType().Name}");
                }
                ByKey.Add(generator.NameKey, generator);
            }

            Ordered = found.ToArray();
        }

        /// <summary> Every generator, ordered by <see cref="IGenerator.ListOrder"/> and then by
        /// NameKey, so a host's list reads sensibly and is stable across runs. </summary>
        public static IReadOnlyList<IGenerator> All => Ordered;

        public static IGenerator Get(string nameKey) => ByKey[nameKey];
        public static bool TryGet(string nameKey, out IGenerator generator) => ByKey.TryGetValue(nameKey, out generator);

        public static IEnumerable<IGenerator> OfKind(GeneratorKind kind)
        {
            foreach (var generator in Ordered)
                if (generator.Kind == kind)
                    yield return generator;
        }

        /// <summary> Forces the static constructor, so a duplicate-key mistake surfaces when a host
        /// wants it to rather than on whichever call happens to touch the registry first. </summary>
        public static void EnsureInitialized()
            => RuntimeHelpers.RunClassConstructor(typeof(GeneratorRegistry).TypeHandle);
    }
}

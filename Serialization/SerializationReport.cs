using System;
using System.Collections.Generic;
using System.Threading;

namespace BH.SDK.Serialization
{
    /// <summary> What one degradation substituted. </summary>
    public enum SubstitutionKind : byte
    {
        /// <summary> An OLDER generation no snapshot is registered for - a gap in the ladder. A newer one is
        /// never reported: it is refused with <see cref="BH.SDK.Versions.NewerGenerationException"/>. </summary>
        UnknownGeneration = 0,

        /// <summary> An older generation that resolved and migrated. Reported because it is a change, not a loss. </summary>
        MigratedGeneration = 1,

        /// <summary> A snapshot resolved but the migration chain from it does not reach today's shape. </summary>
        IncompleteChain = 2,

        /// <summary> An envelope carrying no generation at all. </summary>
        AbsentGeneration = 3,

        /// <summary> A polymorphic tag this build has no type for; the family's lowest tag stood in. </summary>
        UnknownTag = 4,

        /// <summary> A root whose content this build could not parse; it was skipped by its declared length. </summary>
        UnreadableContent = 5
    }

    /// <summary> One substitution a degraded read made. </summary>
    public readonly struct SerializationSubstitution
    {
        /// <summary> The domain the substitution happened in, or the family name for a tag. </summary>
        public readonly string Domain;

        /// <summary> The member, tag or type the substitution happened AT. </summary>
        public readonly string Site;

        /// <summary> What stood in for what was asked for. </summary>
        public readonly string Substituted;

        /// <summary> The generation involved, or <see cref="Versions.ModelGenerations.Invalid"/>. </summary>
        public readonly int Generation;

        /// <summary> Which of the seven degradations this is. </summary>
        public readonly SubstitutionKind Kind;

        /// <summary> Fills every field; nothing here is optional. </summary>
        public SerializationSubstitution(string domain, string site, string substituted, int generation,
            SubstitutionKind kind)
        {
            Domain = domain;
            Site = site;
            Substituted = substituted;
            Generation = generation;
            Kind = kind;
        }

        /// <summary> One line, meant for a console a person is reading. </summary>
        public override string ToString() =>
            $"{Kind} in '{Domain}' at '{Site}': {Substituted}" +
            (Generation == Versions.ModelGenerations.Invalid ? "" : $" (generation {Generation})");
    }

    // The collector is ambient because the generated codecs hold a bare reader and nothing else.
    // AsyncLocal, not [ThreadStatic]: the consumer reads on the thread pool, and a server needs it per
    // logical call. An entry carries NO tree path - a breadcrumb would tax the ordinary path.

    /// <summary> Collects what a degraded read substituted, for whoever asked to be told. </summary>
    public sealed class SerializationReport
    {
        private static readonly AsyncLocal<SerializationReport> Ambient = new();

        private readonly List<SerializationSubstitution> _entries = new();

        /// <summary> The collector this call is filling, or null when nobody asked. </summary>
        public static SerializationReport Current => Ambient.Value;

        /// <summary> Everything collected so far, oldest first. </summary>
        public IReadOnlyList<SerializationSubstitution> Entries => _entries;

        /// <summary> How many substitutions were made. </summary>
        public int Count => _entries.Count;

        /// <summary> Whether the read came back whole. </summary>
        public bool IsEmpty => _entries.Count == 0;

        /// <summary> Collects for this call until the handle is disposed, restoring whatever collected before. </summary>
        public static IDisposable Begin() => Begin(new SerializationReport());

        /// <summary> The same, into a collector the caller already holds. </summary>
        public static IDisposable Begin(SerializationReport report)
        {
            var scope = new Scope(Ambient.Value);
            Ambient.Value = report ?? throw new ArgumentNullException(nameof(report));
            return scope;
        }

        /// <summary> Records one substitution, or does nothing at all when nobody is collecting. </summary>
        public static void Report(string domain, string site, string substituted, int generation,
            SubstitutionKind kind)
        {
            var current = Ambient.Value;
            if (current == null) return;
            current.Add(new SerializationSubstitution(domain, site, substituted, generation, kind));
        }

        // Locked, cheaply and only on the rare path: one logical call CAN fan out (the consumer
        // reads a level's resources in parallel), and two List.Add calls racing corrupt the list
        // rather than losing an entry.

        /// <summary> Records one substitution. </summary>
        public void Add(in SerializationSubstitution entry)
        {
            lock (_entries) _entries.Add(entry);
        }

        /// <summary> Forgets everything collected, so one instance can be reused across reads. </summary>
        public void Clear()
        {
            lock (_entries) _entries.Clear();
        }

        private sealed class Scope : IDisposable
        {
            private readonly SerializationReport _previous;
            private bool _disposed;

            public Scope(SerializationReport previous) => _previous = previous;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Ambient.Value = _previous;
            }
        }
    }
}

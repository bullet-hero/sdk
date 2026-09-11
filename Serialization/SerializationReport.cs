using System;
using System.Collections.Generic;
using System.Threading;

namespace BH.SDK.Serialization
{
    /// <summary> What one degradation substituted. </summary>
    public enum SubstitutionKind : byte
    {
        /// <summary> A generation no snapshot is registered for - a file from the future, or a gap in the ladder. </summary>
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
        UnreadableContent = 5,

        /// <summary> A root whose content ended before its declared length - a future build appended members. </summary>
        ShortContent = 6
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

    // THE COLLECTOR IS AMBIENT BECAUSE THE CODECS HOLD NOTHING. A generated codec carries a bare
    // reader and no context at all, which is what IJsonModel's own header defends and what makes it
    // fast; threading a report parameter through every one of the seven generated bodies would take
    // that apart for a field that is null in every ordinary read.
    //
    // AsyncLocal RATHER THAN [ThreadStatic], and the difference is not academic here: the consumer
    // deserializes on the thread pool (UniTask.RunOnThreadPool, because a whole-level read is a
    // second of frozen frame otherwise), so a thread-static collector opened by the caller would be
    // installed on the thread that does none of the reading and would come back empty every time.
    // AsyncLocal flows into that hop and nowhere else, which is also what the SDK needs as a server
    // DLL: per logical call, so two concurrent reads never interleave one file's findings into the
    // other's.
    //
    // AN ENTRY DELIBERATELY CARRIES NO TREE PATH. A breadcrumb would have to be pushed and popped on
    // the ordinary path, which is the one path that must not pay for this; the domain and the
    // member name are what the degradation site knows for free.

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

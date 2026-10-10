using System;
using BH.SDK.Versions;

namespace BH.SDK.Serialization.Blob
{
    // A root this build cannot parse is stepped over whole by its declared length; a root from the
    // FUTURE is refused (NewerGenerationException); a throw INSIDE content is a version problem, not
    // damage (the header answered that); content not ending at its length is damage. Docs/SERIALIZATION.md.

    /// <summary> The runtime half of a generated root's envelope read: settle the declared length,
    /// skip what this build cannot read, and migrate what it can. </summary>
    public static class BlobEnvelopes
    {
        /// <summary> Settles an ordinary content read against its declared length. Anything but an exact match
        /// is damage and is refused. </summary>
        public static void Finish(ref BlobReader reader, string domain, int contentStart, int length)
        {
            var read = reader.Position - contentStart;
            if (read == length) return;

            throw new BlobFormatException(
                $"{domain} read {read} bytes of the {length} it declared - the payload is damaged");
        }

        /// <summary> Answers a content read that threw: steps over whatever the root declared and leaves the
        /// caller to reset itself. </summary>
        public static void Unreadable(ref BlobReader reader, string domain, int contentStart, int length,
            BlobFormatException error)
        {
            var read = reader.Position - contentStart;
            if (read > length)
                throw new BlobFormatException(
                    $"{domain} read past the {length} bytes it declared - the payload is damaged, not newer", error);

            if (read < length) reader.Skip(length - read);

            SerializationReport.Report(domain, domain, $"the whole root was skipped: {error.Message}",
                ModelGenerations.Invalid, SubstitutionKind.UnreadableContent);
        }

        // A snapshot is read through IBinaryEnvelope, not IBinaryModel: the envelope this method is
        // answering is already consumed, so the snapshot's own Read would look for a second one.

        /// <summary> Reads a root written at another generation: refuses a newer one, migrates an older one when
        /// the chain reaches today's shape, and otherwise skips the root whole. Null means the caller resets itself. </summary>
        public static T OtherGeneration<T>(ref BlobReader reader, string domain, int generation,
            int contentStart, int length) where T : class
        {
            VersionedTypeRegistry.ThrowIfNewer(domain, generation);
            var type = VersionedTypeRegistry.TryResolve(domain, generation);

            if (type == null || !typeof(IBinaryEnvelope).IsAssignableFrom(type))
            {
                reader.Skip(length - (reader.Position - contentStart));
                SerializationReport.Report(domain, domain, "the whole root was skipped and left at its defaults",
                    generation, SubstitutionKind.UnknownGeneration);
                return null;
            }

            var snapshot = (IBinaryEnvelope)Activator.CreateInstance(type);

            try
            {
                snapshot.ReadContent(ref reader);
            }
            catch (BlobFormatException error)
            {
                Unreadable(ref reader, domain, contentStart, length, error);
                return null;
            }

            Finish(ref reader, domain, contentStart, length);

            if (!VersionedTypeRegistry.TryUpgradeToLatest(domain, snapshot, generation, out var upgraded))
                return null;

            if (upgraded is not T typed)
            {
                SerializationReport.Report(domain, type.Name, $"the chain arrived at {upgraded?.GetType().Name}",
                    generation, SubstitutionKind.IncompleteChain);
                return null;
            }

            SerializationReport.Report(domain, type.Name,
                $"migrated to generation {VersionedTypeRegistry.GetLatestAttribute(domain).Generation}",
                generation, SubstitutionKind.MigratedGeneration);
            return typed;
        }
    }
}

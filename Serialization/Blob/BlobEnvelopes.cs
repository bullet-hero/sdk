using System;
using BH.SDK.Versions;

namespace BH.SDK.Serialization.Blob
{
    // DEGRADING AT DOMAIN GRANULARITY IS FREE, AND THAT IS WHY IT IS WHERE THE LINE IS DRAWN. A root
    // already writes `domain + generation + length + content`, so the framing that makes a skip safe
    // is paid for at all twenty roots whether or not anything skips. A level from the future opens
    // with, say, GameLevel empty but LevelSettings, AudioLevel and LevelResources intact.
    //
    // ANYTHING THAT THROWS INSIDE A ROOT'S CONTENT IS A VERSION PROBLEM, NOT DAMAGE, and that is what
    // lets one catch site stand in for a per-value fallback the binary format cannot have. Damage is
    // already answered before the payload is parsed at all: BlobFormat.ReadHeader checks the magic,
    // the codec generation, the declared length against the real one and an xxHash64 of the whole
    // payload, in that order. A byte that survived all four and then fails to parse is a shape this
    // build does not know - an unknown polymorphic tag, an enum member added later - and the honest
    // answer to it is the same skip an unknown generation gets.
    //
    // The one case that stays a refusal is content read LONGER than declared. A reader that ran past
    // the end it was handed did not meet a newer format; it lost its place, and every byte after it
    // means something else.

    /// <summary> The runtime half of a generated root's envelope read: settle the declared length,
    /// skip what this build cannot read, and migrate what it can. </summary>
    public static class BlobEnvelopes
    {
        /// <summary> Settles an ordinary content read against its declared length. Short content is a future
        /// build's appended members and is skipped; long content is damage and is refused. </summary>
        public static void Finish(ref BlobReader reader, string domain, int contentStart, int length)
        {
            var read = reader.Position - contentStart;
            if (read == length) return;

            if (read > length)
                throw new BlobFormatException(
                    $"{domain} read {read} bytes of the {length} it declared - the payload is damaged, not newer");

            reader.Skip(length - read);
            SerializationReport.Report(domain, domain, $"{length - read} trailing bytes this build does not read",
                ModelGenerations.Invalid, SubstitutionKind.ShortContent);
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

        // A SNAPSHOT IS READ THROUGH IBinaryEnvelope RATHER THAN IBinaryModel, and the reason is one
        // line up: the envelope this method is answering has already been consumed, so the snapshot's
        // own Read would look for a second one. See IBinaryEnvelope's header.
        //
        // A ref struct may be passed BY REF to an interface method; what it may not do is be captured
        // or boxed, and nothing here does either.

        /// <summary> Reads a root written at another generation: migrates it when the chain reaches today's
        /// shape, and otherwise skips the root whole. Null means the caller resets itself. </summary>
        public static T OtherGeneration<T>(ref BlobReader reader, string domain, int generation,
            int contentStart, int length) where T : class
        {
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

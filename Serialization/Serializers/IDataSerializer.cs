using System;
using BH.SDK.Versions;

namespace BH.SDK.Serialization.Serializers
{
    // Format-agnostic envelope contract (requirement 3, see VERSION-UPDATE.md "Format-agnosticism").
    // Only the envelope read/write step is format-specific - resolving a version tag to a concrete
    // type and walking the migration chain to a domain's current shape both live in
    // BH.SDK.Versions.VersionedTypeRegistry and are shared by every implementation of this
    // interface. JsonDataSerializer and BlobDataSerializer are today's implementations; a future XML
    // one would implement the same contract without touching VersionedTypeRegistry at all.
    public interface IDataSerializer
    {
        public SerializationType Type { get; }

        public byte[] SerializeEnvelope(string domain, EnvelopeData data);
        public EnvelopeData DeserializeEnvelope(byte[] data, Type payloadType);
    }
}

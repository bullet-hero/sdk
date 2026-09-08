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

    /// <summary> One wire format. Only the envelope's bytes are its own - resolving a version and migrating
    /// to today's shape is shared by every implementation. </summary>
    public interface IDataSerializer
    {
        /// <summary> Which format this is. </summary>
        public SerializationType Type { get; }

        /// <summary> Writes one domain's payload, tagged with the version it was written at. </summary>
        public byte[] SerializeEnvelope(string domain, EnvelopeData data);

        /// <summary> Reads one back, already upgraded to the domain's current shape. </summary>
        public EnvelopeData DeserializeEnvelope(byte[] data, Type payloadType);
    }
}

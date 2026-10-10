using System;
using BH.SDK.Versions;

namespace BH.SDK.Serialization.Serializers
{
    // Format-agnostic envelope contract: only the envelope step is format-specific; tag resolution and
    // the migration walk live in VersionedTypeRegistry for every implementation (JSON and Blob today).

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

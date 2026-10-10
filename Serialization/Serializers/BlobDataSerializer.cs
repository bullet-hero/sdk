using System;
using System.Reflection;
using BH.SDK.Serialization.Blob;
using BH.SDK.Versions;

namespace BH.SDK.Serialization.Serializers
{
    // Not where versions are answered: this checks the FILE (magic, codec generation, length, hash,
    // nothing left over); migrate, degrade and skip belong to the generated root read, which reaches
    // VersionedTypeRegistry from BlobEnvelopes. Docs/SERIALIZATION.md.

    /// <summary> Reads and writes the binary level format. </summary>
    public sealed class BlobDataSerializer : IDataSerializer
    {
        /// <summary> A level's object tree is most of its bytes, so the buffer starts big enough
        /// that a small level never grows it and a large one doubles a handful of times. </summary>
        private const int InitialCapacity = 64 * 1024;

        /// <summary> Which format this writes. </summary>
        public SerializationType Type => SerializationType.Blob;

        /// <summary> Header, then the payload written by the model's own generated codec. </summary>
        public byte[] SerializeEnvelope(string domain, EnvelopeData data)
        {
            if (data.RawPayload == null) return Array.Empty<byte>();

            var payloadType = data.RawPayload.GetType();
            var attribute = payloadType.GetCustomAttribute<ModelGenerationAttribute>();
            if (attribute == null || attribute.Domain != domain || attribute.Generation != data.Generation)
            {
                throw new ArgumentException(
                    $"Payload of type '{payloadType}' does not match domain '{domain}' generation {data.Generation}",
                    nameof(data.RawPayload));
            }

            if (!(data.RawPayload is IBinaryModel model))
            {
                throw new ArgumentException(
                    $"'{payloadType}' carries [ModelGeneration] but no blob codec - it is not [GenerateModel]",
                    nameof(data.RawPayload));
            }

            var payload = new BlobWriter(InitialCapacity);
            model.Write(ref payload);

            // The header carries the payload's length and hash, so it is written second and the two
            // are joined - rather than reserving space and patching, which would hash a buffer the
            // header is still being written into.
            var file = new BlobWriter(payload.Length + BlobFormat.HeaderLength);
            BlobFormat.WriteHeader(ref file, payload.Length, BlobFormat.Hash(payload.AsSpan()));
            var bytes = payload.ToArray();
            file.WriteBytes(bytes, 0, bytes.Length);
            return file.ToArray();
        }

        /// <summary> Checks the header through before decoding a byte, and refuses an older generation outright. </summary>
        public EnvelopeData DeserializeEnvelope(byte[] data, Type payloadType)
        {
            var offset = BlobFormat.ReadHeader(data, out var payloadLength);

            var attribute = payloadType.GetCustomAttribute<ModelGenerationAttribute>();
            if (attribute == null)
                throw new ArgumentException($"'{payloadType}' carries no [ModelGeneration]", nameof(payloadType));

            var instance = Activator.CreateInstance(payloadType);
            if (!(instance is IBinaryModel model))
            {
                throw new ArgumentException(
                    $"'{payloadType}' carries [ModelGeneration] but no blob codec - it is not [GenerateModel]",
                    nameof(payloadType));
            }

            var reader = new BlobReader(data, offset, payloadLength);
            model.Read(ref reader);

            if (reader.Remaining != 0)
                throw new BlobFormatException($"{reader.Remaining} bytes left over after the payload");

            return new EnvelopeData(attribute.Generation, instance);
        }
    }
}

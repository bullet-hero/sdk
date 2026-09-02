using System;
using System.Reflection;
using BH.SDK.Serialization.Blob;
using BH.SDK.Versions;

namespace BH.SDK.Serialization.Serializers
{
    // THE THIRD IMPLEMENTATION OF A CONTRACT WRITTEN FOR EXACTLY THIS. IDataSerializer's own header
    // says a future format would slot in without touching VersionedTypeRegistry, and this is that
    // format - except that it does not use the registry at all, and the reason is worth stating:
    // a JSON envelope has to resolve a version to a historical TYPE because Newtonsoft binds
    // members by name and a snapshot class is how an old shape is described. A blob's payload is
    // read by generated code that is the type, so an old generation is not something this build can
    // decode at all. The version tags are written anyway - every envelope carries its domain and
    // its major.minor - so the day a domain bumps, the machinery has somewhere to attach.
    //
    // NO .blob OF AN OLDER GENERATION CAN EXIST, because no build has ever written one. That is why
    // refusing is honest here rather than a gap: the .json beside it is the recovery path, and it
    // is the format the project promises longevity for.

    /// <summary> Reads and writes the binary level format. </summary>
    public sealed class BlobDataSerializer : IDataSerializer
    {
        /// <summary> A level's object tree is most of its bytes, so the buffer starts big enough
        /// that a small level never grows it and a large one doubles a handful of times. </summary>
        private const int InitialCapacity = 64 * 1024;

        public SerializationType Type => SerializationType.Blob;

        public byte[] SerializeEnvelope(string domain, EnvelopeData data)
        {
            if (data.RawPayload == null) return Array.Empty<byte>();

            var payloadType = data.RawPayload.GetType();
            var attribute = payloadType.GetCustomAttribute<DataVersionAttribute>();
            if (attribute == null || attribute.Domain != domain || attribute.Version != data.Version)
            {
                throw new ArgumentException(
                    $"Payload of type '{payloadType}' does not match domain '{domain}' version {data.Version}",
                    nameof(data.RawPayload));
            }

            if (!(data.RawPayload is IBinaryModel model))
            {
                throw new ArgumentException(
                    $"'{payloadType}' carries [DataVersion] but no blob codec - it is not [GenerateModel]",
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

        public EnvelopeData DeserializeEnvelope(byte[] data, Type payloadType)
        {
            var offset = BlobFormat.ReadHeader(data, out var payloadLength);

            var attribute = payloadType.GetCustomAttribute<DataVersionAttribute>();
            if (attribute == null)
                throw new ArgumentException($"'{payloadType}' carries no [DataVersion]", nameof(payloadType));

            var instance = Activator.CreateInstance(payloadType);
            if (!(instance is IBinaryModel model))
            {
                throw new ArgumentException(
                    $"'{payloadType}' carries [DataVersion] but no blob codec - it is not [GenerateModel]",
                    nameof(payloadType));
            }

            var reader = new BlobReader(data, offset, payloadLength);
            model.Read(ref reader);

            if (reader.Remaining != 0)
                throw new BlobFormatException($"{reader.Remaining} bytes left over after the payload");

            return new EnvelopeData(attribute.Version, instance);
        }
    }
}

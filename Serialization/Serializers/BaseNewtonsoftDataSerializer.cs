using System;
using System.IO;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Serialization.Serializers
{
    // Shared envelope logic for every Newtonsoft-backed IDataSerializer. The converters touch only the
    // abstract JsonWriter/JsonReader, so subclasses supply just the raw reader/writer over the bytes.

    /// <summary> The envelope half every Newtonsoft-backed format shares; a subclass supplies only the
    /// reader and writer over the bytes. </summary>
    public abstract class BaseNewtonsoftDataSerializer : IDataSerializer
    {
        private readonly JsonSerializer _serializer;

        /// <summary> Takes the serializer carrying the whole converter stack. </summary>
        protected BaseNewtonsoftDataSerializer(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary> Which format this writes. </summary>
        public abstract SerializationType Type { get; }

        /// <summary> The writer this format lays bytes down through. </summary>
        protected abstract JsonWriter CreateWriter(Stream stream);

        /// <summary> The reader this format takes bytes back through. </summary>
        protected abstract JsonReader CreateReader(Stream stream);

        /// <summary> Writes one envelope, refusing a payload whose own <c>[ModelGeneration]</c> does not match what it claims. </summary>
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

            using var stream = new MemoryStream();
            using (var writer = CreateWriter(stream))
                _serializer.Serialize(writer, data.RawPayload);
            return stream.ToArray();
        }

        // Two streaming passes, not one materialized tree: the first stops at the generation (it types
        // the payload), only the second reads content.

        /// <summary>
        /// Reads one envelope and returns it already upgraded to the domain's current shape; "raw" only
        /// means handed back untyped.
        /// </summary>
        public EnvelopeData DeserializeEnvelope(byte[] data, Type payloadType)
        {
            VersionedTypeRegistry.ThrowIfNoDomain(payloadType);

            var generation = ReadGeneration(data, payloadType);

            using var stream = new MemoryStream(data);
            using var reader = CreateReader(stream);
            var rawPayload = _serializer.Deserialize(reader, payloadType);

            return new EnvelopeData(generation, rawPayload);
        }

        private int ReadGeneration(byte[] data, Type payloadType)
        {
            using var stream = new MemoryStream(data);
            using var reader = CreateReader(stream);

            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Expected a versioned envelope for type '{payloadType}'");

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var isGeneration = (string)reader.Value == Names.Generation;
                if (!reader.Read()) break;

                // Skip() rather than a full read: everything before the generation is content this
                // pass has no use for, and the ordinary document has nothing there at all.
                if (!isGeneration)
                {
                    reader.Skip();
                    continue;
                }
                var generation = _serializer.Deserialize<int>(reader);
                VersionedTypeRegistry.ThrowIfNewer(VersionedTypeRegistry.GetDomain(payloadType), generation);
                return generation;
            }

            throw new JsonSerializationException($"Missing '{Names.Generation}' property for type '{payloadType}'");
        }
    }
}
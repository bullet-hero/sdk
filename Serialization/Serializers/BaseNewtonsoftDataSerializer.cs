using System;
using System.IO;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Serialization.Serializers
{
    // Shared envelope logic for every Newtonsoft-backed IDataSerializer. None of the converters in
    // Serialization/Converters/* touch JsonTextWriter/JsonTextReader specifically - they only use
    // members of the abstract JsonWriter/JsonReader - so the same JsonSerializer (and therefore the
    // same VersionedEnvelopeConverter chain) works unchanged for both JSON and BSON. Only the raw
    // reader/writer over the byte stream differs per format, which is what subclasses supply.
    // See VERSION-UPDATE.md, "Format-agnosticism".
    public abstract class BaseNewtonsoftDataSerializer : IDataSerializer
    {
        private readonly JsonSerializer _serializer;

        protected BaseNewtonsoftDataSerializer(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public abstract SerializationType Type { get; }

        protected abstract JsonWriter CreateWriter(Stream stream);
        protected abstract JsonReader CreateReader(Stream stream);

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

            using var stream = new MemoryStream();
            using (var writer = CreateWriter(stream))
                _serializer.Serialize(writer, data.RawPayload);
            return stream.ToArray();
        }

        // Two streaming passes over the bytes, not one materialized tree. The version has to be
        // known before the payload can be typed, and reading it used to mean loading the WHOLE
        // document into a JObject and then walking that tree a second time to deserialize - so a
        // level was parsed twice, the second time out of a tree that cost one JToken per value.
        // The first pass here stops at the version property; only the second one reads content.
        //
        // Both passes are format-agnostic: subclasses supply the reader, and nothing below it cares
        // whether the bytes are JSON or BSON.

        /// <summary>
        /// Reads one envelope. VersionedEnvelopeConverter resolves the concrete historical type for
        /// the version it finds and upgrades it to the domain's current shape in one step, so what
        /// comes back is already current-shape - "raw" only means handed back untyped.
        /// </summary>
        public EnvelopeData DeserializeEnvelope(byte[] data, Type payloadType)
        {
            VersionedTypeRegistry.ThrowIfNoDomain(payloadType);

            var version = ReadVersion(data, payloadType);

            using var stream = new MemoryStream(data);
            using var reader = CreateReader(stream);
            var rawPayload = _serializer.Deserialize(reader, payloadType);

            return new EnvelopeData(version, rawPayload);
        }

        private Version ReadVersion(byte[] data, Type payloadType)
        {
            using var stream = new MemoryStream(data);
            using var reader = CreateReader(stream);

            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Expected a versioned envelope for type '{payloadType}'");

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var isVersion = (string)reader.Value == Names.Version;
                if (!reader.Read()) break;

                // Skip() rather than a full read: everything before the version is content this
                // pass has no use for, and the ordinary document has nothing there at all.
                if (!isVersion)
                {
                    reader.Skip();
                    continue;
                }
                return _serializer.Deserialize<Version>(reader);
            }

            throw new JsonSerializationException($"Missing '{Names.Version}' property for type '{payloadType}'");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Serialization.Converters
{
    // Single converter for every SaveData kind and any internal aggregate that opts into
    // versioning via [DataVersion]. Replaces the old per-kind JsonConverterData<T> subclasses and
    // CompatibilityService entirely - see VERSION-UPDATE.md. CanConvert is gated purely on the
    // attribute being present, so this recurses correctly into nested aggregates without any
    // special-casing for "aggregated vs non-aggregated" models.
    public class VersionedEnvelopeConverter : JsonConverter
    {
        // Domains currently being written/read one level up the call stack. Suppresses CanConvert
        // just for that domain so the serializer.Serialize/ToObject calls below - which re-enter this
        // same converter for the exact value being wrapped - fall through to plain member
        // serialization instead of re-wrapping it in another envelope. A differently-domained nested
        // aggregate (e.g. GameLevel's own [DataVersion] inside Level) is a different domain, so it
        // stays convertible and recurses into this converter normally - this is how "aggregated"
        // domains get every nested envelope written/upgraded without any special-casing.
        private readonly HashSet<string> _activeDomains = new();

        public override bool CanConvert(Type objectType) =>
            VersionedTypeRegistry.CanConvert(objectType) && !_activeDomains.Contains(VersionedTypeRegistry.GetDomain(objectType));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var type = value.GetType();
            var attribute = type.GetCustomAttribute<DataVersionAttribute>();

            writer.WriteStartObject();

            // serialize version
            writer.WritePropertyName(Names.Version);
            serializer.Serialize(writer, attribute.Version);

            // THE PAYLOAD IS HANDED STRAIGHT TO ITS OWN WRITER when it has one, and that is not an
            // optimisation - it is the only way this works. Going back through the serializer for
            // the SAME instance trips Newtonsoft's own circular-reference check: the value is
            // already on its serialize stack, pushed by the call that reached this converter, and
            // a second converter for it is a second push. It never fired while the inner call
            // landed on the contract, and fired immediately once a converter answered for it.
            //
            // The _activeDomains dance below is still needed for the other path: a historical
            // snapshot is deliberately NOT a generated model, so it is read and written reflectively
            // and would otherwise re-enter this converter and wrap itself twice.
            writer.WritePropertyName(Names.Value);
            if (value is Json.IJsonModel model)
            {
                model.WriteJson(writer);
            }
            else
            {
                _activeDomains.Add(attribute.Domain);
                serializer.Serialize(writer, value);
                _activeDomains.Remove(attribute.Domain);
            }

            writer.WriteEndObject();
        }

        // Read straight off the reader, one envelope property at a time, instead of loading the
        // envelope into a JObject first. The old shape cost a materialized JToken tree PER DOMAIN,
        // and domains nest: a Level's own tree was cloned again for GameLevel, again for each of the
        // four event aggregates, and again for every Prefab in its resources - each clone one JToken
        // per value in that subtree. WriteJson emits the version first, so the ordinary document
        // needs nothing buffered at all; a document that happens to carry the value first (hand
        // edited, or written by another tool) is still read correctly, by buffering that one subtree
        // until the version that types it arrives.
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Expected start of versioned envelope for '{objectType}'");

            var domain = VersionedTypeRegistry.GetDomain(objectType);

            var hasVersion = false;
            Version version = default;
            object raw = null;
            JToken pendingValue = null;

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = (string)reader.Value;
                if (!reader.Read())
                    throw new JsonSerializationException($"Truncated versioned envelope for domain '{domain}'");

                if (propertyName == Names.Version)
                {
                    version = serializer.Deserialize<Version>(reader);
                    hasVersion = true;

                    if (pendingValue != null)
                    {
                        raw = ReadPayload(pendingValue, domain, version, serializer);
                        pendingValue = null;
                    }
                }
                else if (propertyName == Names.Value)
                {
                    if (hasVersion) raw = ReadPayload(reader, domain, version, serializer);
                    else pendingValue = JToken.Load(reader);
                }
                else reader.Skip();
            }

            if (!hasVersion)
                throw new JsonSerializationException($"Missing '{Names.Version}' property for domain '{domain}'");

            return VersionedTypeRegistry.UpgradeToLatest(domain, raw, version.Major, version.Minor);
        }

        private object ReadPayload(JsonReader reader, string domain, Version version, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var concreteType = VersionedTypeRegistry.Resolve(domain, version.Major, version.Minor);

            _activeDomains.Add(domain);
            var raw = serializer.Deserialize(reader, concreteType);
            _activeDomains.Remove(domain);
            return raw;
        }

        private object ReadPayload(JToken token, string domain, Version version, JsonSerializer serializer)
        {
            if (token == null || token.Type == JTokenType.Null) return null;

            var concreteType = VersionedTypeRegistry.Resolve(domain, version.Major, version.Minor);

            _activeDomains.Add(domain);
            var raw = token.ToObject(concreteType, serializer);
            _activeDomains.Remove(domain);
            return raw;
        }
    }
}

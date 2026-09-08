using System;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Serialization.Converters
{
    // Single converter for every aggregate root and any internal aggregate that opts into
    // versioning via [ModelGeneration]. Replaces the old per-kind JsonConverterData<T> subclasses
    // and CompatibilityService entirely - see Docs/VERSIONING.md. CanConvert is gated purely on the
    // attribute being present, so this recurses correctly into nested aggregates without any
    // special-casing for "aggregated vs non-aggregated" models.
    //
    // THE ENVELOPE'S KEY IS ITS OWN, and deliberately not Names.Version: that one is the AUTHOR's
    // version of a level (LevelMeta/BestRun/LevelStatistics), which is a System.Version written as
    // a string. Sharing "vrs" would put {"vrs":1} and {"vrs":"1.0"} in one file meaning two
    // different things.

    /// <summary> Wraps every <c>[ModelGeneration]</c> domain as <c>{g, v}</c>, and on the way back in resolves
    /// that generation to its historical snapshot type and walks the migration chain up to today's shape. </summary>
    public class VersionedEnvelopeConverter : JsonConverter
    {
        // Domains currently being written/read one level up the call stack. Suppresses CanConvert
        // just for that domain so the serializer.Serialize/ToObject calls below - which re-enter this
        // same converter for the exact value being wrapped - fall through to plain member
        // serialization instead of re-wrapping it in another envelope. A differently-domained nested
        // aggregate (e.g. GameLevel's own [ModelGeneration] inside Level) is a different domain, so it
        // stays convertible and recurses into this converter normally - this is how "aggregated"
        // domains get every nested envelope written/upgraded without any special-casing.
        private readonly HashSet<string> _activeDomains = new();

        /// <summary> True for a versioning boundary - except the domain being written one level up, or every envelope would wrap itself forever. </summary>
        public override bool CanConvert(Type objectType) =>
            VersionedTypeRegistry.CanConvert(objectType) && !_activeDomains.Contains(VersionedTypeRegistry.GetDomain(objectType));

        /// <summary> Wraps the payload as <c>{g, v}</c>, at the domain's current generation. </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var type = value.GetType();
            var attribute = type.GetCustomAttribute<ModelGenerationAttribute>();

            writer.WriteStartObject();

            // serialize generation
            writer.WritePropertyName(Names.Generation);
            writer.WriteValue(attribute.Generation);

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
        // per value in that subtree. WriteJson emits the generation first, so the ordinary document
        // needs nothing buffered at all; a document that happens to carry the value first (hand
        // edited, or written by another tool) is still read correctly, by buffering that one subtree
        // until the generation that types it arrives.

        /// <summary> Resolves the generation tag to its snapshot type, reads that, and walks the migration chain up to today's shape. </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Expected start of versioned envelope for '{objectType}'");

            var domain = VersionedTypeRegistry.GetDomain(objectType);

            // ZERO IS A REAL GENERATION - the frozen snapshots under Versions/V0 are written at it -
            // so "did we read one" cannot be asked of the value itself. The flag is what asks, and
            // Invalid is what an unread envelope holds.
            var hasGeneration = false;
            var generation = ModelGenerations.Invalid;
            object raw = null;
            JToken pendingValue = null;

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = (string)reader.Value;
                if (!reader.Read())
                    throw new JsonSerializationException($"Truncated versioned envelope for domain '{domain}'");

                if (propertyName == Names.Generation)
                {
                    generation = serializer.Deserialize<int>(reader);
                    hasGeneration = true;

                    if (pendingValue != null)
                    {
                        raw = ReadPayload(pendingValue, domain, generation, serializer);
                        pendingValue = null;
                    }
                }
                else if (propertyName == Names.Value)
                {
                    if (hasGeneration) raw = ReadPayload(reader, domain, generation, serializer);
                    else pendingValue = JToken.Load(reader);
                }
                else reader.Skip();
            }

            if (!hasGeneration)
                throw new JsonSerializationException($"Missing '{Names.Generation}' property for domain '{domain}'");

            return VersionedTypeRegistry.UpgradeToLatest(domain, raw, generation);
        }

        private object ReadPayload(JsonReader reader, string domain, int generation, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var concreteType = VersionedTypeRegistry.Resolve(domain, generation);

            _activeDomains.Add(domain);
            var raw = serializer.Deserialize(reader, concreteType);
            _activeDomains.Remove(domain);
            return raw;
        }

        private object ReadPayload(JToken token, string domain, int generation, JsonSerializer serializer)
        {
            if (token == null || token.Type == JTokenType.Null) return null;

            var concreteType = VersionedTypeRegistry.Resolve(domain, generation);

            _activeDomains.Add(domain);
            var raw = token.ToObject(concreteType, serializer);
            _activeDomains.Remove(domain);
            return raw;
        }
    }
}

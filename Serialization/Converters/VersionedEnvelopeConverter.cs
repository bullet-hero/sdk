using System;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Serialization.Converters
{
    // The single converter for every [ModelGeneration] aggregate, gated on the attribute alone, so
    // nested aggregates recurse without special-casing. Its key is Names.Generation, NOT Names.Version
    // (the author's System.Version string): sharing one key puts two meanings in one file.

    /// <summary> Wraps every <c>[ModelGeneration]</c> domain as <c>{g, v}</c>, and on the way back in resolves
    /// that generation to its historical snapshot type and walks the migration chain up to today's shape. </summary>
    public class VersionedEnvelopeConverter : JsonConverter
    {
        // Domains being written/read up the stack: CanConvert is suppressed for just that domain so the
        // inner serialize call writes plain members; a different nested domain still recurses.
        private readonly HashSet<string> _activeDomains = new();

        /// <summary> True for a versioning boundary - except the domain being written one level up, or every envelope would wrap itself forever. </summary>
        public override bool CanConvert(Type objectType) =>
            VersionedTypeRegistry.CanConvert(objectType) &&
            !_activeDomains.Contains(VersionedTypeRegistry.GetDomain(objectType));

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

            // The payload goes STRAIGHT to its own writer when it has one: routing the same instance back
            // through the serializer trips Newtonsoft's circular-reference check. _activeDomains below
            // guards anything reaching this converter without a generated codec.
            writer.WritePropertyName(Names.Value);
            if (value is Json.IJsonModel model)
            {
                model.WriteJson(writer);
            }
            else
            {
                _activeDomains.Add(attribute.Domain);
                try
                {
                    serializer.Serialize(writer, value);
                }
                finally
                {
                    _activeDomains.Remove(attribute.Domain);
                }
            }

            writer.WriteEndObject();
        }

        // Read off the reader one envelope property at a time, never into a JObject (a tree per nested
        // domain). A document carrying the value before the generation buffers just that one subtree.

        /// <summary> Resolves the generation tag to its snapshot type, reads that, and walks the migration chain up to today's shape. </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
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
            {
                SerializationReport.Report(domain, objectType.Name,
                    $"no '{Names.Generation}' at all; read as today's shape", ModelGenerations.Invalid,
                    SubstitutionKind.AbsentGeneration);

                if (pendingValue != null) raw = ReadPayload(pendingValue, domain, generation, serializer);
            }

            if (raw == null) return null;

            // Nothing resolved means no chain to walk: the payload was already read by property name (the
            // lossy half working). Walking would report a second finding and make the two stacks disagree.
            if (VersionedTypeRegistry.TryResolve(domain, generation) == null)
                return objectType.IsInstanceOfType(raw) ? raw : Activator.CreateInstance(objectType);

            if (VersionedTypeRegistry.TryUpgradeToLatest(domain, raw, generation, out var upgraded)
                && objectType.IsInstanceOfType(upgraded))
                return upgraded;

            // The chain stopped short, so what is in hand is a shape nothing above can use. It became
            // a default instance rather than an exception for the same reason everything else here
            // did: the file opens, and the loss is reported instead of thrown.
            SerializationReport.Report(domain, objectType.Name, "left at its defaults", generation,
                SubstitutionKind.IncompleteChain);
            return Activator.CreateInstance(objectType);
        }

        // Released in a finally: a throw mid-payload would leave the domain in _activeDomains for the
        // life of this converter and turn CanConvert off for every later envelope of it.

        private object ReadPayload(JsonReader reader, string domain, int generation, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            VersionedTypeRegistry.ThrowIfNewer(domain, generation);
            var concreteType = VersionedTypeRegistry.Resolve(domain, generation);

            _activeDomains.Add(domain);
            try
            {
                return serializer.Deserialize(reader, concreteType);
            }
            finally
            {
                _activeDomains.Remove(domain);
            }
        }

        private object ReadPayload(JToken token, string domain, int generation, JsonSerializer serializer)
        {
            if (token == null || token.Type == JTokenType.Null) return null;

            VersionedTypeRegistry.ThrowIfNewer(domain, generation);
            var concreteType = VersionedTypeRegistry.Resolve(domain, generation);

            _activeDomains.Add(domain);
            try
            {
                return token.ToObject(concreteType, serializer);
            }
            finally
            {
                _activeDomains.Remove(domain);
            }
        }
    }
}

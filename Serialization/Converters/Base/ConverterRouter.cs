using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    /// <summary> One converter standing in for many, resolving which of them handles a type once. </summary>
    public sealed class ConverterRouter : JsonConverter
    {
        // ConcurrentDictionary cannot store null, and "nothing handles this type" is the answer worth
        // caching most - every plain model class lands on it. Hence a sentinel rather than absence.
        private static readonly JsonConverter NoConverter = new UnroutedConverter();

        private readonly JsonConverter[] _converters;
        private readonly ConcurrentDictionary<Type, JsonConverter> _resolved = new();

        /// <summary> Takes the converters to route between, first match per type winning. </summary>
        public ConverterRouter(IEnumerable<JsonConverter> converters)
        {
            _converters = converters.ToArray();
        }

        /// <summary> True when any routed converter claims the type - resolved once per type and cached, sentinel included. </summary>
        public override bool CanConvert(Type objectType) => Resolve(objectType) != null;

        /// <summary> Hands the value to whichever converter claimed its type. </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var converter = Resolve(objectType);
            if (converter == null)
                throw new JsonSerializationException($"No converter is routed for type '{objectType}'");

            return converter.ReadJson(reader, objectType, existingValue, serializer);
        }

        // Resolved off the value's own type, which is what Newtonsoft asked CanConvert about on this
        // path too: a property declared as a base type is written against the contract of whatever it
        // actually holds, so routing by the declared type here would answer a different question.

        /// <summary> The same on the way out. </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var objectType = value.GetType();
            var converter = Resolve(objectType);
            if (converter == null)
                throw new JsonSerializationException($"No converter is routed for type '{objectType}'");

            converter.WriteJson(writer, value, serializer);
        }

        private JsonConverter Resolve(Type objectType)
        {
            if (_resolved.TryGetValue(objectType, out var cached))
                return ReferenceEquals(cached, NoConverter) ? null : cached;

            JsonConverter match = null;
            foreach (var converter in _converters)
            {
                if (!converter.CanConvert(objectType)) continue;
                match = converter;
                break;
            }

            _resolved[objectType] = match ?? NoConverter;
            return match;
        }

        /// <summary> Cache entry meaning "no converter handles this type"; never invoked. </summary>
        private sealed class UnroutedConverter : JsonConverter
        {
            /// <summary> Never - this is the "nothing handles it" sentinel, since the cache cannot store a null. </summary>
            public override bool CanConvert(Type objectType) => false;

            /// <summary> Hands the value to whichever converter claimed its type. </summary>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer) => throw new NotSupportedException();

            /// <summary> The same on the way out. </summary>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                => throw new NotSupportedException();
        }
    }
}

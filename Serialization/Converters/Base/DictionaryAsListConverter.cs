using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    /// <summary> Writes a dictionary as a plain array of its values, for the case where a value already
    /// carries its own key. Halves what a keyed object costs on the wire, and reads back refusing
    /// duplicates rather than letting the later one win silently. </summary>
    public abstract class DictionaryAsListConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
    {
        /// <summary> Writes the values alone; each already carries its key. </summary>
        public override void WriteJson(JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializer serializer)
        {
            // var sorted = value.Values.OrderBy(v => GetKey(v)).ToList();
            serializer.Serialize(writer, value.Values);
        }
        /// <summary> Rebuilds the dictionary from that array, refusing a duplicate key rather than letting the later one win. </summary>
        public override Dictionary<TKey, TValue> ReadJson(JsonReader reader, Type objectType,
            Dictionary<TKey, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Expected array to deserialize dictionary.");
            
            var values = serializer.Deserialize<List<TValue>>(reader);
            var dict = new Dictionary<TKey, TValue>(values.Count);
            
            foreach (var value in values)
            {
                var key = GetKey(value);
                if (key == null)
                    throw new JsonSerializationException($"Key returned null for an item");
                if (!dict.TryAdd(key, value))
                    throw new JsonSerializationException($"Duplicate key '{key}' found when building dictionary");
            }

            return dict;
        }

        /// <summary> Where the key lives inside the value. </summary>
        protected abstract TKey GetKey(TValue value);
    }
}
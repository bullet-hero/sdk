using System;
using System.Collections.Generic;
using BH.SDK.Models;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    // For a Dictionary<TKey, TValue> whose key cannot be derived from the value (unlike
    // DictionaryAsListConverter): an array of {k, v} pairs, since Newtonsoft's string-key form throws for
    // value-type keys with no TypeConverter. Registered closed in SerializationService.GetConverters().

    /// <summary> Writes a dictionary as an array of key/value pairs, for the case where the key cannot be
    /// recovered from the value. </summary>
    public class DictionaryAsPairListConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
    {
        /// <summary> One entry on the wire. </summary>
        private struct Pair
        {
            [JsonProperty(Names.KeyShort)] public TKey K;
            [JsonProperty(Names.ValueShort)] public TValue V;
        }

        /// <summary> Writes an array of key/value pairs, since the key cannot be recovered from the value. </summary>
        public override void WriteJson(JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializer serializer)
        {
            var list = new List<Pair>(value.Count);
            foreach (var pair in value)
                list.Add(new Pair { K = pair.Key, V = pair.Value });
            serializer.Serialize(writer, list);
        }
        /// <summary> Rebuilds the dictionary from those pairs, refusing a duplicate key. </summary>
        public override Dictionary<TKey, TValue> ReadJson(JsonReader reader, Type objectType,
            Dictionary<TKey, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Expected array to deserialize dictionary.");

            var list = serializer.Deserialize<List<Pair>>(reader);
            var dict = new Dictionary<TKey, TValue>(list.Count);

            foreach (var pair in list)
            {
                if (!dict.TryAdd(pair.K, pair.V))
                    throw new JsonSerializationException($"Duplicate key '{pair.K}' found when building dictionary");
            }

            return dict;
        }
    }
}

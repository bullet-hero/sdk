using System;
using System.Collections.Generic;
using BH.SDK.Models;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    // For a plain Dictionary<TKey, TValue> where TKey can NOT be derived from TValue (unlike
    // DictionaryAsListConverter, which assumes the value embeds its own key, e.g. RectObject.ObjectId) -
    // serializes as an array of {k, v} pair objects instead of relying on Newtonsoft's default
    // string-key dictionary serialization, which throws for value-type keys like ObjectId that have
    // no working TypeConverter. Closed-generic instances are registered directly in
    // SerializationService.GetConverters() - no per-usage subclass needed.
    public class DictionaryAsPairListConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
    {
        private struct Pair
        {
            [JsonProperty(Names.KeyShort)] public TKey K;
            [JsonProperty(Names.ValueShort)] public TValue V;
        }

        public override void WriteJson(JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializer serializer)
        {
            var list = new List<Pair>(value.Count);
            foreach (var pair in value)
                list.Add(new Pair { K = pair.Key, V = pair.Value });
            serializer.Serialize(writer, list);
        }
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

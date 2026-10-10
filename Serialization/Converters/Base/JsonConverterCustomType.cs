using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    /// <summary> The wire shape every polymorphic value in this format takes: <c>[type, value]</c>, where the
    /// tag is the interface's own discriminator enum and the payload is the concrete class's plain members. </summary>
    public abstract class JsonConverterCustomType<T, TType> : JsonConverter<T>, IRequiresDefaultSerializer
    {
        /// <summary> The serializer used for the payload - this converter excluded, so it does not re-wrap it. </summary>
        public JsonSerializer SerializerDefault { get; private set; }

        void IRequiresDefaultSerializer.SetDefaultSerializer(JsonSerializer serializer) =>
            SerializerDefault = serializer;

        // Only this converter itself needs excluding: it's the one that would otherwise re-wrap the
        // resolved concrete type's plain member JSON (e.g. Color4Key's own fields) as another [type, value] array.
        IEnumerable<JsonConverter> IRequiresDefaultSerializer.GetExcludedConverters(
            IReadOnlyList<JsonConverter> allConverters) =>
            new JsonConverter[] { this };

        // The type tag is written directly, not through a serializer: a modest win on Resources,
        // nothing on the objects tree. The wire format is unchanged (the enum is an integer).
        // Measurements and the real cost, the per-value dispatch: Docs/SERIALIZATION.md.

        // The time is in the per-value DISPATCH (ConverterRouter plus Newtonsoft's invocation path), not
        // here: hand-writing the members of the common values measured at nothing and was rejected.
        // Measure the dispatch before optimizing this call (Docs/SERIALIZATION.md).

        /// <summary> Writes the tag, then the concrete class's own members - the tag directly rather than through a serializer, which is measurably cheaper for one enum. </summary>
        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();

            // writer.WritePropertyName(Names.TypeShort);
            writer.WriteValue(GetCustomTypeIndex(value));

            // writer.WritePropertyName(Names.ValueShort);
            // serialize via another serializer, it must NOT include this instance of converter
            SerializerDefault.Serialize(writer, value);

            writer.WriteEndArray();
        }

        /// <summary> Reads the tag, resolves the class it names, and populates it through the serializer that excludes this converter. </summary>
        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return default;
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Expected StartArray");

            reader.Read(); // to first array element, it's type (as "t")
            var customType = ReadCustomType(reader);

            reader.Read(); // to second array element, it's value (as "v")
            var targetType = GetType(customType);
            var value = (T)SerializerDefault.Deserialize(reader, targetType);

            reader.Read(); // to EndArray
            if (reader.TokenType != JsonToken.EndArray)
                throw new JsonSerializationException("Expected EndArray");

            return value;
        }

        /// <summary> The tag written for a value. </summary>
        public abstract TType GetCustomType(T value);

        /// <summary> The concrete class a tag names, or null when nothing does. </summary>
        public abstract Type GetType(TType customType);

        // The fallback is the family's LOWEST tag, every family's plain literal form. An unknown OBJECT
        // becomes an inert RectObject, not a skipped entry: skipping would orphan its children and the
        // runtime does not draw an orphan. An unknown VALUE is invisible, so the report is not optional.

        /// <summary> Stands the family's lowest tag in for one this build has no type for, and reports it. </summary>
        protected Type Fallback(TType customType, Type fallback)
        {
            SerializationReport.Report(typeof(T).Name, customType.ToString(), fallback.Name,
                ModelGenerations.Invalid, SubstitutionKind.UnknownTag);
            return fallback;
        }

        // TType is an enum on every converter built on this, but the signature cannot say so without
        // constraining all of them, so this boxes once per value. Override with an (int) cast if hot.

        /// <summary> The type tag as the number the wire format carries. </summary>
        protected virtual int GetCustomTypeIndex(T value) => Convert.ToInt32(GetCustomType(value));

        /// <summary> The tag the reader is currently sitting on. Accepts the name as well as the
        /// number: `Deserialize` used to read this, and it would have taken either. </summary>
        protected virtual TType ReadCustomType(JsonReader reader)
        {
            var value = reader.Value;
            if (value is string name)
                return (TType)Enum.Parse(typeof(TType), name, ignoreCase: true);

            return (TType)Enum.ToObject(typeof(TType), Convert.ToInt32(value));
        }
    }
}
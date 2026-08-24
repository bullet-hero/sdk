using System;
using System.Collections.Generic;
using BH.SDK.Models;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    public abstract class JsonConverterCustomType<T, TType> : JsonConverter<T>, IRequiresDefaultSerializer
    {
        public JsonSerializer SerializerDefault { get; private set; }

        void IRequiresDefaultSerializer.SetDefaultSerializer(JsonSerializer serializer) =>
            SerializerDefault = serializer;

        // Only this converter itself needs excluding: it's the one that would otherwise re-wrap the
        // resolved concrete type's plain member JSON (e.g. Color4Key's own fields) as another [type, value] array.
        IEnumerable<JsonConverter> IRequiresDefaultSerializer.GetExcludedConverters(
            IReadOnlyList<JsonConverter> allConverters) =>
            new JsonConverter[] { this };

        // THE TYPE TAG IS WRITTEN AND READ DIRECTLY rather than through a serializer. It is plainly the
        // cheaper way to write one enum, and it measured as a real if modest win - `Resources` on a
        // 3.2 MB level went 672 -> 470 ms - but it is worth reading the block below before assuming it
        // did more than that: on the same level's OBJECTS tree, which is three times the size, it was
        // worth nothing at all.
        //
        // The wire format is unchanged: an enum serializes as its integer here (no StringEnumConverter
        // is registered), which is what every level file already carries - `"clr":[0,{...}]`. Verified
        // rather than assumed: re-serializing a real level reproduces its file byte for byte.

        // THE SECOND HALF OF THE WRAPPER IS WHERE THE TIME GOES, AND IT IS NOT THIS CALL. Measured on
        // a 3.2 MB level: its objects tree costs ~1420 ms through this converter stack and 295 ms
        // through the same contract resolver with no converters at all, so ~1120 ms of it is this
        // layer. The obvious suspects were both tried and both measured at nothing:
        //
        // - the type tag above, which used to go through `serializer.Serialize` - kept, since it is
        //   plainly cheaper, but worth 0 ms on this tree (it did save ~200 ms on `Resources`);
        // - hand-writing the members for `Vector2Value`, `FloatValue` and `Color4Value` - the three
        //   most common values in a level - instead of this `Serialize` call. Eight passes put that
        //   build at min 1398 / max 1437 / mean 1419 ms against 1421 without it. Reverted: it is a
        //   hand-kept duplicate of what the contract writes, so a field added to one of those models
        //   and not to its writer changes the wire format silently, and it bought nothing.
        //
        // So the cost is the per-value DISPATCH - `ConverterRouter` plus Newtonsoft's own
        // converter-invocation path - rather than the work inside the converters. Anything aimed at
        // this number should start by measuring that, and should not assume otherwise from the
        // shape of the code here.

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

        public abstract TType GetCustomType(T value);
        public abstract Type GetType(TType customType);

        // `TType` is an enum on every one of the seventeen converters built on this, but nothing in the
        // signature can say so without constraining the type parameter and touching all of them, so the
        // conversion goes through `Convert`. That boxes the enum once per value - tens of nanoseconds
        // and one gen-0 object, negligible beside the multi-megabyte string being built around it. A
        // converter that writes enough values to care can override this with a direct `(int)` cast,
        // which boxes nothing.

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
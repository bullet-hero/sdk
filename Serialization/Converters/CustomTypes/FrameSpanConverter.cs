using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    // Writes the two LOGICAL numbers, each negated when its edge is anchored; the in-memory sign-bit
    // packing must never reach the file (open format). No offset: neither number can be zero, since
    // frames count from FrameRules.MinFrame. A two-number array, so the same under BSON.

    /// <summary> Writes FrameSpan as [start, duration], each negated when its own edge is anchored. </summary>
    public class FrameSpanConverter : JsonConverter<FrameSpan>
    {
        /// <summary> Writes the two logical numbers, each negated when its own edge is anchored - never the in-memory packing. </summary>
        public override void WriteJson(JsonWriter writer, FrameSpan value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.IsAnchoredStart ? -value.StartFrame : value.StartFrame);
            writer.WriteValue(value.IsAnchoredEnd ? -value.FrameDuration : value.FrameDuration);
            writer.WriteEndArray();
        }

        // Every out-of-shape input degrades to a default span instead of throwing: FrameSpan's own
        // constructor clamps whatever it is handed into the legal range anyway, so a corrupt file
        // costs the author one wrong object rather than a level that refuses to open.

        /// <summary> Unpacks that pair, leniently: a damaged one should cost the author an object rather than the level. </summary>
        public override FrameSpan ReadJson(JsonReader reader, Type objectType, FrameSpan existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                reader.Skip();
                return default;
            }

            var array = JArray.Load(reader);
            if (array.Count < 2) return default;

            var rawStart = array[0].Value<int>();
            var rawDuration = array[1].Value<int>();

            var anchors = FrameAnchor.None;
            if (rawStart < 0) anchors |= FrameAnchor.Start;
            if (rawDuration < 0) anchors |= FrameAnchor.End;

            var startFrame = rawStart < 0 ? -rawStart : rawStart;
            var frameDuration = rawDuration < 0 ? -rawDuration : rawDuration;

            return new FrameSpan(startFrame, frameDuration, anchors);
        }
    }
}
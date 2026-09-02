using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    // FrameSpan packs its anchor flags into the sign bits of its two backing ints, and the wire
    // format does the same thing for the same reason - two numbers say everything a span is. The
    // in-memory packing itself must never reach the file though: a level is an open format read by
    // third party tools, and a start frame surfacing as -2147483548 would be unreadable to all of
    // them. So this writes the two LOGICAL numbers, sign-flipped when that edge is anchored.
    //
    // The duration carries no offset because it cannot be zero (FrameDuration >= 1 by invariant),
    // so its sign is free. The start CAN be zero, and -0 does not exist in JSON, so an
    // anchored start is written as -(start + 1) - the one off-by-one in the format, confined to the
    // negative branch so an ordinary unanchored span still reads as its own plain frame number.
    //
    // The array form (rather than an object with named keys) matches the rest of the wire format,
    // which is deliberately compact, and works identically under BSON.

    /// <summary> Writes FrameSpan as [start, duration], each negated when its own edge is anchored. </summary>
    public class FrameSpanConverter : JsonConverter<FrameSpan>
    {
        public override void WriteJson(JsonWriter writer, FrameSpan value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.IsAnchoredStart ? -(value.StartFrame + 1) : value.StartFrame);
            writer.WriteValue(value.IsAnchoredEnd ? -value.FrameDuration : value.FrameDuration);
            writer.WriteEndArray();
        }

        // Every out-of-shape input degrades to a default span instead of throwing: FrameSpan's own
        // constructor clamps whatever it is handed into the legal range anyway, so a corrupt file
        // costs the author one wrong object rather than a level that refuses to open.
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

            var startFrame = rawStart < 0 ? -(rawStart + 1) : rawStart;
            var frameDuration = rawDuration < 0 ? -rawDuration : rawDuration;

            return new FrameSpan(startFrame, frameDuration, anchors);
        }
    }
}

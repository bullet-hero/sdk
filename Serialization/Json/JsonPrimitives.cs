using System;
using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Statistics;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Json
{
    // THE FOUR STRUCTS AND ONE CLASS A GENERATED WRITER CANNOT WRITE FOR ITSELF, and every one of
    // them reproduces an encoding that already exists on disk rather than inventing a new one.
    // FrameSpan's is the only clever one and its cleverness is load-bearing: an anchored edge is the
    // number NEGATED, and because -0 does not exist an anchored start is offset by one first. The
    // other three are plain objects Newtonsoft's own contract would have written, spelled out here
    // because a struct with get-only properties has nothing a member-driven generator can assign.
    //
    // The reads are deliberately as LENIENT as the converters they replace. FrameSpanConverter's
    // header says why: a corrupt file should cost the author one wrong object rather than a level
    // that refuses to open.

    /// <summary> JSON encodings for the values the generator hands off. </summary>
    public static class JsonPrimitives
    {
        #region FrameSpan

        public static void Write(JsonWriter writer, FrameSpan value)
        {
            writer.WriteStartArray();
            // Negated when the edge is anchored; the start is offset by one first, since -0 exists
            // in no format and a span may legitimately start at zero.
            writer.WriteValue(value.IsAnchoredStart ? -(value.StartFrame + 1) : value.StartFrame);
            writer.WriteValue(value.IsAnchoredEnd ? -value.FrameDuration : value.FrameDuration);
            writer.WriteEndArray();
        }

        public static FrameSpan ReadFrameSpan(JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                reader.Skip();
                return default;
            }

            var read = 0;
            var start = 0;
            var duration = 1;

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                var number = Convert.ToInt32(reader.Value);
                if (read == 0) start = number;
                else if (read == 1) duration = number;
                read++;
            }

            if (read < 2) return default;

            var anchors = FrameAnchor.None;
            if (start < 0)
            {
                anchors |= FrameAnchor.Start;
                start = -start - 1;
            }
            if (duration < 0)
            {
                anchors |= FrameAnchor.End;
                duration = -duration;
            }

            return new FrameSpan(start, duration, anchors);
        }

        #endregion

        #region ModificationKey

        public static void Write(JsonWriter writer, ModificationKey value)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(Names.ObjectId);
            writer.WriteValue(value.ObjectId.value);
            writer.WritePropertyName(Names.PathShort);
            writer.WriteValue(value.Path);
            writer.WriteEndObject();
        }

        public static ModificationKey ReadModificationKey(JsonReader reader)
        {
            var objectId = ObjectId.Null;
            var path = string.Empty;

            if (reader.TokenType != JsonToken.StartObject)
            {
                reader.Skip();
                return new ModificationKey(objectId, path);
            }

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var name = (string)reader.Value;
                reader.Read();

                if (name == Names.ObjectId) objectId = new ObjectId(Convert.ToInt32(reader.Value));
                else if (name == Names.PathShort) path = reader.Value as string;
                else reader.Skip();
            }

            return new ModificationKey(objectId, path);
        }

        #endregion

        #region RunProfile

        public static void Write(JsonWriter writer, RunProfile value)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(Names.Lives);
            writer.WriteValue(value.LifeCount);
            writer.WritePropertyName(Names.SpeedCenti);
            writer.WriteValue(value.SpeedCenti);
            writer.WritePropertyName(Names.Checkpoints);
            writer.WriteValue(value.UseCheckpoints);
            writer.WritePropertyName(Names.Bot);
            writer.WriteValue((byte)value.Bot);
            writer.WriteEndObject();
        }

        public static RunProfile ReadRunProfile(JsonReader reader)
        {
            var lives = 0;
            var speed = 0;
            var checkpoints = false;
            var bot = BotKind.None;

            if (reader.TokenType != JsonToken.StartObject)
            {
                reader.Skip();
                return new RunProfile(lives, speed, checkpoints, bot);
            }

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var name = (string)reader.Value;
                reader.Read();

                if (name == Names.Lives) lives = Convert.ToInt32(reader.Value);
                else if (name == Names.SpeedCenti) speed = Convert.ToInt32(reader.Value);
                else if (name == Names.Checkpoints) checkpoints = Convert.ToBoolean(reader.Value);
                else if (name == Names.Bot) bot = (BotKind)Convert.ToByte(reader.Value);
                else reader.Skip();
            }

            return new RunProfile(lives, speed, checkpoints, bot);
        }

        #endregion

        #region Pixel

        public static void Write(JsonWriter writer, Pixel value) => writer.WriteValue(value.rgba);

        public static Pixel ReadPixel(JsonReader reader) => new Pixel { rgba = Convert.ToInt32(reader.Value) };

        #endregion

        #region Guid and DateTime

        // A Guid arrives as a string from a text reader and as an already-boxed Guid from a binary
        // one - the split PrimitiveGuidConverter has carried since BSON existed. Kept because a
        // JsonReader is not necessarily a JsonTextReader.
        public static Guid ReadGuid(JsonReader reader)
        {
            switch (reader.Value)
            {
                case Guid guid: return guid;
                case string text: return Guid.TryParse(text, out var parsed) ? parsed : Guid.Empty;
                default: return Guid.Empty;
            }
        }

        public static DateTime ReadDateTime(JsonReader reader)
        {
            switch (reader.Value)
            {
                case DateTime value: return value;
                case DateTimeOffset offset: return offset.UtcDateTime;
                case string text: return DateTime.TryParse(text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)
                    ? parsed
                    : default;
                default: return default;
            }
        }

        #endregion

        #region System.Version

        public static void WriteVersion(JsonWriter writer, Version value)
        {
            if (value is null) writer.WriteNull();
            else writer.WriteValue(value.ToString());
        }

        public static Version ReadVersion(JsonReader reader)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var text = reader.Value as string;
            return text is null ? null : Version.Parse(text);
        }

        #endregion
    }
}

using System;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Json
{
    // THE THIRD CONTRACT A MODEL CARRIES, beside IModel<T> (what it is to the program) and
    // IBinaryModel (what it is to a .blob). Keeping them apart is what lets a model be copied
    // without knowing about bytes, and written to a file without knowing about Copy.
    //
    // Reading is deliberately NOT the mirror of writing. A writer knows every member; a reader
    // meets a document that may be hand-edited, written by another tool, or written by a build that
    // knew members this one does not - so it switches on the property NAME and skips what it does
    // not recognise, in whatever order the document happens to carry. That is also what makes an
    // added member free: an older file simply leaves it at what the constructor built.

    /// <summary> A model that can write and read itself as JSON, without a JsonSerializer. </summary>
    public interface IJsonModel
    {
        /// <summary> Writes this model, envelope included when it is an aggregate root. </summary>
        void WriteJson(JsonWriter writer);

        /// <summary> Reads one back over this instance. </summary>
        void ReadJson(JsonReader reader);

        /// <summary> Reads ONE member by its wire name, and answers whether it recognised it. The
        /// reader arrives on the value's first token and must be left on its last. Public because
        /// the read loop lives outside the model; nothing else has a reason to call it. </summary>
        bool ReadJsonMember(JsonReader reader, string name);
    }

    /// <summary> The reader loops a generated body relies on. </summary>
    public static class JsonModels
    {
        /// <summary> Reads a plain object into a model, one property at a time. The reader arrives
        /// ON the StartObject and leaves ON the matching EndObject. </summary>
        public static void ReadObject(JsonReader reader, IJsonModel model)
        {
            if (reader.TokenType == JsonToken.Null) return;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException(
                    $"Expected an object for {model.GetType().Name}, found {reader.TokenType}");

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) return;
                if (reader.TokenType != JsonToken.PropertyName) continue;

                var name = (string)reader.Value;
                reader.Read();

                // A member this build does not know is stepped over whole, subtree included. That
                // is the entire forward-compatibility story of the format, and it is free.
                if (!model.ReadJsonMember(reader, name)) reader.Skip();
            }
        }

        /// <summary> Writes a versioned member inside its own envelope. The member is wrapped by
        /// whoever HOLDS it rather than by itself, which is what leaves the top-level wrapper to
        /// VersionedEnvelopeConverter - and with it the migration path an older file still needs. </summary>
        public static void WriteEnvelope(JsonWriter writer, IJsonModel value, string version)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("version");
            writer.WriteValue(version);
            writer.WritePropertyName("value");
            value.WriteJson(writer);
            writer.WriteEndObject();
        }

        /// <summary> The other side of it. </summary>
        public static T ReadEnveloped<T>(JsonReader reader) where T : class, IJsonModel, new()
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException(
                    $"Expected an envelope for {typeof(T).Name}, found {reader.TokenType}");

            var value = new T();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) return value;
                if (reader.TokenType != JsonToken.PropertyName) continue;

                var name = (string)reader.Value;
                reader.Read();

                if (name == "value") ReadObject(reader, value);
                else reader.Skip();
            }

            return value;
        }

        /// <summary> Reads a versioned aggregate's `{version, value}` wrapper. The version is
        /// CHECKED rather than used to resolve a type - the caller already knows which type it is
        /// reading into - so a document that puts `value` first reads exactly the same. </summary>
        public static void ReadEnvelope(JsonReader reader, IJsonModel model,
            string domain, int major, int minor)
        {
            if (reader.TokenType == JsonToken.Null) return;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException(
                    $"Expected an envelope for {domain}, found {reader.TokenType}");

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) return;
                if (reader.TokenType != JsonToken.PropertyName) continue;

                var name = (string)reader.Value;
                reader.Read();

                if (name == "value") ReadObject(reader, model);
                else if (name == "version") CheckVersion(reader, domain, major, minor);
                else reader.Skip();
            }
        }

        private static void CheckVersion(JsonReader reader, string domain, int major, int minor)
        {
            var text = reader.Value as string;
            if (text is null) return;
            if (!Version.TryParse(text, out var version)) return;

            if (version.Major != major || version.Minor != minor)
                throw new JsonSerializationException(
                    $"{domain} is version {version}, this build reads {major}.{minor}");
        }

        /// <summary> A model whose declared type is sealed: null, or an object read into a fresh
        /// instance. The reader sits on the value's first token either way. </summary>
        public static T Read<T>(JsonReader reader) where T : class, IJsonModel, new()
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = new T();
            value.ReadJson(reader);
            return value;
        }
    }
}

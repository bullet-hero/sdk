using System;
using BH.SDK.Models;
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
        public static void WriteEnvelope(JsonWriter writer, IJsonModel value, int generation)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(Names.Generation);
            writer.WriteValue(generation);
            writer.WritePropertyName(Names.Value);
            value.WriteJson(writer);
            writer.WriteEndObject();
        }

        // A NESTED GENERATION IS CHECKED AND REFUSED, NEVER MIGRATED, and the asymmetry with the
        // TOP-LEVEL envelope is the thing to understand here. VersionedEnvelopeConverter resolves an
        // old generation to its snapshot type and walks the migration chain; this cannot, because
        // migrating means reading a type that is not this one, and a generated codec only ever reads
        // ITSELF - it holds a bare JsonReader and no serializer, which is the whole of what makes it
        // fast. Handing it one would change what IJsonModel is.
        //
        // So the choice is between refusing and lying, and it used to lie: the generation was
        // Skip()ed, the payload was read by property name into whatever type this build has, and a
        // domain that had moved came back as CONSTRUCTOR DEFAULTS with nothing thrown and nothing
        // logged. Measured, before this: a nested LevelSettings written at generation 0 read back
        // fps=60 through the generated codec and fps=61 through the reflective one - the same file,
        // two answers, and the quiet one is the default path.
        //
        // Refusing is what the .blob codec already does for the same case (ModelBlobEmitter), so the
        // two formats now agree. What is still missing is the migration itself; Docs/VERSIONING.md
        // carries the options and why closing it properly waits for a real second snapshot.
        //
        // AN ABSENT TAG IS ALSO A REFUSAL. Every writer this format has ever had emits one, so its
        // absence is a damaged or foreign document rather than an old one - and treating it as "no
        // objection" would reopen exactly the hole above.

        /// <summary> The other side of it: reads a nested domain written at the generation this build
        /// expects, and refuses any other. </summary>
        public static T ReadEnveloped<T>(JsonReader reader, int expectedGeneration)
            where T : class, IJsonModel, new()
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException(
                    $"Expected an envelope for {typeof(T).Name}, found {reader.TokenType}");

            var value = new T();
            var hasGeneration = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType != JsonToken.PropertyName) continue;

                var name = (string)reader.Value;
                reader.Read();

                if (name == Names.Value) ReadObject(reader, value);
                else if (name == Names.Generation)
                {
                    CheckGeneration(reader, typeof(T), expectedGeneration);
                    hasGeneration = true;
                }
                else reader.Skip();
            }

            // Checked at the END rather than before the payload, so a document that carries `v`
            // first - hand-edited, or written by another tool - is refused for its generation rather
            // than for its property order.
            if (!hasGeneration)
                throw new JsonSerializationException(
                    $"{typeof(T).Name} envelope carries no '{Names.Generation}' property");

            return value;
        }

        private static void CheckGeneration(JsonReader reader, Type type, int expected)
        {
            if (reader.TokenType != JsonToken.Integer)
                throw new JsonSerializationException(
                    $"{type.Name} envelope declares its generation as {reader.TokenType}, expected a number");

            var generation = Convert.ToInt32(reader.Value);
            if (generation == expected) return;

            throw new JsonSerializationException(
                $"{type.Name} is generation {generation}, this build reads {expected}. A nested domain " +
                "is refused rather than migrated - see Docs/VERSIONING.md");
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

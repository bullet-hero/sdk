using System;
using BH.SDK.Models;
using BH.SDK.Versions;
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

        // A KNOWN GENERATION MIGRATES, AN UNKNOWN ONE DEGRADES, and this reads a nested domain exactly
        // as VersionedEnvelopeConverter reads the top-level one. It became able to because a frozen
        // snapshot carries [GenerateModel] now: it reads ITSELF with its own generated codec, so this
        // path still holds a bare JsonReader and no serializer, which is the whole of what makes it
        // fast and is what IJsonModel's identity rests on.
        //
        // WHAT MUST NOT COME BACK IS THE SILENCE. Before the refusal this stood on, the generation was
        // Skip()ed, the payload was read by property name into whatever type this build has, and a
        // domain that had moved came back as CONSTRUCTOR DEFAULTS with nothing thrown and nothing
        // logged. Measured, then: a nested LevelSettings written at generation 0 read back fps=60
        // through the generated codec and fps=61 through the reflective one - the same file, two
        // answers, and the quiet one was the default path. The tolerant read is back; the silence is
        // not, and BOTH codec stacks must degrade to the same value or useGeneratedCodecs stops being
        // a switch that changes nothing.
        //
        // MIGRATION NEEDS THE GENERATION BEFORE THE PAYLOAD, which every writer this format has ever
        // had provides - WriteEnvelope emits `g` first. A document that carries `v` first (hand-edited,
        // or written by another tool) takes the lossy half instead of a buffered JToken, because
        // nothing on this read path materializes a token tree; that is a rule of the format, and
        // VersionedEnvelopeConverter is its single documented exception.
        //
        // AN ABSENT TAG IS NO LONGER A REFUSAL EITHER. It is reported, and the payload is read as-is.

        /// <summary> The other side of it: reads a nested domain, migrating a generation that resolves and
        /// degrading one that does not. </summary>
        public static T ReadEnveloped<T>(JsonReader reader, string domain, int expectedGeneration)
            where T : class, IJsonModel, new()
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException(
                    $"Expected an envelope for {typeof(T).Name}, found {reader.TokenType}");

            T value = null;
            var generation = ModelGenerations.Invalid;
            var hasGeneration = false;
            var readAhead = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType != JsonToken.PropertyName) continue;

                var name = (string)reader.Value;
                reader.Read();

                if (name == Names.Generation)
                {
                    hasGeneration = TryReadGeneration(reader, domain, typeof(T), out generation);
                }
                else if (name == Names.Value)
                {
                    if (hasGeneration && generation != expectedGeneration)
                    {
                        value = ReadOtherGeneration<T>(reader, domain, generation, expectedGeneration);
                    }
                    else
                    {
                        value = ReadHere<T>(reader);
                        readAhead = !hasGeneration;
                    }
                }
                else reader.Skip();
            }

            if (!hasGeneration)
                SerializationReport.Report(domain, typeof(T).Name,
                    $"no '{Names.Generation}' at all; read as today's shape", ModelGenerations.Invalid,
                    SubstitutionKind.AbsentGeneration);
            else if (readAhead && generation != expectedGeneration)
                SerializationReport.Report(domain, typeof(T).Name,
                    "the payload came before its generation, so it was read as today's shape", generation,
                    SubstitutionKind.UnknownGeneration);

            return value ?? new T();
        }

        private static T ReadHere<T>(JsonReader reader) where T : class, IJsonModel, new()
        {
            var value = new T();
            ReadObject(reader, value);
            return value;
        }

        // A KNOWN GENERATION MIGRATES, AN UNKNOWN ONE DEGRADES, and only the second half is lossy.
        // Migration needs a type that is not this one, which is exactly what a snapshot carrying
        // [GenerateModel] now is: it reads itself with its own generated codec, so this path still
        // holds a bare reader and no serializer.
        //
        // Activator.CreateInstance once per out-of-date envelope is free at this scale - envelopes are
        // ~1.4% of a level's nodes (266 of 19 341 in volcano) and this branch runs only when a file is
        // actually old. The ordinary read never reaches it.

        private static T ReadOtherGeneration<T>(JsonReader reader, string domain, int generation, int expected)
            where T : class, IJsonModel, new()
        {
            var type = VersionedTypeRegistry.TryResolve(domain, generation);

            if (type != null && typeof(IJsonModel).IsAssignableFrom(type))
            {
                var snapshot = (IJsonModel)Activator.CreateInstance(type);
                ReadObject(reader, snapshot);

                if (VersionedTypeRegistry.TryUpgradeToLatest(domain, snapshot, generation, out var upgraded)
                    && upgraded is T migrated)
                {
                    SerializationReport.Report(domain, type.Name, $"migrated to generation {expected}", generation,
                        SubstitutionKind.MigratedGeneration);
                    return migrated;
                }

                SerializationReport.Report(domain, type.Name, "left at its defaults", generation,
                    SubstitutionKind.IncompleteChain);
                return new T();
            }

            // The lossy half, and it is safe in JSON and nowhere else: every known key lands, every
            // unknown key is stepped over whole, and every key that MOVED falls to the constructor's
            // value. Nothing can migrate a shape no build has ever seen.
            var value = ReadHere<T>(reader);
            SerializationReport.Report(domain, typeof(T).Name, "read into today's shape by property name",
                generation, SubstitutionKind.UnknownGeneration);
            return value;
        }

        private static bool TryReadGeneration(JsonReader reader, string domain, Type type, out int generation)
        {
            generation = ModelGenerations.Invalid;

            if (reader.TokenType == JsonToken.Integer)
            {
                generation = Convert.ToInt32(reader.Value);
                return true;
            }

            SerializationReport.Report(domain, type.Name,
                $"the generation is a {reader.TokenType} rather than a number", ModelGenerations.Invalid,
                SubstitutionKind.AbsentGeneration);
            reader.Skip();
            return false;
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

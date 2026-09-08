using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Serializers
{
    // JSON implementation of IDataSerializer - see NewtonsoftDataSerializer for the shared envelope
    // logic. Works uniformly for every [DataVersion] domain, top-level (Level, Theme, UserSettings,
    // ...) or nested (LevelSettings, GameLevel, ...) alike, since VersionedEnvelopeConverter.CanConvert
    // is gated purely on the attribute being present, not on a fixed type list.
    //
    // ONE SHAPE, ALWAYS COMPACT. There used to be a second mode that wrote the same document
    // indented, and it never earned its place: nothing could recover the choice from a file, so it
    // described the person who saved rather than the level. Reading a level file by eye is what an
    // editor's formatter is for, on demand.

    /// <summary> The readable format, and the one the project's longevity promise is about. Always compact. </summary>
    public class JsonDataSerializer : BaseNewtonsoftDataSerializer
    {
        /// <summary> Takes the serializer carrying the whole converter stack. </summary>
        public JsonDataSerializer(JsonSerializer serializer) : base(serializer)
        {
        }

        /// <summary> Which format this writes. </summary>
        public override SerializationType Type => SerializationType.Json;

        /// <summary> A compact UTF-8 text writer that leaves the stream open. </summary>
        protected override JsonWriter CreateWriter(Stream stream) =>
            new JsonTextWriter(new StreamWriter(stream, Encoding.UTF8, 1024, true))
                { Formatting = Formatting.None };

        /// <summary> The matching reader. </summary>
        protected override JsonReader CreateReader(Stream stream) =>
            new JsonTextReader(new StreamReader(stream, Encoding.UTF8, false, 1024, true));
    }
}

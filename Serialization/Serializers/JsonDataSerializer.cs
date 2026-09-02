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
    public class JsonDataSerializer : BaseNewtonsoftDataSerializer
    {
        public JsonDataSerializer(JsonSerializer serializer) : base(serializer)
        {
        }

        public override SerializationType Type => SerializationType.Json;

        protected override JsonWriter CreateWriter(Stream stream) =>
            new JsonTextWriter(new StreamWriter(stream, Encoding.UTF8, 1024, true))
                { Formatting = Formatting.None };

        protected override JsonReader CreateReader(Stream stream) =>
            new JsonTextReader(new StreamReader(stream, Encoding.UTF8, false, 1024, true));
    }
}

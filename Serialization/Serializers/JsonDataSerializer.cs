using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Serializers
{
    // JSON implementation of IDataSerializer (shared logic: BaseNewtonsoftDataSerializer), for every
    // [ModelGeneration] domain top-level or nested. One shape, always compact.

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

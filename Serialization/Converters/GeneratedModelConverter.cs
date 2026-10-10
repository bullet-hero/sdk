using System;
using BH.SDK.Serialization.Json;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters
{
    // One converter for every IJsonModel, LAST in the list: first match wins by runtime type, so a
    // tagged value still reaches its own converter; first, every tagged value would lose its tag.
    // A snapshot is declined: VersionedEnvelopeConverter's reflective path reads it. Docs/SERIALIZATION.md.

    /// <summary> Routes a model to the codec BH.SDK.Roslyn wrote for it. </summary>
    public sealed class GeneratedModelConverter : JsonConverter
    {
        /// <summary> One answer for every generated model - two hundred entries in the router's list would undo what it exists for. </summary>
        public override bool CanConvert(Type objectType) => typeof(IJsonModel).IsAssignableFrom(objectType);

        /// <summary> Writes through the model's own generated codec. </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is null) writer.WriteNull();
            else ((IJsonModel)value).WriteJson(writer);
        }

        /// <summary> Reads through it, over a fresh instance. </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            // ObjectCreationHandling.Replace means existingValue is null on every ordinary read;
            // honouring it anyway costs nothing and keeps this correct if that ever changes.
            var model = existingValue as IJsonModel ?? (IJsonModel)Activator.CreateInstance(objectType);
            model.ReadJson(reader);
            return model;
        }
    }
}

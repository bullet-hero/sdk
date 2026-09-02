using System;
using BH.SDK.Serialization.Json;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters
{
    // ONE CONVERTER FOR TWO HUNDRED MODELS. Every generated model implements IJsonModel, so a single
    // CanConvert answers for all of them - which matters, because Newtonsoft walks a serializer's
    // converter list per VALUE and the list is what ConverterRouter exists to make cheap. Two
    // hundred entries there would have undone that.
    //
    // IT GOES LAST IN THE LIST, AND THAT IS THE WHOLE OF ITS WIRING. The router resolves by the
    // value's runtime type and takes the FIRST match, so a Vector2Value still reaches
    // Vector2Converter and still comes out as `[0,{...}]`; the inner serializer that converter uses
    // excludes only itself, finds this one, and writes the `{x,y}` payload from generated code. A
    // model with no polymorphic wrapper - PosKey, LevelSettings - reaches this directly. Put it
    // first and every tagged value in the format would silently lose its tag.
    //
    // VersionedEnvelopeConverter still owns the top-level `{version, value}` wrapper. That is not a
    // leftover: it is what resolves an OLD version to its historical snapshot type and walks the
    // migration chain, and a snapshot is deliberately not a generated model - so this converter
    // declines it and the reflective path reads it, exactly as before.

    /// <summary> Routes a model to the codec BH.SDK.Roslyn wrote for it. </summary>
    public sealed class GeneratedModelConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(IJsonModel).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is null) writer.WriteNull();
            else ((IJsonModel)value).WriteJson(writer);
        }

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

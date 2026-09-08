using System;
using BH.SDK.Models.Interfaces.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Writes any float id wrapper as the bare number, and rebuilds it through its one-argument constructor. </summary>
    public class PrimitiveFloatConverter : JsonConverter
    {
        /// <summary> Every float id wrapper at once, rather than one converter per wrapper. </summary>
        public override bool CanConvert(Type objectType) => typeof(IPrimitiveFloat).IsAssignableFrom(objectType);

        /// <summary> Writes the bare number. </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((((IPrimitiveFloat)value)!).Value);
        }

        /// <summary> Rebuilds the wrapper through its one-argument constructor. </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var value = Convert.ToSingle(reader.Value);
            return Activator.CreateInstance(objectType, value);
        }
    }
}
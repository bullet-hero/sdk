using System;
using BH.SDK.Models.Interfaces.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Writes any int id wrapper as the bare number, and rebuilds it through its one-argument constructor. </summary>
    public class PrimitiveIntConverter : JsonConverter
    {
        /// <summary> Every int id wrapper at once. </summary>
        public override bool CanConvert(Type objectType) => typeof(IPrimitiveInt).IsAssignableFrom(objectType);

        /// <summary> Writes the bare number. </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((((IPrimitiveInt)value)!).Value);
        }

        /// <summary> Rebuilds the wrapper through its one-argument constructor. </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var value = Convert.ToInt32(reader.Value);
            return Activator.CreateInstance(objectType, value);
        }
    }
}

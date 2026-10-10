using System;
using BH.SDK.Models.Interfaces.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Writes any guid id wrapper as the bare guid, and rebuilds it through its one-argument constructor. </summary>
    public class PrimitiveGuidConverter : JsonConverter
    {
        /// <summary> Every guid id wrapper at once. </summary>
        public override bool CanConvert(Type objectType) => typeof(IPrimitiveGuid).IsAssignableFrom(objectType);

        /// <summary> Writes the bare guid. </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((((IPrimitiveGuid)value)!).Value);
        }

        /// <summary> Rebuilds the wrapper through its one-argument constructor. </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            // A Guid is a string under the JSON reader and an already-boxed Guid under a binary one;
            // the same converter chain has to serve both.
            var value = reader.Value switch
            {
                Guid guid => guid,
                string str => new Guid(str),
                _ => Guid.Empty,
            };
            return Activator.CreateInstance(objectType, value);
        }
    }
}
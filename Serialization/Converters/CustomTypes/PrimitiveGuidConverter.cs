using System;
using BH.SDK.Models.Interfaces.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    public class PrimitiveGuidConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(IPrimitiveGuid).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((((IPrimitiveGuid)value)!).Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            // JsonDataSerializer writes a Guid as a plain string token (reader.Value surfaces as
            // string); a binary format can hand back an already-boxed Guid (reader.Value
            // surfaces as an already-boxed Guid) - see BaseNewtonsoftDataSerializer's doc comment on
            // why the same converter chain has to work for both. WriteJson below only ever needs the
            // Guid overload since IPrimitiveGuid.Value already hands back a Guid.
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
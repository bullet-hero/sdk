using System;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters
{
    /// <summary> A version as its textual form, so an envelope's tag is readable in the file. </summary>
    public class VersionConverter : JsonConverter<Version>
    {
        /// <summary> Writes the textual form, so an envelope's tag is readable in the file. </summary>
        public override void WriteJson(JsonWriter writer, Version value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            
            writer.WriteValue(value.ToString());
        }

        /// <summary> Parses it back, refusing anything that is not a version. </summary>
        public override Version ReadJson(JsonReader reader, Type objectType, Version existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            
            if (reader.TokenType != JsonToken.String)
                throw new JsonSerializationException($"Expected string token when reading Version, got {reader.TokenType}");
            
            var versionString = reader.Value?.ToString();
            if (string.IsNullOrEmpty(versionString))
                return null;
            
            try
            {
                return new Version(versionString);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Invalid version string: '{versionString}'", ex);
            }
        }
    }
}
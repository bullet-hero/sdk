using System;

namespace BH.SDK.Serialization.Serializers
{
    // Maps SerializationType to/from the on-disk file extension it's written/read with, so callers
    // can pick a serializer from an existing file (directory scan) or build a file name for a
    // caller-chosen format (save), without duplicating the ".json"/".blob" strings at every call site.
    //
    // There is no ToFormatting here any more, and no formatting question anywhere: every JSON
    // document this project writes is compact. The mode that wrote an indented one described the
    // person saving rather than the level - nothing could recover the choice from a file - and
    // reading one by eye is what an editor's own formatter is for, on demand.
    public static class SerializationTypeExtensions
    {
        public static string ToFileExtension(this SerializationType type)
        {
            return type switch
            {
                SerializationType.Json => ".json",
                SerializationType.Blob => ".blob",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static bool TryFromFileExtension(string extension, out SerializationType type)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".json":
                    type = SerializationType.Json;
                    return true;
                case ".blob":
                    type = SerializationType.Blob;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }
    }
}
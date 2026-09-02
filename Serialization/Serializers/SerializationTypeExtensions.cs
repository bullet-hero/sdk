using System;
using System.Collections.Generic;

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
        private static readonly SerializationType[] Probe =
        {
            SerializationType.Json,
            SerializationType.Blob,
        };

        // WHICH FILE WINS WHEN TWO EXIST, which is a different question from what an enum's numbers
        // happen to be and from what a settings dropdown lists. Every probe in the project - the
        // game's own PathUtils, the package reader - used to spell this order out for itself, so a
        // third format meant finding three hand-written pairs of branches, and one of them being
        // missed would resolve a real level to "no file".
        //
        // Json is first because it is the default everywhere and the one a hand-made folder carries.
        // The order is NOT Enum.GetValues': that returns numeric order, and a member's number is a
        // retirement record (1 was Bson, 2 was JsonPretty) rather than a statement about preference.

        /// <summary> Every live format, in the order a probe should try them. </summary>
        public static IReadOnlyList<SerializationType> ProbeOrder { get; } = Array.AsReadOnly(Probe);

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
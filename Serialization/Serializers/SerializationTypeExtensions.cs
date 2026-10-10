using System;
using System.Collections.Generic;

namespace BH.SDK.Serialization.Serializers
{
    // Maps SerializationType to the on-disk file extension and back, so no call site spells ".json" or
    // ".blob" itself. There is no ToFormatting: every JSON document written is compact.

    /// <summary> The one mapping between a format and the file extension it lives under. </summary>
    public static class SerializationTypeExtensions
    {
        private static readonly SerializationType[] Probe =
        {
            SerializationType.Json,
            SerializationType.Blob,
        };

        // Which file wins when two exist, in preference order, NOT Enum.GetValues' numeric order (a
        // number is a retirement record here). Json first: the default, and what a hand-made folder carries.

        /// <summary> Every live format, in the order a probe should try them. </summary>
        public static IReadOnlyList<SerializationType> ProbeOrder { get; } = Array.AsReadOnly(Probe);

        /// <summary> The extension a file of this format is written with, dot included. </summary>
        public static string ToFileExtension(this SerializationType type)
        {
            return type switch
            {
                SerializationType.Json => ".json",
                SerializationType.Blob => ".blob",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary> The format an extension names, case-insensitively; false when none does. </summary>
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
using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Publishing
{
    // What a level WEIGHS, which neither of its files can say. An input to the check, not part of the
    // format (hence no [JsonProperty]); optional, and never needed by a profile with no size limits.

    /// <summary> Measured sizes of a level's files, for the size half of a publish check. </summary>
    public class PublishPayload
    {
        private readonly Dictionary<(ResourceType, int), long> _resourceBytes = new();

        /// <summary> Size of level.json/.blob. </summary>
        public long LevelBytes { get; set; }

        /// <summary> Size of metadata.json/.blob. </summary>
        public long MetaBytes { get; set; }

        /// <summary> Everything the level folder weighs, packed - including files no model
        /// references. Zero means it was not measured. </summary>
        public long TotalBytes { get; set; }

        /// <summary> Record one resource file's size. </summary>
        public void SetResourceBytes(ResourceType resourceType, TypedResourceId resourceId, long bytes)
            => _resourceBytes[(resourceType, resourceId.value)] = bytes;

        /// <summary> Size of one resource's file, or zero when it was not measured. </summary>
        public long GetResourceBytes(ResourceType resourceType, TypedResourceId resourceId)
            => _resourceBytes.TryGetValue((resourceType, resourceId.value), out var bytes) ? bytes : 0;

        /// <summary> Every measured resource, for a caller that wants to report them. </summary>
        public IReadOnlyDictionary<(ResourceType, int), long> ResourceBytes => _resourceBytes;
    }
}

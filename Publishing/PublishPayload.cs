using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Publishing
{
    // What a level WEIGHS, which neither of its two files can say. A level is a folder - the model
    // knows a texture is referenced, not that it is 90 MB - so the sizes have to come from whoever
    // is holding the actual bytes: the editor reading its own level directory before an upload, or a
    // server measuring what just arrived. That makes this an input to the check, not part of the
    // format, and the reason it is a plain class with no [JsonProperty] anywhere on it.
    //
    // Passing it is optional for the same reason passing the level file is: a caller that only has
    // metadata.json can still check most of the policy. A profile with no size limits never needs it
    // at all.

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

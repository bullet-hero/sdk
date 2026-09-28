using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: ResourceMeta is no domain of its own, it is versioned
    // inside LevelMeta's envelope. Frozen because generation 2 appended ResourceGuid to the live one.

    /// <summary> A resource record as LevelMeta generation 1 (Release) stored it - file resources only,
    /// before a record could credit a theme, effect, shape or prefab. A frozen snapshot - never edit it
    /// to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class ResourceMetaV1 : IModel<ResourceMetaV1>
    {
        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("resource_type")]
        public ResourceType ResourceType { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("resource_id")]
        public TypedResourceId ResourceId { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("title")]
        public IString ResourceTitle { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("desc")]
        public IString ResourceDescription { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("url")]
        public string ResourceUrl { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("license")]
        public ILicense ResourceLicense { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("permissions")]
        public List<PermissionGrant> ResourcePermissions { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("hashes")]
        public List<string> ResourceHashes { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("src")]
        public List<IString> ResourceSources { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("authors")]
        public List<Author> ResourceAuthors { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("featured")]
        public bool ResourceFeatured { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public ResourceMetaV1()
        {
            ResourceType = ResourceType.Bytes;
            ResourceId = TypedResourceId.Null;
            ResourceTitle = new StringValue();
            ResourceDescription = new StringValue();
            ResourceUrl = string.Empty;
            ResourceLicense = new NoSpecifiedLicense();
            ResourcePermissions = new List<PermissionGrant>();
            ResourceHashes = new List<string>();
            ResourceSources = new List<IString>();
            ResourceAuthors = new List<Author>();
            ResourceFeatured = false;
        }
    }
}

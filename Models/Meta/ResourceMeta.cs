using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Meta
{
    /// <summary>
    /// Legal/attribution record for one user-defined resource, or the cover: who made it, under what
    /// license, where from. Lives in LevelMeta, never in Level: this says whether it may be used at all.
    /// </summary>
    [RuleContainer]
    [RuleResourceMetaId]
    [GenerateModel]
    public sealed partial class ResourceMeta : IModel<ResourceMeta>
    {
        /// <summary> Which resource family the record describes - the only thing that disambiguates
        /// an otherwise untyped id, and what decides whether ResourceId or ResourceGuid is used. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.ResourceType)]
        public ResourceType ResourceType { get; set; }

        // Abstract resId, it cast to dedicated ids (audioResId, texResId...)

        /// <summary> The described FILE resource, kept untyped so one list can cover every category.
        /// In the user-defined (negative) range - game resources need no UGC metadata - and Null for a
        /// data resource, which uses ResourceGuid instead (RuleResourceMetaId). </summary>
        [JsonProperty(Names.ResourceId)]
        public TypedResourceId ResourceId { get; set; }

        /// <summary> Display name of the original work, localizable (IString). </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Title)]
        public IString ResourceTitle { get; set; }

        /// <summary> Free-form description of the work, localizable (IString). </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxEditorDescription)]
        [JsonProperty(Names.Description)]
        public IString ResourceDescription { get; set; }

        /// <summary> Canonical page of the work (the artist's post/store page), for attribution.
        /// Not a download link - those live in Resource.Sources on the level side. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxUrl)]
        [JsonProperty(Names.Url)]
        public string ResourceUrl { get; set; }

        // Unspecified is the only honest default (it was CC BY-NC): an unfilled record is one nobody read
        // the terms of. Matches what RuleNotNull repairs a null into. Docs/RESOURCE_META.md

        /// <summary> Terms the work is distributed under. Unspecified until the author states
        /// otherwise - it means "unknown", never "permitted". </summary>
        [RuleNotNull(typeof(NoSpecifiedLicense))]
        [JsonProperty(Names.License)]
        public ILicense ResourceLicense { get; set; }

        /// <summary> Permissions from rights holders covering this resource - Option B of the
        /// licensing policy, for works whose own license does not allow redistribution. </summary>
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxPermissions), RuleCollectionNoNullItems]
        [JsonProperty(Names.Permissions)]
        public List<PermissionGrant> ResourcePermissions { get; set; }

        // Identity of the BYTES, not of the record: a takedown is answered by hash, not name. Plural (one
        // resource, several files). Format is "<algorithm>:<hex>", so the algorithm can change.

        /// <summary> Content hashes of the files behind this resource ("sha256:ab12..."). </summary>
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxHashes), RuleCollectionNoNullItems]
        [JsonProperty(Names.Hashes)]
        public List<string> ResourceHashes { get; set; }

        /// <summary> Human-readable provenance strings (where it was taken from), localizable. Free
        /// text for a reader, unlike the machine-usable URIs in Resource.Sources. </summary>
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxMetaSources)]
        [JsonProperty(Names.Sources)]
        public List<IString> ResourceSources { get; set; }

        /// <summary> Everyone who must be credited for this single resource - separate from the
        /// level's own LevelMeta.Authors. </summary>
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxAuthors)]
        [JsonProperty(Names.Authors)]
        public List<Author> ResourceAuthors { get; set; }

        // Which record a card names, decided by the author: a level's presentation may never open level.json.
        // Declared LAST: the blob writes in declaration order and that order is append-only
        // (.claude/rules/sdk-format.md). Anything new goes here, under this comment.

        /// <summary> Whether this resource is worth naming on the level's card, not only inside the
        /// full credits. False is ordinary - most records are paperwork, not a headline. </summary>
        [JsonProperty(Names.Featured)]
        public bool ResourceFeatured { get; set; }

        // GENERATION 2. A theme, effect, shape or prefab is addressed by a guid, which no int slot can
        // hold, so a record crediting one carries it here and leaves ResourceId Null. Last, per the
        // append-only order above.

        /// <summary> The described DATA resource's id (theme, effect, shape, prefab), or empty for a
        /// file resource. </summary>
        [JsonProperty(Names.ResourceGuid)]
        public Guid ResourceGuid { get; set; }

        // Generation 2. AI generation is a property of the bytes, three states (AiGeneration). Appended last.

        /// <summary> Whether this resource was generated by AI, as its record declares.
        /// NotSpecified until the author says. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.AiGenerated)]
        public AiGeneration ResourceAiGenerated { get; set; }

        // No age rating here on purpose: it lives on LevelMeta alone. Docs/RESOURCE_META.md

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ResourceMeta()
        {
            ResourceType = ResourceType.Bytes;
            ResourceId = TypedResourceId.Null;
            ResourceTitle = new StringValue();
            ResourceDescription = new StringValue();
            ResourceUrl = string.Empty;
            ResourceLicense = new NoSpecifiedLicense();
            ResourceSources = new List<IString>();
            ResourceAuthors = new List<Author>();
            ResourcePermissions = new List<PermissionGrant>();
            ResourceHashes = new List<string>();
            ResourceFeatured = false;
            ResourceGuid = Guid.Empty;
            ResourceAiGenerated = AiGeneration.NotSpecified;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public ResourceMeta(ResourceType resourceType, TypedResourceId resourceId, IString resourceTitle,
            IString resourceDescription, string resourceUrl, ILicense resourceLicense,
            List<IString> resourceSources, List<Author> resourceAuthors,
            List<PermissionGrant> resourcePermissions = null, List<string> resourceHashes = null,
            bool resourceFeatured = false, Guid resourceGuid = default,
            AiGeneration resourceAiGenerated = AiGeneration.NotSpecified)
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
            ResourceTitle = resourceTitle;
            ResourceDescription = resourceDescription;
            ResourceUrl = resourceUrl;
            ResourceLicense = resourceLicense;
            ResourceSources = resourceSources;
            ResourceAuthors = resourceAuthors;
            ResourcePermissions = resourcePermissions ?? new List<PermissionGrant>();
            ResourceHashes = resourceHashes ?? new List<string>();
            ResourceFeatured = resourceFeatured;
            ResourceGuid = resourceGuid;
            ResourceAiGenerated = resourceAiGenerated;
        }

        /// <summary> A permission that still stands at the given UTC time, if this record holds one.
        /// An unset `now` means "no clock available" and never lapses a grant. </summary>
        public bool TryGetActivePermission(DateTime now, out PermissionGrant grant)
        {
            foreach (var permission in ResourcePermissions)
            {
                if (permission == null || !permission.IsActiveAt(now)) continue;
                grant = permission;
                return true;
            }

            grant = null;
            return false;
        }
    }
}
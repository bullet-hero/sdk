using System;
using System.Collections.Generic;
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
    /// Legal/attribution record for one user-defined resource of a level - who made it, under what
    /// license, where it came from. Lives in LevelMeta (metadata.json), never inside Level itself:
    /// the level file says how a resource is used, this says whether it may be used at all.
    /// </summary>
    [RuleContainer]
    public class ResourceMeta : IModel<ResourceMeta>
    {
        /// <summary> Which resource family ResourceId belongs to - the only thing that disambiguates
        /// an otherwise untyped id. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.ResourceType)]
        public ResourceType ResourceType { get; set; }

        // Abstract resId, it cast to dedicated ids (audioResId, texResId...)

        /// <summary> The described resource, kept untyped so one list can cover every category.
        /// Capped to the user-defined (negative) range - game resources need no UGC metadata. </summary>
        [RuleIPrimitiveIntMax(TypedResourceId.MaxUserDefinedValue)]
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

        // Unspecified is the only honest default here, and it used to be CC BY-NC. An unfilled record
        // is a record nobody read the terms of, and defaulting it to a real license made the file
        // state, on the author's behalf, something they never checked - about a work that is usually
        // someone else's. A publish profile can then refuse the level, which is the correct outcome;
        // a level wrongly labelled CC BY-NC is indistinguishable from one that genuinely is, and no
        // later pass can tell the two apart. Note this matches what RuleNotNull already repaired a
        // null into, so the constructor and the rule finally agree.

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

        // Identity of the BYTES, not of the record. A takedown names a work, and answering it means
        // finding every level carrying that work - by name it is a guess, by hash it is a lookup.
        // Plural because one resource can be several files (a track re-encoded per platform) and each
        // has its own digest. Format is "<algorithm>:<hex>" so the algorithm can change without
        // breaking what is already stored.

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

        // No age rating or content descriptors here on purpose - those live on LevelMeta alone. A
        // rating describes what a player is about to experience, which is a property of the finished
        // level, not of an asset in isolation: the same track is menu music in one level and a jump
        // scare in another. Per-resource ratings would also have to be guessed by whoever imported
        // the asset, and a guessed number folded into the level's own would make it meaningless.
        
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
        }
        public ResourceMeta(ResourceType resourceType, TypedResourceId resourceId, IString resourceTitle,
            IString resourceDescription, string resourceUrl, ILicense resourceLicense,
            List<IString> resourceSources, List<Author> resourceAuthors,
            List<PermissionGrant> resourcePermissions = null, List<string> resourceHashes = null)
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
        }
        public void Reset()
        {
            ResourceType = ResourceType.Bytes;
            ResourceId = TypedResourceId.Null;
            ResourceTitle = new StringValue();
            ResourceDescription = new StringValue();
            ResourceUrl = string.Empty;
            ResourceLicense = new NoSpecifiedLicense();
            ResourceSources.Clear();
            ResourceAuthors.Clear();
            ResourcePermissions.Clear();
            ResourceHashes.Clear();
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

        public object Clone() => Copy();
        public ResourceMeta Copy() => new(ResourceType, ResourceId, ResourceTitle.Copy(),
            ResourceDescription.Copy(), ResourceUrl, ResourceLicense.Copy(),
            ResourceSources.CopyList(), ResourceAuthors.CopyList(),
            ResourcePermissions.CopyList(), new List<string>(ResourceHashes));

        public void Update(ResourceMeta src)
        {
            ResourceType = src.ResourceType;
            ResourceId = src.ResourceId;
            ResourceTitle = src.ResourceTitle.Copy();
            ResourceDescription = src.ResourceDescription.Copy();
            ResourceUrl = src.ResourceUrl;
            ResourceLicense = src.ResourceLicense.Copy();
            ResourceSources = src.ResourceSources.CopyList();
            ResourceAuthors = src.ResourceAuthors.CopyList();
            ResourcePermissions = src.ResourcePermissions.CopyList();
            ResourceHashes = new List<string>(src.ResourceHashes);
        }

        public void Pull(ResourceMeta src)
        {
            ResourceType = src.ResourceType;
            ResourceId = src.ResourceId;
            ResourceTitle = ResourceTitle.PullFrom(src.ResourceTitle);
            ResourceDescription = ResourceDescription.PullFrom(src.ResourceDescription);
            ResourceUrl = src.ResourceUrl;
            ResourceLicense = ResourceLicense.PullFrom(src.ResourceLicense);
            ResourceSources = src.ResourceSources.CopyList();
            ResourceAuthors = src.ResourceAuthors.CopyList();
            ResourcePermissions = src.ResourcePermissions.CopyList();
            ResourceHashes = new List<string>(src.ResourceHashes);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ResourceMeta)obj);
        }
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add((int)ResourceType);
            hashCode.Add(ResourceId);
            hashCode.Add(ResourceTitle);
            hashCode.Add(ResourceDescription);
            hashCode.Add(ResourceUrl);
            hashCode.Add(ResourceLicense);
            hashCode.Add(ResourceSources.GetListHashCode());
            hashCode.Add(ResourceAuthors.GetListHashCode());
            hashCode.Add(ResourcePermissions.GetListHashCode());
            hashCode.Add(ResourceHashes.GetListHashCode());
            return hashCode.ToHashCode();
        }

        public bool Equals(ResourceMeta other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ResourceType == other.ResourceType
                   && ResourceId == other.ResourceId
                   && ResourceTitle.Equals(other.ResourceTitle)
                   && ResourceDescription.Equals(other.ResourceDescription)
                   && ResourceUrl.Equals(other.ResourceUrl)
                   && ResourceLicense.Equals(other.ResourceLicense)
                   && ResourceSources.ListEquals(other.ResourceSources)
                   && ResourceAuthors.ListEquals(other.ResourceAuthors)
                   && ResourcePermissions.ListEquals(other.ResourcePermissions)
                   && ResourceHashes.ListEquals(other.ResourceHashes);
        }
    }
}
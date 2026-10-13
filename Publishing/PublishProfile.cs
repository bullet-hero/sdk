using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Publishing
{
    // The publishing policy as DATA: a new service is a file, never an analyzer edit. Which typical
    // licenses are acceptable lives HERE and nowhere else (it is the service's opinion, not a fact
    // about the license). What a caller does with the answer is not decided here: Docs/PUBLISHING.md.

    /// <summary> One service's conditions for accepting a level. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.PublishProfile, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class PublishProfile : IModel<PublishProfile>
    {
        /// <summary> Which service this is the policy of ("ows-default", "workshop"). Reported back
        /// in every report, since the same level passes one profile and fails another. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ProfileKey)]
        public string ProfileKey { get; set; }

        /// <summary> Typical licenses the service accepts. Empty means it accepts any of them -
        /// which is what a purely local profile wants, not an oversight. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AllowedLicenses)]
        public List<TypicalLicenseType> AllowedLicenses { get; set; }

        /// <summary> How resources may be fetched. Empty means any way. Needs the level file to
        /// check, so it is silently skipped on a meta-only pass. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AllowedUriTypes)]
        public List<ResourceUriType> AllowedUriTypes { get; set; }

        /// <summary> Whether a resource may say nothing about its terms. </summary>
        [JsonProperty(Names.AllowUnknownLicense)]
        public bool AllowUnknownLicense { get; set; }

        /// <summary> Whether a rights holder's permission can carry a resource whose own license the
        /// service would otherwise refuse - Option B of the licensing policy. Such a resource is
        /// never published silently: it is always raised for review. </summary>
        [JsonProperty(Names.AllowPermissionInstead)]
        public bool AllowPermissionInstead { get; set; }

        /// <summary> Whether every user-defined resource of the level needs its own record. </summary>
        [JsonProperty(Names.RequireResourceMeta)]
        public bool RequireResourceMeta { get; set; }

        /// <summary> Whether a record must name the page the work came from. </summary>
        [JsonProperty(Names.RequireResourceUrl)]
        public bool RequireResourceUrl { get; set; }

        /// <summary> Whether every resource must credit somebody. A license that demands attribution
        /// is enforced regardless of this flag - the flag only decides whether the service also
        /// demands it for works that do not. </summary>
        [JsonProperty(Names.RequireAttribution)]
        public bool RequireAttribution { get; set; }

        /// <summary> Whether the level must declare an age rating. </summary>
        [JsonProperty(Names.RequireAgeRating)]
        public bool RequireAgeRating { get; set; }

        /// <summary> Whether the level must credit its own authors. </summary>
        [JsonProperty(Names.RequireLevelAuthors)]
        public bool RequireLevelAuthors { get; set; }

        /// <summary> Whether every record must carry a content hash, so a later takedown can find
        /// every level holding the same work. </summary>
        [JsonProperty(Names.RequireHashes)]
        public bool RequireHashes { get; set; }

        // Three ceilings because they fail differently (one asset, level.json, the total); bytes,
        // zero means no limit. Docs/PUBLISHING.md.

        /// <summary> Largest single resource file the service accepts. Zero means no limit. </summary>
        [JsonProperty(Names.MaxResourceBytes)]
        public long MaxResourceBytes { get; set; }

        /// <summary> Largest level.json/metadata.json the service accepts, each on its own. Zero
        /// means no limit. </summary>
        [JsonProperty(Names.MaxDataFileBytes)]
        public long MaxDataFileBytes { get; set; }

        /// <summary> Largest whole level the service accepts, everything included. Zero means no
        /// limit. </summary>
        [JsonProperty(Names.MaxTotalBytes)]
        public long MaxTotalBytes { get; set; }

        /// <summary> The site roster this service grades resources against. Empty disables source
        /// grading entirely. </summary>
        [RuleNotNull, RuleCollectionNoNullItems]
        [JsonProperty(Names.Sources)]
        public List<TrustedSource> Sources { get; set; }

        /// <summary> What a site absent from the roster counts as. Never Approved in a shipped
        /// profile - "we have never heard of it" is not an endorsement. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.UnknownSourceTrust)]
        public SourceTrust UnknownSourceTrust { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public PublishProfile()
        {
            ProfileKey = string.Empty;
            AllowedLicenses = new List<TypicalLicenseType>();
            AllowedUriTypes = new List<ResourceUriType>();
            AllowUnknownLicense = true;
            AllowPermissionInstead = true;
            RequireResourceMeta = false;
            RequireResourceUrl = false;
            RequireAttribution = false;
            RequireAgeRating = false;
            RequireLevelAuthors = false;
            RequireHashes = false;
            MaxResourceBytes = 0;
            MaxDataFileBytes = 0;
            MaxTotalBytes = 0;
            Sources = new List<TrustedSource>();
            UnknownSourceTrust = SourceTrust.Unknown;
        }

        /// <summary> True when the service accepts this typical license. An empty list accepts
        /// every one of them. </summary>
        public bool AllowsLicense(TypicalLicenseType type)
            => AllowedLicenses.Count == 0 || AllowedLicenses.Contains(type);

        /// <summary> True when the service accepts resources fetched this way. An empty list
        /// accepts every way. </summary>
        public bool AllowsUriType(ResourceUriType uriType)
            => AllowedUriTypes.Count == 0 || AllowedUriTypes.Contains(uriType);

        /// <summary> Whether this service bounds sizes at all - i.e. whether a check needs measured
        /// sizes handed to it to be complete. </summary>
        public bool HasSizeLimits => MaxResourceBytes > 0 || MaxDataFileBytes > 0 || MaxTotalBytes > 0;

        // A rostered site and an unrostered one may carry the same grade on purpose; reporting is
        // where they diverge (the first names the entry, the second says there is none).

        /// <summary> The roster entry covering a URL's site, if any. </summary>
        public bool TryGetSource(string url, out TrustedSource source)
        {
            source = null;
            if (Sources.Count == 0) return false;

            var host = TrustedSource.ExtractHost(url);
            if (string.IsNullOrEmpty(host)) return false;

            foreach (var candidate in Sources)
            {
                if (candidate == null || !candidate.CoversHost(host)) continue;
                source = candidate;
                return true;
            }

            return false;
        }

        /// <summary> How the service grades the site a URL points at, roster miss included. </summary>
        public SourceTrust GetTrust(string url)
        {
            if (Sources.Count == 0) return SourceTrust.Approved;
            return TryGetSource(url, out var source) ? source.Trust : UnknownSourceTrust;
        }

        // The three shipped profiles: exists at all, may go to strangers, may go to strangers on a
        // platform with store rules on top. An operator edits the json, not this class.

        /// <summary> Nothing is required. What a device applies to its own files - a level a player
        /// made for themselves is nobody else's business. </summary>
        public static PublishProfile CreateOpen() => new()
        {
            ProfileKey = "local",
            AllowUnknownLicense = true,
            AllowPermissionInstead = true,
            UnknownSourceTrust = SourceTrust.Approved,
        };

        // GPL/AGPL/LGPL absent on purpose (App Store terms collide); CC BY-SA and CC BY-ND absent per
        // the public UGC licensing policy. Reasons: Docs/PUBLISHING.md.

        /// <summary> What the public UGC licensing policy (https://bullethero.space/en/notes/ugc-licensing-policy) describes: a level publishable as CC BY-NC because
        /// every resource in it already permits that. The baseline for a public server. </summary>
        public static PublishProfile CreateStandard() => new()
        {
            ProfileKey = "standard",
            AllowedLicenses = new List<TypicalLicenseType>
            {
                TypicalLicenseType.CC0_1_0,
                TypicalLicenseType.CC_BY_4_0,
                TypicalLicenseType.CC_BY_3_0,
                TypicalLicenseType.CC_BY_NC_4_0,
                TypicalLicenseType.CC_BY_NC_3_0,
                TypicalLicenseType.SIL_OFL_1_1,
                TypicalLicenseType.MIT,
                TypicalLicenseType.Apache_2_0,
                TypicalLicenseType.Unlicensed,
            },
            AllowedUriTypes = new List<ResourceUriType>
            {
                ResourceUriType.LevelPath,
                ResourceUriType.StreamingAssets,
                ResourceUriType.DirectUrl,
            },
            AllowUnknownLicense = false,
            AllowPermissionInstead = true,
            RequireResourceMeta = true,
            RequireResourceUrl = true,
            RequireAttribution = false,
            RequireAgeRating = true,
            RequireLevelAuthors = true,
            RequireHashes = false,
            MaxResourceBytes = 64 * ByteSizeUtils.Megabyte,
            MaxDataFileBytes = 32 * ByteSizeUtils.Megabyte,
            MaxTotalBytes = 256 * ByteSizeUtils.Megabyte,
            Sources = TrustedSourceCatalog.CreateDefault(),
            UnknownSourceTrust = SourceTrust.RequiresLicenseCheck,
        };

        // The Workshop asks LESS than a public server, on purpose: refuse only where plainly unsafe.
        // ND and SA pass here; what they demand of the level is a compatibility warning (tolerant
        // profile). A public server ASKS instead of refusing. Docs/PUBLISHING.md.

        /// <summary> The Steam Workshop's conditions: every CC BY variant, CC0, OFL, MIT, Apache and the
        /// Unlicense pass; missing paperwork is a warning; the public server's sizes. </summary>
        public static PublishProfile CreateWorkshop() => new()
        {
            ProfileKey = "workshop",
            AllowedLicenses = new List<TypicalLicenseType>
            {
                TypicalLicenseType.CC0_1_0,
                TypicalLicenseType.CC_BY_4_0,
                TypicalLicenseType.CC_BY_SA_4_0,
                TypicalLicenseType.CC_BY_NC_4_0,
                TypicalLicenseType.CC_BY_NC_SA_4_0,
                TypicalLicenseType.CC_BY_ND_4_0,
                TypicalLicenseType.CC_BY_NC_ND_4_0,
                TypicalLicenseType.CC_BY_3_0,
                TypicalLicenseType.CC_BY_SA_3_0,
                TypicalLicenseType.CC_BY_NC_3_0,
                TypicalLicenseType.CC_BY_NC_SA_3_0,
                TypicalLicenseType.CC_BY_ND_3_0,
                TypicalLicenseType.CC_BY_NC_ND_3_0,
                TypicalLicenseType.SIL_OFL_1_1,
                TypicalLicenseType.MIT,
                TypicalLicenseType.Apache_2_0,
                TypicalLicenseType.Unlicensed,
            },
            AllowedUriTypes = new List<ResourceUriType>
            {
                ResourceUriType.LevelPath,
                ResourceUriType.StreamingAssets,
                ResourceUriType.DirectUrl,
            },
            AllowUnknownLicense = true,
            AllowPermissionInstead = true,
            RequireResourceMeta = false,
            RequireResourceUrl = true,
            RequireAttribution = false,
            RequireAgeRating = false,
            RequireLevelAuthors = false,
            RequireHashes = false,
            MaxResourceBytes = 64 * ByteSizeUtils.Megabyte,
            MaxDataFileBytes = 32 * ByteSizeUtils.Megabyte,
            MaxTotalBytes = 256 * ByteSizeUtils.Megabyte,
            Sources = TrustedSourceCatalog.CreateDefault(),
            UnknownSourceTrust = SourceTrust.RequiresLicenseCheck,
        };

        // The tighter sizes are not a stricter opinion about the same thing - they are what a phone
        // can actually download over a mobile connection and keep on a device that is already full.

        /// <summary> Standard plus what a store build needs: no arbitrary URLs (a fetch nobody
        /// moderated), attribution on everything, a hash on every record so a takedown can be
        /// answered across the whole catalogue, and sizes a phone can carry. </summary>
        public static PublishProfile CreateStrict()
        {
            var profile = CreateStandard();
            profile.ProfileKey = "strict";
            profile.AllowedUriTypes = new List<ResourceUriType>
            {
                ResourceUriType.LevelPath,
                ResourceUriType.StreamingAssets,
            };
            profile.RequireAttribution = true;
            profile.RequireHashes = true;
            profile.MaxResourceBytes = 32 * ByteSizeUtils.Megabyte;
            profile.MaxDataFileBytes = 16 * ByteSizeUtils.Megabyte;
            profile.MaxTotalBytes = 128 * ByteSizeUtils.Megabyte;
            profile.UnknownSourceTrust = SourceTrust.RequiresResourceCheck;
            return profile;
        }
    }
}
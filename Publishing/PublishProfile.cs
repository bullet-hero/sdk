using System;
using System.Collections.Generic;
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
    // The publishing policy as DATA, which is the whole design. Every service a level can reach -
    // Steam Workshop, the official server, a community-run one, a store build's own catalogue - wants
    // a different answer to the same handful of questions, and those answers change over the years
    // while the code does not. A profile is one such answer, serialized, so adding a service means
    // writing a file and adding a store means shipping a stricter one, never editing an analyzer.
    //
    // Which typical licenses are acceptable lives HERE and only here. It reads like a property of
    // the license ("is CC BY-SA allowed?") but it is a property of the receiving service: BY-SA is
    // fine for a server that does not mind ShareAlike propagating, and refused by one that publishes
    // everything as CC BY-NC. A table baked into the SDK would state one service's opinion as if it
    // were a fact about the license.
    //
    // What the profile deliberately does NOT decide is what a caller does with the answer. A client
    // blocks an upload on HasErrors; a server queues NeedsManualReview instead of publishing; a
    // catalogue re-runs the whole thing when the policy changes and re-sorts what it already holds.

    /// <summary> One service's conditions for accepting a level. </summary>
    [RuleContainer]
    [DataVersion(DataDomains.PublishProfile, 1, 0)]
    public class PublishProfile : IModel<PublishProfile>
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

        // Three separate ceilings rather than one, because they fail differently. A single huge file
        // is usually one unconverted asset and the author can fix it by re-encoding; an oversized
        // level.json is a structural problem (hundreds of thousands of keyframes) that no re-encode
        // helps; and a total that fits neither pattern is simply a level nobody can download on a
        // phone. Sizes are BYTES - see ByteSizeUtils for why there is no size type - and zero means
        // no limit, so a profile that cares about none of this stays all-zero.

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
        public void Reset()
        {
            ProfileKey = string.Empty;
            AllowedLicenses.Clear();
            AllowedUriTypes.Clear();
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
            Sources.Clear();
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

        // Whether the roster KNOWS a site and how it grades it are two answers, and a caller that
        // only ever asks for the grade cannot tell "we vetted this and it needs reading" from "we
        // have never heard of it" - the profile maps both onto the same value on purpose. Reporting
        // is where they diverge: the first names the entry, the second says there is none.

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

        // The three shipped profiles are the three questions actually being asked today: may this
        // level exist at all (yes, always), may it be handed to strangers, and may it be handed to
        // strangers on a platform whose own store rules apply on top. An operator writes their own
        // by editing the json, not by adding a factory here.

        /// <summary> Nothing is required. What a device applies to its own files - a level a player
        /// made for themselves is nobody else's business. </summary>
        public static PublishProfile CreateOpen() => new()
        {
            ProfileKey = "local",
            AllowUnknownLicense = true,
            AllowPermissionInstead = true,
            UnknownSourceTrust = SourceTrust.Approved,
        };

        // GPL/AGPL/LGPL are absent on purpose, and it is not about the licenses being unsuitable for
        // a level. A GPL work redistributed through the App Store collides with Apple's own terms -
        // the VLC case - so allowing it here would build a catalogue that cannot be served to iOS
        // later. MIT/Apache cover the same ground (a text, a data file) with no such collision.
        //
        // CC BY-SA and CC BY-ND are absent for the reason UGC-LICENSING-POLICY.md gives: ShareAlike
        // would force the level's own license to change, NoDerivatives would forbid the editing the
        // game is built around.

        /// <summary> What UGC-LICENSING-POLICY.md describes: a level publishable as CC BY-NC because
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
                ResourceUriType.AddressableKey,
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
                ResourceUriType.AddressableKey,
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

        public object Clone() => Copy();
        public PublishProfile Copy() => new()
        {
            ProfileKey = ProfileKey,
            AllowedLicenses = new List<TypicalLicenseType>(AllowedLicenses),
            AllowedUriTypes = new List<ResourceUriType>(AllowedUriTypes),
            AllowUnknownLicense = AllowUnknownLicense,
            AllowPermissionInstead = AllowPermissionInstead,
            RequireResourceMeta = RequireResourceMeta,
            RequireResourceUrl = RequireResourceUrl,
            RequireAttribution = RequireAttribution,
            RequireAgeRating = RequireAgeRating,
            RequireLevelAuthors = RequireLevelAuthors,
            RequireHashes = RequireHashes,
            MaxResourceBytes = MaxResourceBytes,
            MaxDataFileBytes = MaxDataFileBytes,
            MaxTotalBytes = MaxTotalBytes,
            Sources = Sources.CopyList(),
            UnknownSourceTrust = UnknownSourceTrust,
        };

        public void Update(PublishProfile src)
        {
            ProfileKey = src.ProfileKey;
            AllowedLicenses = new List<TypicalLicenseType>(src.AllowedLicenses);
            AllowedUriTypes = new List<ResourceUriType>(src.AllowedUriTypes);
            AllowUnknownLicense = src.AllowUnknownLicense;
            AllowPermissionInstead = src.AllowPermissionInstead;
            RequireResourceMeta = src.RequireResourceMeta;
            RequireResourceUrl = src.RequireResourceUrl;
            RequireAttribution = src.RequireAttribution;
            RequireAgeRating = src.RequireAgeRating;
            RequireLevelAuthors = src.RequireLevelAuthors;
            RequireHashes = src.RequireHashes;
            MaxResourceBytes = src.MaxResourceBytes;
            MaxDataFileBytes = src.MaxDataFileBytes;
            MaxTotalBytes = src.MaxTotalBytes;
            Sources = src.Sources.CopyList();
            UnknownSourceTrust = src.UnknownSourceTrust;
        }

        public void Pull(PublishProfile src)
        {
            ProfileKey = src.ProfileKey;
            AllowedLicenses = new List<TypicalLicenseType>(src.AllowedLicenses);
            AllowedUriTypes = new List<ResourceUriType>(src.AllowedUriTypes);
            AllowUnknownLicense = src.AllowUnknownLicense;
            AllowPermissionInstead = src.AllowPermissionInstead;
            RequireResourceMeta = src.RequireResourceMeta;
            RequireResourceUrl = src.RequireResourceUrl;
            RequireAttribution = src.RequireAttribution;
            RequireAgeRating = src.RequireAgeRating;
            RequireLevelAuthors = src.RequireLevelAuthors;
            RequireHashes = src.RequireHashes;
            MaxResourceBytes = src.MaxResourceBytes;
            MaxDataFileBytes = src.MaxDataFileBytes;
            MaxTotalBytes = src.MaxTotalBytes;
            Sources = src.Sources.CopyList();
            UnknownSourceTrust = src.UnknownSourceTrust;
        }

        public override bool Equals(object obj) => obj is PublishProfile value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(ProfileKey);
            hashCode.Add(AllowedLicenses.GetListHashCode());
            hashCode.Add(AllowedUriTypes.GetListHashCode());
            hashCode.Add(AllowUnknownLicense);
            hashCode.Add(AllowPermissionInstead);
            hashCode.Add(RequireResourceMeta);
            hashCode.Add(RequireResourceUrl);
            hashCode.Add(RequireAttribution);
            hashCode.Add(RequireAgeRating);
            hashCode.Add(RequireLevelAuthors);
            hashCode.Add(RequireHashes);
            hashCode.Add(MaxResourceBytes);
            hashCode.Add(MaxDataFileBytes);
            hashCode.Add(MaxTotalBytes);
            hashCode.Add(Sources.GetListHashCode());
            hashCode.Add((int)UnknownSourceTrust);
            return hashCode.ToHashCode();
        }

        public bool Equals(PublishProfile other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ProfileKey.Equals(other.ProfileKey)
                   && AllowedLicenses.ListEquals(other.AllowedLicenses)
                   && AllowedUriTypes.ListEquals(other.AllowedUriTypes)
                   && AllowUnknownLicense == other.AllowUnknownLicense
                   && AllowPermissionInstead == other.AllowPermissionInstead
                   && RequireResourceMeta == other.RequireResourceMeta
                   && RequireResourceUrl == other.RequireResourceUrl
                   && RequireAttribution == other.RequireAttribution
                   && RequireAgeRating == other.RequireAgeRating
                   && RequireLevelAuthors == other.RequireLevelAuthors
                   && RequireHashes == other.RequireHashes
                   && MaxResourceBytes == other.MaxResourceBytes
                   && MaxDataFileBytes == other.MaxDataFileBytes
                   && MaxTotalBytes == other.MaxTotalBytes
                   && Sources.ListEquals(other.Sources)
                   && UnknownSourceTrust == other.UnknownSourceTrust;
        }
    }
}

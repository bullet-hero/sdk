using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Publishing
{
    // The third validation pass, and the only one that asks a question about the outside world.
    // RuleAnalyzer asks whether a value is in range; LevelGraphAnalyzer asks whether the objects
    // agree with each other; this asks whether the level may be handed to strangers - which nothing
    // in the file can answer alone, only the file plus a service's policy.
    //
    // Nothing here repairs anything, for the same reason graph findings never do: every fix is a
    // fact the author has to supply. Naming a license nobody read, or crediting an author nobody
    // identified, would be the analyzer inventing the very paperwork it exists to demand.
    //
    // The level file is optional and that is a designed capability, not a convenience. metadata.json
    // is its own serialization root so a catalogue can grade thousands of levels without opening one
    // of them, and the meta-only pass really does cover most of the policy. Exactly two findings need
    // level.json - a resource with no record at all (ResourceMetaMissing) and where a resource is
    // fetched from (ResourceUriTypeNotAllowed) - and the report says which pass it was, so a clean
    // meta-only result is never mistaken for a clean full one.

    /// <summary> Checks a level against one service's publishing conditions. </summary>
    public class PublishReadinessAnalyzer
    {
        /// <summary>
        /// Grade a level for one service. Pass the level file whenever it is available - without it
        /// resource coverage and fetch locations cannot be checked, and the report says so.
        /// `now` (UTC) decides whether permissions have lapsed; leaving it unset means "no clock",
        /// under which no permission ever expires.
        /// </summary>
        public PublishReadinessReport Analyze(LevelMeta meta, PublishProfile profile,
            Level level = null, DateTime now = default, PublishPayload payload = null)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var issues = new List<PublishIssue>();

            AnalyzeLevelMeta(meta, profile, issues);
            foreach (var resourceMeta in meta.ResourcesMeta)
            {
                if (resourceMeta == null) continue;
                AnalyzeResourceMeta(resourceMeta, profile, now, issues);
                AnalyzeResourceSize(resourceMeta, profile, payload, issues);
            }

            if (level != null) AnalyzeLevel(meta, level, profile, issues);
            if (payload != null) AnalyzePayloadSize(payload, profile, issues);

            // Sizes are only a missing input where the profile bounds them; for one that does not,
            // a caller that measured nothing has still supplied everything the check needs.
            var inputsComplete = level != null && (!profile.HasSizeLimits || payload != null);

            return new PublishReadinessReport(issues, level != null, payload != null,
                inputsComplete, profile.ProfileKey);
        }

        #region Level meta

        private static void AnalyzeLevelMeta(LevelMeta meta, PublishProfile profile,
            List<PublishIssue> issues)
        {
            if (!IsLicenseAcceptable(meta.LevelLicense, profile, out var unspecified))
            {
                issues.Add(new PublishIssue(PublishRule.LevelLicenseNotAllowed, RuleGroup.Error,
                    "meta.license",
                    unspecified
                        ? "The level states no license of its own."
                        : $"The level's license is not accepted by profile '{profile.ProfileKey}'."));
            }

            if (profile.RequireAgeRating && meta.LevelAgeRating == AgeRating.Unrated)
            {
                issues.Add(new PublishIssue(PublishRule.LevelAgeRatingMissing, RuleGroup.Error,
                    "meta.age_rating", "The level declares no age rating."));
            }

            if (profile.RequireLevelAuthors && (meta.LevelAuthors == null || meta.LevelAuthors.Count == 0))
            {
                issues.Add(new PublishIssue(PublishRule.LevelAuthorsMissing, RuleGroup.Error,
                    "meta.authors", "The level credits no authors."));
            }

            if (meta.LevelLogo != null && !profile.AllowsUriType(meta.LevelLogo.UriType))
            {
                issues.Add(new PublishIssue(PublishRule.ResourceUriTypeNotAllowed, RuleGroup.Error,
                    "meta.logo", $"The logo is fetched as {meta.LevelLogo.UriType}, " +
                                 $"which profile '{profile.ProfileKey}' does not accept."));
            }
        }

        #endregion

        #region Resource meta

        private static void AnalyzeResourceMeta(ResourceMeta resourceMeta, PublishProfile profile,
            DateTime now, List<PublishIssue> issues)
        {
            var path = DescribeResource(resourceMeta.ResourceType, resourceMeta.ResourceId.value);

            AnalyzeResourceLicense(resourceMeta, profile, now, path, issues);
            AnalyzePermissions(resourceMeta, now, path, issues);
            AnalyzeAttribution(resourceMeta, profile, path, issues);

            if (profile.RequireResourceUrl && string.IsNullOrWhiteSpace(resourceMeta.ResourceUrl))
            {
                issues.Add(new PublishIssue(PublishRule.ResourceUrlMissing, RuleGroup.Error, path,
                    "The record names no page the work can be traced back to."));
            }

            if (profile.RequireHashes && resourceMeta.ResourceHashes.Count == 0)
            {
                issues.Add(new PublishIssue(PublishRule.ResourceHashMissing, RuleGroup.Error, path,
                    "The record carries no content hash, so a takedown could not find this work again."));
            }

            AnalyzeSourceTrust(resourceMeta, profile, path, issues);
        }

        // A permission does not make a refused license acceptable - it makes it a question for a
        // person. Option B is a claim about a private exchange, and the whole reason the grant
        // carries proof is that somebody is expected to open it, so this path always produces a
        // review rather than a pass.

        private static void AnalyzeResourceLicense(ResourceMeta resourceMeta, PublishProfile profile,
            DateTime now, string path, List<PublishIssue> issues)
        {
            if (IsLicenseAcceptable(resourceMeta.ResourceLicense, profile, out var unspecified)) return;

            var rule = unspecified
                ? PublishRule.ResourceLicenseUnspecified
                : PublishRule.ResourceLicenseNotAllowed;

            var covered = profile.AllowPermissionInstead
                          && TryGetUsablePermission(resourceMeta, now, out _);

            if (covered)
            {
                issues.Add(new PublishIssue(rule, RuleGroup.Warning, path,
                    "The license alone does not permit publishing; a rights holder's permission is " +
                    "claimed instead and has to be checked by hand."));
                return;
            }

            issues.Add(new PublishIssue(rule, RuleGroup.Error, path,
                unspecified
                    ? "The record states no license, and no permission stands in for one." +
                      DescribeUnlicensedSource(resourceMeta.ResourceLicense)
                    : $"The license is not accepted by profile '{profile.ProfileKey}'."));
        }

        private static void AnalyzePermissions(ResourceMeta resourceMeta, DateTime now, string path,
            List<PublishIssue> issues)
        {
            if (resourceMeta.ResourcePermissions.Count == 0) return;

            var anyActive = false;
            foreach (var permission in resourceMeta.ResourcePermissions)
            {
                if (permission == null) continue;
                if (permission.IsActiveAt(now)) anyActive = true;

                if (permission.Scope == PermissionScope.Undefined || !permission.HasProof())
                {
                    issues.Add(new PublishIssue(PublishRule.PermissionIncomplete, RuleGroup.Warning,
                        path, "A permission names no scope or points at no evidence, so nobody can " +
                              "verify what was actually allowed."));
                }
            }

            if (!anyActive)
            {
                issues.Add(new PublishIssue(PublishRule.PermissionExpired, RuleGroup.Warning, path,
                    "Every permission recorded for this resource has lapsed."));
            }
        }

        // THE PROFILE IS NOW THE ONLY REASON TO DEMAND A CREDIT. A custom license used to state its
        // own RequiresAttribution, and that bool is gone along with the other six - it was the
        // author's own grading of their own wording, which is exactly the claim this analyzer cannot
        // verify. Typical licenses are still deliberately not decoded: whether CC BY "requires
        // attribution" is a fact, but which typical licenses a service accepts at all is already the
        // profile's decision, and hard-coding a rights table next to it would put the same policy in
        // two places.

        private static void AnalyzeAttribution(ResourceMeta resourceMeta, PublishProfile profile,
            string path, List<PublishIssue> issues)
        {
            var hasAuthors = resourceMeta.ResourceAuthors is { Count: > 0 };
            if (hasAuthors) return;

            if (!profile.RequireAttribution) return;

            issues.Add(new PublishIssue(PublishRule.ResourceAttributionMissing, RuleGroup.Error, path,
                "The service requires every resource to credit somebody."));
        }

        private static void AnalyzeSourceTrust(ResourceMeta resourceMeta, PublishProfile profile,
            string path, List<PublishIssue> issues)
        {
            if (profile.Sources.Count == 0) return;
            if (string.IsNullOrWhiteSpace(resourceMeta.ResourceUrl)) return;

            // A rostered site and an unrostered one can carry the very same grade - most profiles map
            // "never heard of it" onto RequiresLicenseCheck - so the grade alone cannot decide which
            // finding this is. The rule comes from whether an entry was found, and only the severity
            // comes from the grade.

            var known = profile.TryGetSource(resourceMeta.ResourceUrl, out var source);
            var trust = known ? source.Trust : profile.UnknownSourceTrust;
            if (trust == SourceTrust.Approved) return;

            if (!known)
            {
                issues.Add(new PublishIssue(PublishRule.SourceUnknown,
                    trust == SourceTrust.NotAllowed ? RuleGroup.Error : RuleGroup.Warning, path,
                    "The site this came from is in no roster entry, so nothing is known about its " +
                    $"terms; this service treats such a site as {trust}."));
                return;
            }

            if (trust == SourceTrust.NotAllowed)
            {
                issues.Add(new PublishIssue(PublishRule.SourceNotAllowed, RuleGroup.Error, path,
                    $"Nothing may be published from {source.Title}. {source.Note}".TrimEnd()));
                return;
            }

            issues.Add(new PublishIssue(PublishRule.SourceNeedsReview, RuleGroup.Warning, path,
                $"{source.Title} is graded {trust}, so this record has to be confirmed by hand. " +
                $"{source.Note}".TrimEnd()));
        }

        #endregion

        #region Sizes

        // A size of zero is "not measured", never "an empty file": a caller hands over what it could
        // measure, and a resource whose file it never found must not be reported as comfortably
        // within a limit it was never checked against.

        private static void AnalyzeResourceSize(ResourceMeta resourceMeta, PublishProfile profile,
            PublishPayload payload, List<PublishIssue> issues)
        {
            if (payload == null || profile.MaxResourceBytes <= 0) return;

            var bytes = payload.GetResourceBytes(resourceMeta.ResourceType, resourceMeta.ResourceId);
            if (bytes <= 0 || bytes <= profile.MaxResourceBytes) return;

            issues.Add(new PublishIssue(PublishRule.ResourceTooLarge, RuleGroup.Error,
                DescribeResource(resourceMeta.ResourceType, resourceMeta.ResourceId.value),
                $"The file is {ByteSizeUtils.Format(bytes)}, over this service's " +
                $"{ByteSizeUtils.Format(profile.MaxResourceBytes)} limit for one resource."));
        }

        private static void AnalyzePayloadSize(PublishPayload payload, PublishProfile profile,
            List<PublishIssue> issues)
        {
            if (profile.MaxDataFileBytes > 0)
            {
                AddDataFileIssue(payload.LevelBytes, profile.MaxDataFileBytes, "level", issues);
                AddDataFileIssue(payload.MetaBytes, profile.MaxDataFileBytes, "meta", issues);
            }

            if (profile.MaxTotalBytes <= 0) return;
            if (payload.TotalBytes <= 0 || payload.TotalBytes <= profile.MaxTotalBytes) return;

            issues.Add(new PublishIssue(PublishRule.PayloadTooLarge, RuleGroup.Error, "level",
                $"The level weighs {ByteSizeUtils.Format(payload.TotalBytes)}, over this service's " +
                $"{ByteSizeUtils.Format(profile.MaxTotalBytes)} limit."));
        }

        private static void AddDataFileIssue(long bytes, long limit, string path,
            List<PublishIssue> issues)
        {
            if (bytes <= 0 || bytes <= limit) return;

            issues.Add(new PublishIssue(PublishRule.DataFileTooLarge, RuleGroup.Error, path,
                $"The file is {ByteSizeUtils.Format(bytes)}, over this service's " +
                $"{ByteSizeUtils.Format(limit)} limit for one data file."));
        }

        #endregion

        #region Level

        private static void AnalyzeLevel(LevelMeta meta, Level level, PublishProfile profile,
            List<PublishIssue> issues)
        {
            var covered = new HashSet<(ResourceType, int)>();
            foreach (var resourceMeta in meta.ResourcesMeta)
            {
                if (resourceMeta == null) continue;
                covered.Add((resourceMeta.ResourceType, resourceMeta.ResourceId.value));
            }

            var present = new HashSet<(ResourceType, int)>();
            var resources = level.Resources;
            if (resources != null)
            {
                foreach (var pair in resources.Textures)
                    AnalyzeLevelResource(ResourceType.Texture, pair.Key.value, pair.Value,
                        profile, covered, present, issues);
                foreach (var pair in resources.Fonts)
                    AnalyzeLevelResource(ResourceType.Font, pair.Key.value, pair.Value,
                        profile, covered, present, issues);
                foreach (var pair in resources.Audios)
                    AnalyzeLevelResource(ResourceType.Audio, pair.Key.value, pair.Value,
                        profile, covered, present, issues);
            }

            // Only the three families a level can actually hold are checked for orphans. Bytes and
            // Text records describe resources LevelResources has no dictionary for, so "no matching
            // resource" is their normal state, not a finding.
            foreach (var entry in covered)
            {
                if (present.Contains(entry)) continue;
                if (entry.Item1 != ResourceType.Texture
                    && entry.Item1 != ResourceType.Font
                    && entry.Item1 != ResourceType.Audio) continue;

                issues.Add(new PublishIssue(PublishRule.ResourceMetaOrphaned, RuleGroup.Advice,
                    DescribeResource(entry.Item1, entry.Item2),
                    "The record describes a resource this level does not have."));
            }
        }

        private static void AnalyzeLevelResource(ResourceType resourceType, int id, Resource resource,
            PublishProfile profile, HashSet<(ResourceType, int)> covered,
            HashSet<(ResourceType, int)> present, List<PublishIssue> issues)
        {
            var key = (resourceType, id);
            present.Add(key);
            var path = DescribeResource(resourceType, id);

            if (profile.RequireResourceMeta && !covered.Contains(key))
            {
                issues.Add(new PublishIssue(PublishRule.ResourceMetaMissing, RuleGroup.Error, path,
                    "The level ships this resource with no licensing record at all."));
            }

            if (resource?.Sources == null) return;
            foreach (var source in resource.Sources)
            {
                if (source == null || profile.AllowsUriType(source.UriType)) continue;
                issues.Add(new PublishIssue(PublishRule.ResourceUriTypeNotAllowed, RuleGroup.Error,
                    path, $"The resource is fetched as {source.UriType}, which profile " +
                          $"'{profile.ProfileKey}' does not accept."));
            }
        }

        #endregion

        #region Shared

        // A CUSTOM LICENSE IS NEVER ACCEPTABLE AUTOMATICALLY, and that is the honest answer rather
        // than a strict one. It used to be judged by its own AllowsDistribution bool, i.e. by the
        // author's own reading of their own wording; the profile's list knows the typical licenses
        // and nothing else, and no rule can grade prose it has never seen. So it lands as "not
        // accepted", which the permission path below still downgrades to a review when a rights
        // holder's grant stands behind it - a person reading the terms, which was the only thing
        // that ever really happened here.

        private static bool IsLicenseAcceptable(ILicense license, PublishProfile profile,
            out bool unspecified)
        {
            unspecified = false;

            switch (license)
            {
                case null:
                case NoSpecifiedLicense _:
                    unspecified = true;
                    return profile.AllowUnknownLicense;

                case TypicalLicense typical:
                    return profile.AllowsLicense(typical.Type);

                case CustomLicense _:
                    return false;

                default:
                    unspecified = true;
                    return profile.AllowUnknownLicense;
            }
        }

        private static bool TryGetUsablePermission(ResourceMeta resourceMeta, DateTime now,
            out PermissionGrant grant)
        {
            foreach (var permission in resourceMeta.ResourcePermissions)
            {
                if (permission == null) continue;
                if (permission.Scope == PermissionScope.Undefined) continue;
                if (!permission.HasProof()) continue;
                if (!permission.IsActiveAt(now)) continue;

                grant = permission;
                return true;
            }
            grant = null;
            return false;
        }

        private static string DescribeResource(ResourceType resourceType, int id)
            => $"meta.resources[{resourceType}:{id}]";

        // The platform an unlicensed work names is not another finding - it changes nothing about
        // the verdict, since no platform issues terms. It goes into the message because it is the
        // one line that tells a moderator which conversation to have.

        private static string DescribeUnlicensedSource(ILicense license)
        {
            if (license is not NoSpecifiedLicense unlicensed) return string.Empty;
            if (unlicensed.Source == NoLicenseSourceType.Undefined) return string.Empty;
            return $" Taken from {unlicensed.Source}, which licenses nothing to anyone.";
        }

        #endregion
    }
}

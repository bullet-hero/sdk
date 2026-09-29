using System;
using System.Collections.Generic;
using BH.SDK.Models.Collections;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Publishing
{
    // A COLLECTION IS GRADED ON WHETHER IT STANDS ON ITS OWN, since that is the one thing an importer
    // relies on and nobody can fix after the upload: every file the manifest names is in it, every user
    // resource its prefabs and effects point at is in it (a closure left behind is an unresolved
    // reference in every level that imports it), and it says who made it and under what terms. The
    // per-resource licence and attribution checks are the level analyzer's own, run on the credits the
    // collection carries; a profile's size cap applies to the whole folder, as it does to a level.
    //
    // It reads no files. What is present is handed in as a set of store paths and a byte count, so the
    // same check runs on a folder on disk, an unpacked upload on a server, or memory.

    /// <summary> Checks a collection against one service's publishing conditions. </summary>
    public class CollectionReadinessAnalyzer
    {
        /// <summary> Grades a collection. <paramref name="presentFiles"/> is every store path the
        /// collection holds; <paramref name="totalBytes"/> its size, or 0 when not measured. </summary>
        public PublishReadinessReport Analyze(ResourceCollection manifest, LevelResources resources,
            PublishProfile profile, ISet<string> presentFiles, long totalBytes = 0, DateTime now = default)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var issues = new List<PublishIssue>();

            if (IsBlank(manifest.Name))
                issues.Add(new PublishIssue(PublishRule.CollectionNameMissing, RuleGroup.Error, "collection",
                    "The collection has no name to be listed under."));

            // Blocking only where the profile asks for a licence at all: handing a file to a friend is not
            // publishing it, and a profile that allows an unknown licence says exactly that.
            if (manifest.License == null || manifest.License is NoSpecifiedLicense)
                issues.Add(new PublishIssue(PublishRule.CollectionLicenseUnspecified,
                    profile.AllowUnknownLicense ? RuleGroup.Warning : RuleGroup.Error, "collection",
                    "The collection states no license, so nobody may use what is in it."));

            if (Count(resources) == 0)
                issues.Add(new PublishIssue(PublishRule.CollectionEmpty, RuleGroup.Error, "collection",
                    "The collection holds nothing."));

            AnalyzeMedia(manifest, presentFiles, issues);
            AnalyzeClosure(resources, issues);

            // The level analyzer's own per-record checks, run on the collection's credits: a collection
            // is a set of resources whose licensing is exactly what a level importing it inherits.
            var meta = new BH.SDK.Models.LevelMeta { ResourcesMeta = manifest.ResourcesMeta };
            meta.LevelLicense = manifest.License;
            meta.LevelAuthors = manifest.Authors;
            var records = new PublishReadinessAnalyzer().Analyze(meta, profile, null, now, null, resources);
            foreach (var issue in records.Issues)
                if (issue.Path != null && issue.Path.StartsWith("meta.resources", StringComparison.Ordinal))
                    issues.Add(issue);

            if (profile.MaxTotalBytes > 0 && totalBytes > profile.MaxTotalBytes)
                issues.Add(new PublishIssue(PublishRule.PayloadTooLarge, RuleGroup.Error, "collection",
                    $"The collection weighs {ByteSizeUtils.Format(totalBytes)}, over this service's " +
                    $"{ByteSizeUtils.Format(profile.MaxTotalBytes)} limit.",
                    ByteSizeUtils.Format(totalBytes), ByteSizeUtils.Format(profile.MaxTotalBytes)));

            return new PublishReadinessReport(issues, levelInspected: true, payloadInspected: totalBytes > 0,
                inputsComplete: presentFiles != null && (!profile.HasSizeLimits || totalBytes > 0),
                profileKey: profile.ProfileKey);
        }

        private static void AnalyzeMedia(ResourceCollection manifest, ISet<string> presentFiles,
            List<PublishIssue> issues)
        {
            if (presentFiles == null) return;

            foreach (var texture in manifest.Textures.Values) Check(texture, ResourceType.Texture, texture.TextureResourceId.value);
            foreach (var font in manifest.Fonts.Values) Check(font, ResourceType.Font, font.FontResourceId.value);
            foreach (var audio in manifest.Audios.Values) Check(audio, ResourceType.Audio, audio.AudioResourceId.value);

            void Check(Resource resource, ResourceType type, int id)
            {
                foreach (var source in resource.Sources)
                {
                    if (source == null || source.UriType != ResourceUriType.LevelPath) continue;
                    if (presentFiles.Contains(source.Uri)) continue;

                    var name = ResourceNames.OfFile(resource, id);
                    issues.Add(new PublishIssue(PublishRule.CollectionMediaMissing, RuleGroup.Error,
                        $"collection.{type}:{id}",
                        $"'{source.Uri}', the file of {type} '{name}', is named by the manifest and is not in the collection.",
                        type, name, source.Uri));
                }
            }
        }

        // Only a reference that LOOKS user-made can be missing: a file resource with a negative id, or a
        // guid the collection does not hold and that is not a game preset. The analyzer cannot tell a
        // game preset guid from a dangling one, so guid references are checked against the closure of
        // the data resources only - a shape, effect or prefab id no resource here defines is reported,
        // and a game-defined shape is recognised by being addressed through the built-in catalogue.
        private static void AnalyzeClosure(LevelResources resources, List<PublishIssue> issues)
        {
            var reported = new HashSet<ResourceRef>();
            var ownerType = ResourceType.Prefab;
            var ownerName = string.Empty;
            ResourceRef Visit(ResourceRef reference)
            {
                if (IsGameDefined(reference) || ResourceClosure.Owns(resources, reference) || !reported.Add(reference))
                    return reference;

                issues.Add(new PublishIssue(PublishRule.CollectionReferenceMissing, RuleGroup.Error,
                    $"collection.{reference}",
                    $"{ownerType} '{ownerName}' points at {reference}, and the collection does not carry it.",
                    reference.Type, reference.ToString(), ownerType, ownerName));
                return reference;
            }

            foreach (var prefab in resources.Prefabs.Values)
            {
                ownerType = ResourceType.Prefab;
                ownerName = prefab?.Root?.Name ?? string.Empty;
                ResourceGraph.Walk(prefab, Visit, modifications: true);
            }

            foreach (var effect in resources.Effects.Values)
            {
                ownerType = ResourceType.Effect;
                ownerName = effect?.Name ?? string.Empty;
                ResourceGraph.Walk(effect, Visit);
            }
        }

        private static bool IsGameDefined(ResourceRef reference)
        {
            switch (reference.Type)
            {
                case ResourceType.Texture:
                case ResourceType.Font:
                case ResourceType.Audio:
                    return reference.Id >= TypedResourceId.MinGameDefinedValue;
                case ResourceType.Shape:
                    return Services.Shapes.ShapeCatalogService.TryDecode(reference.AsShape, out _);
                default:
                    // A theme or effect preset is a guid the game ships; it cannot be told apart here,
                    // and an import resolves it against the build it lands in.
                    return reference.Type == ResourceType.Theme || reference.Type == ResourceType.Effect;
            }
        }

        private static bool IsBlank(IString value)
            => value == null || value is StringValue plain && string.IsNullOrWhiteSpace(plain.Value);

        private static int Count(LevelResources resources)
            => resources.Textures.Count + resources.Fonts.Count + resources.Audios.Count
               + resources.CompositeShapes.Count + resources.Themes.Count + resources.Effects.Count
               + resources.Prefabs.Count;
    }
}

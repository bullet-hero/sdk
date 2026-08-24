using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    // TODO add IResetable (and tests)

    /// <summary>
    /// Everything about a level that is not the level itself: identity, presentation, authorship and
    /// licensing. Its own file (metadata.json) and its own serialization root - so a menu can list
    /// hundreds of levels without deserializing a single one of them.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.LevelMeta, 1, 0)]
    public class LevelMeta : IModel<LevelMeta>
    {
        /// <summary> Stable identity of the level, surviving renames and folder moves - what scores
        /// and progress attach to. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.LevelId)]
        public LevelId LevelId { get; set; }

        /// <summary> Title shown in menus, localizable. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxGameString)]
        [JsonProperty(Names.Name)]
        public IString LevelName { get; set; }

        /// <summary> Description shown in menus, localizable. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxEditorDescription)]
        [JsonProperty(Names.Description)]
        public IString LevelDescription { get; set; }

        /// <summary> Cover image, as a resource location rather than a TextureResourceId - the logo
        /// is metadata, so it must be loadable without touching the level's own resources. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Logo)]
        public ResourceKey LevelLogo { get; set; }

        /// <summary> The author's own version of this level, bumped by whoever edits it. Unrelated
        /// to the format version that [DataVersion] tracks. </summary>
        [RuleNotNull(1, 0)]
        [JsonProperty(Names.Version)]
        public Version LevelVersion { get; set; }
        
        // Level can have any license, but typical it's a 2 choice
        // CC BY-NC or CC BY-NC-SA, they both have incompatible resources (because of ShareAlike)
        // If you don't know what to choose - choose CC BY-NC
        
        /// <summary> Terms the level as a whole is published under - separate from, and constrained
        /// by, the licenses of the resources it uses. </summary>
        [RuleNotNull(typeof(TypicalLicense), TypicalLicenseType.CC_BY_NC_4_0)]
        [JsonProperty(Names.License)]
        public ILicense LevelLicense { get; set; }

        /// <summary> Who made the level itself (mapping, design) - not the authors of its assets,
        /// which are credited per resource below. </summary>
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxAuthors)]
        [JsonProperty(Names.Authors)]
        public List<Author> LevelAuthors { get; set; }

        /// <summary> One UGC record per user-defined resource: origin, license, credits. This is what
        /// makes a level shareable without guessing where its assets came from. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxResourcesMeta), RuleCollectionNoNullItems]
        [JsonProperty(Names.ResourcesMeta)]
        public List<ResourceMeta> ResourcesMeta { get; set; }

        // Rating lives on the level and only on the level - deliberately not on ResourceMeta. What a
        // player is warned about is the finished experience, and an asset has no rating of its own:
        // the same track is menu music in one level and a jump scare in another. Whoever imported it
        // would have to guess a number, and a guessed number folded into the level's own would make
        // the level's rating mean nothing.

        /// <summary> Minimum age the level is meant for - what a player is shown before playing. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.AgeRating)]
        public AgeRating LevelAgeRating { get; set; }

        /// <summary> What the level contains beyond the bare age number, including the accessibility
        /// warnings (flashing visuals, loud audio). </summary>
        [RuleEnumFlagsValid]
        [JsonProperty(Names.ContentDescriptors)]
        public ContentDescriptor LevelContentDescriptors { get; set; }

        public LevelMeta()
        {
            LevelId = LevelId.NewId();
            LevelName = new StringValue();
            LevelDescription = new StringValue();
            LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            LevelVersion = new Version(1, 0);
            LevelLicense = new TypicalLicense(TypicalLicenseType.CC_BY_NC_4_0);
            LevelAuthors = new List<Author>();
            ResourcesMeta = new List<ResourceMeta>();
            LevelAgeRating = AgeRating.Unrated;
            LevelContentDescriptors = ContentDescriptor.None;
        }
        public LevelMeta(LevelId levelId, IString levelName, IString levelDescription, ResourceKey levelLogo,
            Version levelVersion, ILicense levelLicense, List<Author> levelAuthors, List<ResourceMeta> resourcesMeta,
            AgeRating levelAgeRating = AgeRating.Unrated,
            ContentDescriptor levelContentDescriptors = ContentDescriptor.None)
        {
            LevelId = levelId;
            LevelName = levelName;
            LevelDescription = levelDescription;
            LevelLogo = levelLogo;
            LevelVersion = levelVersion;
            LevelLicense = levelLicense;
            LevelAuthors = levelAuthors;
            ResourcesMeta = resourcesMeta;
            LevelAgeRating = levelAgeRating;
            LevelContentDescriptors = levelContentDescriptors;
        }
        public void Reset()
        {
            LevelId = LevelId.NewId();
            LevelName = new StringValue();
            LevelDescription = new StringValue();
            LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            LevelVersion = new Version(1, 0);
            LevelLicense = new TypicalLicense(TypicalLicenseType.CC_BY_NC_4_0);
            LevelAuthors = new List<Author>();
            ResourcesMeta = new List<ResourceMeta>();
            LevelAgeRating = AgeRating.Unrated;
            LevelContentDescriptors = ContentDescriptor.None;
        }

        public object Clone() => Copy();
        public LevelMeta Copy() => new(LevelId, LevelName.Copy(), LevelDescription.Copy(),
            LevelLogo.Copy(), (Version)LevelVersion.Clone(), LevelLicense.Copy(),
            LevelAuthors.CopyList(), ResourcesMeta.CopyList(),
            LevelAgeRating, LevelContentDescriptors);

        public void Update(LevelMeta src)
        {
            LevelId = src.LevelId;
            LevelName = src.LevelName.Copy();
            LevelDescription = src.LevelDescription.Copy();
            LevelLogo = src.LevelLogo.Copy();
            LevelVersion = (Version)src.LevelVersion.Clone();
            LevelLicense = src.LevelLicense.Copy();
            LevelAuthors = src.LevelAuthors.CopyList();
            ResourcesMeta = src.ResourcesMeta.CopyList();
            LevelAgeRating = src.LevelAgeRating;
            LevelContentDescriptors = src.LevelContentDescriptors;
        }

        public void Pull(LevelMeta src)
        {
            LevelId = src.LevelId;
            LevelName = LevelName.PullFrom(src.LevelName);
            LevelDescription = LevelDescription.PullFrom(src.LevelDescription);
            LevelLogo.Pull(src.LevelLogo);
            LevelVersion = (Version)src.LevelVersion.Clone();
            LevelLicense = LevelLicense.PullFrom(src.LevelLicense);
            LevelAuthors = src.LevelAuthors.CopyList();
            ResourcesMeta = src.ResourcesMeta.CopyList();
            LevelAgeRating = src.LevelAgeRating;
            LevelContentDescriptors = src.LevelContentDescriptors;
        }

        public override bool Equals(object obj) => obj is LevelMeta value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(LevelId);
            hashCode.Add(LevelName);
            hashCode.Add(LevelDescription);
            hashCode.Add(LevelLogo);
            hashCode.Add(LevelVersion);
            hashCode.Add(LevelLicense);
            hashCode.Add(LevelAuthors.GetListHashCode());
            hashCode.Add(ResourcesMeta.GetListHashCode());
            hashCode.Add((int)LevelAgeRating);
            hashCode.Add((int)LevelContentDescriptors);
            return hashCode.ToHashCode();
        }

        public bool Equals(LevelMeta other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = LevelId.Equals(other.LevelId)
                         && LevelName.Equals(other.LevelName)
                         && LevelDescription.Equals(other.LevelDescription)
                         && LevelLogo.Equals(other.LevelLogo)
                         && LevelVersion.Equals(other.LevelVersion)
                         && LevelLicense.Equals(other.LevelLicense)
                         && LevelAuthors.ListEquals(other.LevelAuthors)
                         && ResourcesMeta.ListEquals(other.ResourcesMeta)
                         && LevelAgeRating == other.LevelAgeRating
                         && LevelContentDescriptors == other.LevelContentDescriptors;
            return result;
        }
    }
}
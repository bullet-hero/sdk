using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
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
    /// <summary>
    /// Everything about a level that is not the level itself: identity, presentation, authorship and
    /// licensing. Its own file, so a menu can list hundreds of levels without deserializing one.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.LevelMeta, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class LevelMeta : IModel<LevelMeta>
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
        /// to the format generation that [ModelGeneration] tracks. </summary>
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

        // Rating lives on the level only, never on ResourceMeta, and is ONE number (no descriptor flags).
        // Docs/RESOURCE_META.md

        /// <summary> Minimum age the level is meant for - what a player is shown before playing. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.AgeRating)]
        public AgeRating LevelAgeRating { get; set; }

        // Plain strings, not IString: a tag is a token a player MATCHES, and a translated one stops
        // matching across languages. Free text, not a closed set. Docs/RESOURCE_META.md

        /// <summary> Author-chosen filter tokens, shown as written and never translated. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Tags)]
        public List<string> LevelTags { get; set; }

        // Denormalised from Level.Settings (the browser never opens level.json). ZERO MEANS UNKNOWN, never
        // "instant": do not compare it. Docs/RESOURCE_META.md

        /// <summary> The level's length in seconds, or 0 when unknown. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.Duration)]
        public float LevelDuration { get; set; }

        // The gate that costs no read: a level claiming more than this build has is REFUSED. Computed at
        // save from the registry (LevelGenerations.Required()), never typed; Invalid means "no claim".
        // Docs/RESOURCE_META.md

        /// <summary> The highest model generation any domain in this level uses - what a client must
        /// support to read it whole - or <see cref="ModelGenerations.Invalid"/> when the file makes no claim. </summary>
        [RuleMinValue(ModelGenerations.Invalid)]
        [JsonProperty(Names.MinGeneration)]
        public int MinGeneration { get; set; }

        // The level's OWN content only; each resource declares itself. ContainsAiContent is derived.

        /// <summary> Whether the level's own content was generated by AI, as its author declares.
        /// NotSpecified until they say. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.AiGenerated)]
        public AiGeneration LevelAiGenerated { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
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
            LevelTags = new List<string>();
            LevelDuration = 0f;
            MinGeneration = ModelGenerations.Invalid;
            LevelAiGenerated = AiGeneration.NotSpecified;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public LevelMeta(LevelId levelId, IString levelName, IString levelDescription, ResourceKey levelLogo,
            Version levelVersion, ILicense levelLicense, List<Author> levelAuthors, List<ResourceMeta> resourcesMeta,
            AgeRating levelAgeRating = AgeRating.Unrated)
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
            LevelTags = new List<string>();
            LevelDuration = 0f;
            MinGeneration = ModelGenerations.Invalid;
            LevelAiGenerated = AiGeneration.NotSpecified;
        }

        /// <summary> True when the level itself or any resource record, the cover's included, declares
        /// AI generation. Only <see cref="AiGeneration.Yes"/> counts - an unspecified declaration is
        /// unknown, not a claim. </summary>
        public bool ContainsAiContent()
        {
            if (LevelAiGenerated == AiGeneration.Yes) return true;
            if (ResourcesMeta == null) return false;

            foreach (var record in ResourcesMeta)
            {
                if (record != null && record.ResourceAiGenerated == AiGeneration.Yes) return true;
            }

            return false;
        }
    }
}
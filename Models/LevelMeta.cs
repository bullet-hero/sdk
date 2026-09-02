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
    // TODO add IResetable (and tests)

    /// <summary>
    /// Everything about a level that is not the level itself: identity, presentation, authorship and
    /// licensing. Its own file (metadata.json) and its own serialization root - so a menu can list
    /// hundreds of levels without deserializing a single one of them.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.LevelMeta, 1, 0)]
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

        // PLAIN STRINGS, NOT IString, and that is the one decision here worth arguing. Every
        // other piece of text on this model is localized because a player READS it; a tag is a
        // filter token that a player MATCHES, and a translated one stops matching the moment two
        // players run different languages. So a tag is the author's own word, shown as written.
        //
        // Free text rather than a closed set for the same reason ILevelSource.Id is a string:
        // whoever authors a level knows what it is, and a shipped vocabulary would be wrong for
        // every genre nobody thought of.

        /// <summary> Author-chosen filter tokens, shown as written and never translated. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Tags)]
        public List<string> LevelTags { get; set; }

        // DENORMALISED FROM Level.Settings, and it has to be. The browser reads metadata for
        // every level in a folder and never opens level.json - reading a length from there would
        // be hundreds of files parsed to draw one filter chip, which is the same cost this model
        // already refuses for the age rating. EditorService rewrites it on every save, so it is
        // correct for anything this game has written.
        //
        // ZERO MEANS UNKNOWN, not "instant": a level authored before this field existed, or
        // written by another tool, has no length here. Every consumer must treat 0 as "do not
        // know" and never as a value to compare - a duration filter that hid such levels would
        // hide every level that predates the field.

        /// <summary> The level's length in seconds, or 0 when unknown. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.Duration)]
        public float LevelDuration { get; set; }

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
            LevelTags = new List<string>();
            LevelDuration = 0f;
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
            LevelTags = new List<string>();
            LevelDuration = 0f;
        }
    }
}
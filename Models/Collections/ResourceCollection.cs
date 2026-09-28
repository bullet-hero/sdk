using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Collections
{
    // WHAT A COLLECTION IS, BESIDES ITS FILES. The data resources - prefabs, themes, shapes, effects -
    // need no table here: each is an envelope file of its own under the kind's folder, exactly like the
    // device library, so the folder IS the table. The file resources cannot do that - a texture is
    // bytes, and the Resource record saying where those bytes are and how to read them has to live
    // somewhere - so they are listed here, with a LevelPath uri read relative to the collection root.
    //
    // THEIR IDS ARE COLLECTION-LOCAL, like a level's: negative ints minted by whoever built the
    // collection, and renumbered by every import (ResourceRemap). A prefab inside the collection refers
    // to its textures by those same local ids, which is what keeps the collection self-contained.
    //
    // A NEW DOMAIN, NOT A BUMP. Nothing on any disk has this shape before generation 2, and no existing
    // model refers to it - a level copies what it imports and never points at a collection.

    /// <summary> A collection's manifest: who made it, under what license, and the file resources it
    /// carries. The data resources are the envelope files beside it. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.Collection, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class ResourceCollection : IModel<ResourceCollection>
    {
        /// <summary> Stable identity: the folder name, and the Workshop item's `bhid` tag. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.CollectionId)]
        public CollectionId CollectionId { get; set; }

        /// <summary> Title shown in the library, localizable. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxGameString)]
        [JsonProperty(Names.Name)]
        public IString Name { get; set; }

        /// <summary> What is in it and what it is for, localizable. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxEditorDescription)]
        [JsonProperty(Names.Description)]
        public IString Description { get; set; }

        /// <summary> Who made the collection as a whole. Per-resource credit goes in ResourcesMeta. </summary>
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxAuthors)]
        [JsonProperty(Names.Authors)]
        public List<Author> Authors { get; set; }

        /// <summary> Terms the collection is distributed under. Unspecified until the author states it. </summary>
        [RuleNotNull(typeof(NoSpecifiedLicense))]
        [JsonProperty(Names.License)]
        public ILicense License { get; set; }

        /// <summary> Images it carries, files under media/. </summary>
        [GenerateModelKeyed(nameof(TextureResource.TextureResourceId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxTextures)]
        [RuleDictionaryKeyMatches(nameof(TextureResource.TextureResourceId))]
        [JsonProperty(Names.Textures)]
        public Dictionary<TextureResourceId, TextureResource> Textures { get; set; }

        /// <summary> Typefaces it carries, files under media/. </summary>
        [GenerateModelKeyed(nameof(FontResource.FontResourceId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxFonts)]
        [RuleDictionaryKeyMatches(nameof(FontResource.FontResourceId))]
        [JsonProperty(Names.Fonts)]
        public Dictionary<FontResourceId, FontResource> Fonts { get; set; }

        /// <summary> Clips it carries, files under media/. </summary>
        [GenerateModelKeyed(nameof(AudioResource.AudioResourceId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxAudios)]
        [RuleDictionaryKeyMatches(nameof(AudioResource.AudioResourceId))]
        [JsonProperty(Names.Audios)]
        public Dictionary<AudioResourceId, AudioResource> Audios { get; set; }

        /// <summary> Per-resource origin, license and credit, for file and data resources alike -
        /// what an import copies into the level's own metadata. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxResourcesMeta), RuleCollectionNoNullItems]
        [JsonProperty(Names.ResourcesMeta)]
        public List<ResourceMeta> ResourcesMeta { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ResourceCollection()
        {
            CollectionId = CollectionId.NewId();
            Name = new StringValue();
            Description = new StringValue();
            Authors = new List<Author>();
            License = new NoSpecifiedLicense();
            Textures = new Dictionary<TextureResourceId, TextureResource>();
            Fonts = new Dictionary<FontResourceId, FontResource>();
            Audios = new Dictionary<AudioResourceId, AudioResource>();
            ResourcesMeta = new List<ResourceMeta>();
        }
    }
}

using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Data;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// Everything a level owns beyond its objects: seven dictionaries of user-defined resources.
    /// Only user-defined (negative-id) entries ever appear here - anything shipped with the game
    /// lives in the game's own registries and is referenced by id without being copied in.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.LevelResources, 1, 0)]
    [GenerateModel]
    public sealed partial class LevelResources : IModel<LevelResources>
    {
        // Every dictionary here is capped and key-checked. The caps bound LOAD time rather than frame
        // time: each entry is a user-defined resource the loader resolves before playback starts.
        // The key checks close a gap serialization hides - DictionaryAsListConverter rebuilds keys
        // from values on read, so only in-memory code can make the two disagree, and when it does,
        // lookup by id finds nothing while iteration finds the resource.

        /// <summary> Images the level ships with. </summary>
        [GenerateModelKeyed(nameof(TextureResource.TextureResourceId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxTextures)]
        [RuleDictionaryKeyMatches(nameof(TextureResource.TextureResourceId))]
        [JsonProperty(Names.Textures)]
        public Dictionary<TextureResourceId, TextureResource> Textures { get; set; }

        /// <summary> Typefaces the level ships with. </summary>
        [GenerateModelKeyed(nameof(FontResource.FontResourceId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxFonts)]
        [RuleDictionaryKeyMatches(nameof(FontResource.FontResourceId))]
        [JsonProperty(Names.Fonts)]
        public Dictionary<FontResourceId, FontResource> Fonts { get; set; }

        /// <summary> Clips the level ships with, including the song itself. </summary>
        [GenerateModelKeyed(nameof(AudioResource.AudioResourceId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxAudios)]
        [RuleDictionaryKeyMatches(nameof(AudioResource.AudioResourceId))]
        [JsonProperty(Names.Audios)]
        public Dictionary<AudioResourceId, AudioResource> Audios { get; set; }

        /// <summary> Custom shapes, beyond the built-in library. Usable both as what an object draws
        /// and as what it collides with. </summary>
        [GenerateModelKeyed(nameof(CompositeShape.ShapeId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxCompositeShapes)]
        [RuleDictionaryKeyMatches(nameof(CompositeShape.ShapeId))]
        [JsonProperty(Names.Shapes)]
        public Dictionary<ShapeId, CompositeShape> CompositeShapes { get; set; }

        /// <summary> Color palettes the level switches between over time. </summary>
        [GenerateModelKeyed(nameof(ThemeData.ThemeId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxThemes)]
        [RuleDictionaryKeyMatches(nameof(ThemeData.ThemeId))]
        [JsonProperty(Names.Themes)]
        public Dictionary<ThemeId, ThemeData> Themes { get; set; }

        /// <summary> Particle-system definitions, shared by every EffectObject that points at one. </summary>
        [GenerateModelKeyed(nameof(EffectData.EffectId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxEffects)]
        [RuleDictionaryKeyMatches(nameof(EffectData.EffectId))]
        [JsonProperty(Names.Effects)]
        public Dictionary<EffectId, EffectData> Effects { get; set; }

        /// <summary> Reusable object templates. Unlike the other six, these hold level content rather
        /// than external assets - a prefab is authored here, not fetched. </summary>
        [GenerateModelKeyed(nameof(Prefab.PrefabId))]
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPrefabs)]
        [RuleDictionaryKeyMatches(nameof(Prefab.PrefabId))]
        [JsonProperty(Names.Prefabs)]
        public Dictionary<PrefabId, Prefab> Prefabs { get; set; }

        public LevelResources()
        {
            Textures = new Dictionary<TextureResourceId, TextureResource>();
            Fonts = new Dictionary<FontResourceId, FontResource>();
            Audios = new Dictionary<AudioResourceId, AudioResource>();

            CompositeShapes = new Dictionary<ShapeId, CompositeShape>();
            Themes = new Dictionary<ThemeId, ThemeData>();
            Effects = new Dictionary<EffectId, EffectData>();
            
            Prefabs = new Dictionary<PrefabId, Prefab>();
        }
        public LevelResources(Dictionary<TextureResourceId, TextureResource> textures,
            Dictionary<FontResourceId, FontResource> fonts,
            Dictionary<AudioResourceId, AudioResource> audios,
            Dictionary<ShapeId, CompositeShape> compositeShapes,
            Dictionary<ThemeId, ThemeData> themes,
            Dictionary<EffectId, EffectData> effects,
            Dictionary<PrefabId, Prefab> prefabs)
        {
            Textures = textures;
            Fonts = fonts;
            Audios = audios;
            CompositeShapes = compositeShapes;
            Themes = themes;
            Effects = effects;
            Prefabs = prefabs;
        }
    }
}
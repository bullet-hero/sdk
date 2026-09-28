using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Only ResourcesMeta changed at generation 2 - each record gained ResourceGuid - so only it is
    // retyped to a snapshot (ResourceMetaV1). Every other member keeps TODAY's class. Keys are literal:
    // a snapshot spells them the way its own generation did.

    /// <summary> Generation 1 (Release) of the level-metadata domain - the shape 1.0.0 shipped, before a
    /// resource record could credit a theme, effect, shape or prefab. A frozen snapshot - never edit it
    /// to match today's shape. </summary>
    [ModelGeneration(ModelDomains.LevelMeta, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class LevelMetaV1 : IModel<LevelMetaV1>
    {
        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("level_id")]
        public LevelId LevelId { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("name")]
        public IString LevelName { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("desc")]
        public IString LevelDescription { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("logo")]
        public ResourceKey LevelLogo { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("vrs")]
        public Version LevelVersion { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("license")]
        public ILicense LevelLicense { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("authors")]
        public List<Author> LevelAuthors { get; set; }

        /// <summary> Changed at this bump: records without a guid slot. </summary>
        [JsonProperty("resources_meta")]
        public List<ResourceMetaV1> ResourcesMeta { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("age_rating")]
        public AgeRating LevelAgeRating { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("tags")]
        public List<string> LevelTags { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("duration")]
        public float LevelDuration { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("min_generation")]
        public int MinGeneration { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public LevelMetaV1()
        {
            LevelId = LevelId.Null;
            LevelName = new StringValue();
            LevelDescription = new StringValue();
            LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            LevelVersion = new Version(1, 0);
            LevelLicense = new TypicalLicense(TypicalLicenseType.CC_BY_NC_4_0);
            LevelAuthors = new List<Author>();
            ResourcesMeta = new List<ResourceMetaV1>();
            LevelAgeRating = AgeRating.Unrated;
            LevelTags = new List<string>();
            LevelDuration = 0f;
            MinGeneration = ModelGenerations.Invalid;
        }
    }
}

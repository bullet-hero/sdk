using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Overlays photographic grain. Also useful practically - grain hides color banding on large
    /// flat backgrounds.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class FilmGrainKey : PostProcessingKeyframe, IModel<FilmGrainKey>
    {
        /// <summary> Which grain texture to use - picks the character (fine/medium/coarse), not the
        /// amount. </summary>
        [RuleEnumValid(FilmGrainType.Medium1)]
        [JsonProperty(Names.Type)]
        public FilmGrainType Type { get; set; }

        /// <summary> How visible the grain is. </summary>
        [RuleInRange(PostProcessingRules.FilmGrain.IntensityMin,
            PostProcessingRules.FilmGrain.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        public FilmGrainKey()
        {
            Type = FilmGrainType.Medium1;
            Intensity = 1.0f;
        }
        public FilmGrainKey(FilmGrainType type, float intensity,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Type = type;
            Intensity = intensity;
        }
    }
}
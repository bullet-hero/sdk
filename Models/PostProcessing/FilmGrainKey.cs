using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Overlays photographic grain. Also useful practically - grain hides color banding on large
    /// flat backgrounds.
    /// </summary>
    [RuleContainer]
    public class FilmGrainKey : PostProcessingKeyframe, IModel<FilmGrainKey>
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
        public override void Reset()
        {
            base.Reset();
            Type = FilmGrainType.Medium1;
            Intensity = 1.0f;
        }
        
        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        FilmGrainKey ICopyable<FilmGrainKey>.Copy() => CopyImpl();
        
        private FilmGrainKey CopyImpl() => new(Type, Intensity, Active, Frame, Ease);

        public void Update(FilmGrainKey src)
        {
            base.Update(src);

            Type = src.Type;
            Intensity = src.Intensity;
        }

        public void Pull(FilmGrainKey src)
        {
            base.Pull(src);

            Type = src.Type;
            Intensity = src.Intensity;
        }

        public override bool Equals(object obj) => obj is FilmGrainKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), (int)Type, Intensity);

        public bool Equals(FilmGrainKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Type == other.Type
                         && Intensity.Equals(other.Intensity);
            return result;
        }
    }
}
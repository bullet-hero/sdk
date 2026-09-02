using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Camera shake key - a procedural offset layered on top of the camera's own position track, so
    /// shaking never destroys the authored movement underneath it. Camera-only, no object equivalent.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ShakeKey : Keyframe, IModel<ShakeKey>
    {
        /// <summary> Overall strength; zero disables the shake without removing the key. </summary>
        [RuleInRange(ValueRules.MinShake, ValueRules.MaxShake)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        /// <summary> How fast the offset changes - the difference between a rumble and a jitter. </summary>
        [RuleInRange(ValueRules.MinShake, ValueRules.MaxShake)]
        [JsonProperty(Names.Speed)]
        public float Speed { get; set; }

        /// <summary> Horizontal weight, multiplied by Intensity - lets a shake be purely sideways. </summary>
        [RuleInRange(ValueRules.MinShake, ValueRules.MaxShake)]
        [JsonProperty(Names.CoordX)]
        public float IntensityX { get; set; }

        /// <summary> Vertical weight, multiplied by Intensity. </summary>
        [RuleInRange(ValueRules.MinShake, ValueRules.MaxShake)]
        [JsonProperty(Names.CoordY)]
        public float IntensityY { get; set; }

        public ShakeKey()
        {
            Intensity = 1f;
            Speed = 1f;
            IntensityX = 1f;
            IntensityY = 1f;
        }
        public ShakeKey(float intensity, float speed,
            int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Intensity = intensity;
            Speed = speed;
            IntensityX = 1f;
            IntensityY = 1f;
        }
        public ShakeKey(float intensity, float speed, float intensityX, float intensityY,
            int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Intensity = intensity;
            Speed = speed;
            IntensityX = intensityX;
            IntensityY = intensityY;
        }
    }
}
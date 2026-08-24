using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Camera shake key - a procedural offset layered on top of the camera's own position track, so
    /// shaking never destroys the authored movement underneath it. Camera-only, no object equivalent.
    /// </summary>
    [RuleContainer]
    public class ShakeKey : Keyframe, IModel<ShakeKey>
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
        public override void Reset()
        {
            base.Reset();
            Intensity = 1f;
            Speed = 1f;
            IntensityX = 1f;
            IntensityY = 1f;
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        ShakeKey ICopyable<ShakeKey>.Copy() => CopyImpl();
        
        private ShakeKey CopyImpl() => new(Intensity, Speed, IntensityX, IntensityY, Frame, Ease);

        public void Update(ShakeKey src)
        {
            base.Update(src);

            Intensity = src.Intensity;
            Speed = src.Speed;
            IntensityX = src.IntensityX;
            IntensityY = src.IntensityY;
        }

        public void Pull(ShakeKey src)
        {
            base.Pull(src);

            Intensity = src.Intensity;
            Speed = src.Speed;
            IntensityX = src.IntensityX;
            IntensityY = src.IntensityY;
        }

        public override bool Equals(object obj) => obj is ShakeKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(),
            Intensity, Speed, IntensityX, IntensityY);

        public bool Equals(ShakeKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Intensity.Equals(other.Intensity)
                         && Speed.Equals(other.Speed)
                         && IntensityX.Equals(other.IntensityX)
                         && IntensityY.Equals(other.IntensityY);
            return result;
        }
    }
}
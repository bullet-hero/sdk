namespace BH.SDK.Models.Enums.Effects
{
    /// <summary> Which form an effect's colour is authored in. </summary>
    public enum EffectColorType : byte
    {
        /// <summary> One colour, constant for every particle. </summary>
        Value = 0,

        /// <summary> Sampled from a gradient along the particle's own lifetime. </summary>
        GradientOverLife = 1,

        /// <summary> Sampled from a gradient keyed on how fast the particle is moving. </summary>
        GradientBySpeed = 2,

        /// <summary> One number drawn per particle and used on every channel. </summary>
        RandomUniform = 3,

        /// <summary> A number drawn per particle per channel. </summary>
        RandomPerComponent = 4,

        /// <summary> A point drawn per particle from one gradient, then held for its whole life. </summary>
        GradientRandom = 5,
    }
}
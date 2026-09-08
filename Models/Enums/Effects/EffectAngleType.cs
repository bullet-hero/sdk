namespace BH.SDK.Models.Enums.Effects
{
    /// <summary> Which form an effect's angle is authored in. </summary>
    public enum EffectAngleType : byte
    {
        /// <summary> One angle, constant for every particle. </summary>
        Value = 0,

        /// <summary> Driven by a curve along the particle's own lifetime. </summary>
        CurvesOverLife = 1,

        /// <summary> Driven by a curve keyed on how fast the particle is moving. </summary>
        CurvesBySpeed = 2,

        /// <summary> One number drawn per particle and used on every axis. </summary>
        RandomUniform = 3,

        /// <summary> A number drawn per particle per axis. </summary>
        RandomPerComponent = 4,
    }
}
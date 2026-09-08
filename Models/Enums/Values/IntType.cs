namespace BH.SDK.Models.Enums.Values
{
    /// <summary> Which form an authored whole number is in. </summary>
    public enum IntType : byte
    {
        /// <summary> The number itself. </summary>
        Value = 0,

        /// <summary> Drawn between two bounds, addressed rather than generated, so a run reproduces. </summary>
        RandomMinMax = 1,

        /// <summary> The same, quantized to a step. </summary>
        RandomMinMaxStep = 2,
    }
}
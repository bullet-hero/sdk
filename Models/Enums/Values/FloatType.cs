namespace BH.SDK.Models.Enums.Values
{
    /// <summary> Which form an authored float is in. </summary>
    public enum FloatType : byte
    {
        /// <summary> The number itself. </summary>
        Value = 0,

        /// <summary> Drawn between two bounds, addressed rather than generated, so a run reproduces. </summary>
        RandomMinMax = 1,

        /// <summary> The same, quantized to a step - so a random angle can still land on a grid. </summary>
        RandomMinMaxStep = 2,
    }
}
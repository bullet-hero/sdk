namespace BH.SDK.Models.Enums.Effects
{
    /// <summary> How successive particles are placed across the shape an effect emits from. </summary>
    public enum EffectShapeSpreadType : byte
    {
        /// <summary> Each particle lands anywhere on the shape. </summary>
        Random = 0,

        /// <summary> Walks the shape end to end, then jumps back to the start. </summary>
        Loop = 1,

        /// <summary> Walks the shape end to end and back again. </summary>
        PingPong = 2,

        /// <summary> Sweeps back and forth, lingering at both ends. </summary>
        Sine = 3,
    }
}
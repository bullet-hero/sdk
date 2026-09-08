namespace BH.SDK.Models.Interfaces.Primitives
{
    /// <summary> A wrapper around one float - an id or a measure that must not be confused with a bare number. </summary>
    public interface IPrimitiveFloat : IResetable
    {
        /// <summary> The number it wraps. </summary>
        public float Value { get; }
    }
}
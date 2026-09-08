namespace BH.SDK.Models.Interfaces.Primitives
{
    /// <summary> A wrapper around one int - an id or a count that must not be confused with a bare number. </summary>
    public interface IPrimitiveInt : IResetable
    {
        /// <summary> The number it wraps. </summary>
        public int Value { get; }
    }
}

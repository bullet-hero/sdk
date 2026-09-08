using System;

namespace BH.SDK.Models.Interfaces.Primitives
{
    /// <summary> A wrapper around one guid - a globally unique id that must not be confused with another kind's. </summary>
    public interface IPrimitiveGuid : IResetable
    {
        /// <summary> The guid it wraps. </summary>
        public Guid Value { get; }
    }
}

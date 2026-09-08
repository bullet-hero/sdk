using System;

namespace BH.SDK.Models.Interfaces
{
    /// <summary> Deep-copyable, and typed - unlike <see cref="ICloneable"/>, whose result needs a cast. </summary>
    public interface ICopyable<out T> : ICloneable
    {
        /// <summary> A new instance sharing nothing mutable with this one. </summary>
        public T Copy();
    }
}
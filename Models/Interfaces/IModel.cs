using System;

namespace BH.SDK.Models.Interfaces
{
    /// <summary>
    /// Every live domain model: copy, compare, reset, and become another instance in place - by
    /// replacement (Update) or without invalidating anything pointing inside it (Pull).
    /// </summary>
    public interface IModel<T> : ICopyable<T>, IEquatable<T>, IResetable, IUpdatable<T>, IMoveable<T>
    {
        
    }
}
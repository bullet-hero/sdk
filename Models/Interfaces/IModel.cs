using System;

namespace BH.SDK.Models.Interfaces
{
    /// <summary>
    /// Every live domain model: copy it, compare it, reset it to defaults, and make it become
    /// another instance in place - by replacement (Update) or without invalidating anything that
    /// points inside it (Pull).
    /// </summary>
    public interface IModel<T> : ICopyable<T>, IEquatable<T>, IResetable, IUpdatable<T>, IMoveable<T>
    {
        
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BH.SDK.Roslyn.Model
{
    // An incremental generator caches by VALUE, and ImmutableArray<T> compares by reference - so a
    // model spec carrying one would miss the cache on every keystroke and re-emit 205 files. This
    // is the standard fix and exists for that one reason.

    /// <summary> An immutable array that equals another one holding equal items. </summary>
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {
        private readonly ImmutableArray<T> _items;

        /// <summary> Wraps an array; a default instance is legal and reads as empty. </summary>
        public EquatableArray(ImmutableArray<T> items) => _items = items;

        /// <summary> How many items, a default instance counting as none. </summary>
        public int Length => _items.IsDefault ? 0 : _items.Length;
        /// <summary> One item. </summary>
        public T this[int index] => _items[index];

        /// <summary> True when it holds nothing. </summary>
        public bool IsEmpty => Length == 0;

        /// <summary> Item by item - which is the whole point of the wrapper. </summary>
        public bool Equals(EquatableArray<T> other)
        {
            if (Length != other.Length) return false;
            for (var i = 0; i < Length; i++)
                if (!_items[i].Equals(other._items[i]))
                    return false;
            return true;
        }

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is EquatableArray<T> other && Equals(other);

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
        public override int GetHashCode()
        {
            var hash = 17;
            for (var i = 0; i < Length; i++)
                hash = unchecked(hash * 31 + (_items[i]?.GetHashCode() ?? 0));
            return hash;
        }

        /// <summary> Walks the items, answering nothing for a default instance. </summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Length; i++) yield return _items[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary> Construction helpers for <see cref="EquatableArray{T}"/>. </summary>
    internal static class EquatableArray
    {
        /// <summary> Wraps a sequence, so an incremental pipeline can compare it by VALUE - which an
        /// ImmutableArray compares by reference, re-emitting every model on every keystroke. </summary>
        public static EquatableArray<T> From<T>(IEnumerable<T> items) where T : IEquatable<T>
            => new EquatableArray<T>(ImmutableArray.CreateRange(items));
    }
}

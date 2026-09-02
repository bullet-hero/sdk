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

        public EquatableArray(ImmutableArray<T> items) => _items = items;

        public int Length => _items.IsDefault ? 0 : _items.Length;
        public T this[int index] => _items[index];
        public bool IsEmpty => Length == 0;

        public bool Equals(EquatableArray<T> other)
        {
            if (Length != other.Length) return false;
            for (var i = 0; i < Length; i++)
                if (!_items[i].Equals(other._items[i]))
                    return false;
            return true;
        }

        public override bool Equals(object obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            var hash = 17;
            for (var i = 0; i < Length; i++)
                hash = unchecked(hash * 31 + (_items[i]?.GetHashCode() ?? 0));
            return hash;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Length; i++) yield return _items[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static class EquatableArray
    {
        public static EquatableArray<T> From<T>(IEnumerable<T> items) where T : IEquatable<T>
            => new EquatableArray<T>(ImmutableArray.CreateRange(items));
    }
}

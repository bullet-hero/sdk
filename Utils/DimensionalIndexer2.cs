using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BH.SDK.Utils
{
    /// <summary>
    /// Proxy is a struct designed to work with regular arrays as in 2-dimensional arrays.
    /// Special implementation for only 2 dimensions for optimization purposes
    /// </summary>
    public readonly struct DimensionalIndexer2 : IEnumerable<int>, IEnumerable<DimensionalIndexer2.IndexTuple>
    {
        /// <summary> How many cells across. </summary>
        public int LengthWidth { get; }

        /// <summary> How many cells down. </summary>
        public int LengthHeight { get; }

        /// <summary> How many cells in total. </summary>
        public int Length { get; }

        /// <summary> Built from its width and height. </summary>
        public DimensionalIndexer2(int lengthWidth, int lengthHeight)
        {
            LengthWidth = lengthWidth; // aka width
            LengthHeight = lengthHeight; // aka height
            Length = lengthWidth * lengthHeight;
        }
        
        /// <summary> A pair of coordinates as one flat index. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int indexWidth, int indexHeight)
        {
            return indexHeight * LengthWidth + indexWidth;
        }
        /// <summary> A coordinate pair as one flat index. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(IndexTuple indexTuple)
        {
            return indexTuple.IndexHeight * LengthWidth + indexTuple.IndexWidth;
        }
        /// <summary> The same without an indexer instance. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(int indexWidth, int indexHeight, int lengthWidth)
        {
            return indexHeight * lengthWidth + indexWidth;
        }
        /// <summary> The same from a pair. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(IndexTuple indexTuple, int lengthWidth)
        {
            return indexTuple.IndexHeight * lengthWidth + indexTuple.IndexWidth;
        }
        
        /// <summary> A flat index back as a pair of coordinates. </summary>
        public (int, int) GetIndexes(int index)
        {
            var indexHeight = index / LengthWidth;
            var indexWidth = index - indexHeight * LengthWidth;
            return (indexWidth, indexHeight);
        }
        /// <summary> A flat index back as coordinates, without allocating a tuple. </summary>
        public void GetIndexes(int index, out int indexWidth, out int indexHeight)
        {
            indexHeight = index / LengthWidth;
            indexWidth = index - indexHeight * LengthWidth;
        }
        
        /// <summary> The same without an indexer instance. </summary>
        public static (int, int) GetIndexes(int index, int lengthWidth)
        {
            var indexHeight = index / lengthWidth;
            var indexWidth = index - indexHeight * lengthWidth;
            return (indexWidth, indexHeight);
        }
        /// <summary> The same, without allocating a tuple. </summary>
        public static void GetIndexes(int index, int lengthWidth, out int indexWidth, out int indexHeight)
        {
            indexHeight = index / lengthWidth;
            indexWidth = index - indexHeight * lengthWidth;
        }
        
        // IEnumerators
        
        /// <summary> Every flat index in order. </summary>
        public IEnumerator<int> Enumerate()
        {
            for (var index = 0; index < Length; index++)
                yield return index;
        }
        /// <summary> The same without an indexer instance. </summary>
        public static IEnumerator<int> Enumerate(int lengthWidth, int lengthHeight)
        {
            var length = lengthWidth * lengthHeight;
            for (var index = 0; index < length; index++)
                yield return index;
        }
        
        /// <summary> Every cell as a coordinate pair. </summary>
        public IEnumerable<(int, int)> Enumerate2()
        {
            for (var indexHeight = 0; indexHeight < LengthHeight; indexHeight++)
            for (var indexWidth = 0; indexWidth < LengthWidth; indexWidth++)
                yield return (indexWidth, indexHeight);
        }
        /// <summary> The same without an indexer instance. </summary>
        public static IEnumerable<(int, int)> Enumerate2(int lengthWidth, int lengthHeight)
        {
            for (var indexHeight = 0; indexHeight < lengthHeight; indexHeight++)
            for (var indexWidth = 0; indexWidth < lengthWidth; indexWidth++)
                yield return (indexWidth, indexHeight);
        }
        
        /// <summary> A collection paired with each item's flat index. </summary>
        public static IEnumerable<(T, int)> Enumerate<T>(IEnumerable<T> collection)
        {
            using var enumerator = collection.GetEnumerator();
            var counter = 0;
            
            while (enumerator.MoveNext())
                yield return (enumerator.Current, counter++);
        }
        
        /// <summary> A collection paired with each item's coordinates. </summary>
        public IEnumerable<(T, int, int)> Enumerate2<T>(IEnumerable<T> collection)
        {
            using var enumerator = collection.GetEnumerator();
            int counter1 = 0, counter2 = 0;

            while (enumerator.MoveNext())
            {
                yield return (enumerator.Current, counter1, counter2);
                
                counter1++;
                if (counter1 == LengthWidth)
                {
                    counter2++;
                    counter1 = 0;
                }
            }
        }
        /// <summary> The same against a width the caller supplies. </summary>
        public IEnumerable<(T, int, int)> Enumerate2<T>(IEnumerable<T> collection, int lengthWidth)
        {
            using var enumerator = collection.GetEnumerator();
            int counter1 = 0, counter2 = 0;

            while (enumerator.MoveNext())
            {
                yield return (enumerator.Current, counter1, counter2);
                
                counter1++;
                if (counter1 == lengthWidth)
                {
                    counter2++;
                    counter1 = 0;
                }
            }
        }
        
        /// <summary> Walks the flat indices in a foreach. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(LengthWidth);
        
        /// <summary> Walks the coordinate pairs. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator2 GetEnumerator2() => new(LengthWidth, LengthHeight);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<IndexTuple> IEnumerable<IndexTuple>.GetEnumerator() => new Enumerator2(LengthWidth, LengthHeight);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => new Enumerator(Length);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(Length);
        
        /// <summary> Walks the flat index range. </summary>
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int _length;

            /// <summary> The index the walk is on. </summary>
            public int Current { get; private set; }

            /// <summary> Built from its length. </summary>
            public Enumerator(int length)
            {
                _length = length;
                Current = -1;
            }

            /// <summary> Advances to the next step, answering false at the end. </summary>
            public bool MoveNext()
            {
                return ++Current < _length;
            }

            /// <summary> Back to the values the constructor writes. </summary>
            public void Reset() => Current = -1;
            object IEnumerator.Current => Current;
            /// <summary> Nothing to release; the walk allocates nothing. </summary>
            public void Dispose() { }
        }

        /// <summary> One cell as its two coordinates. </summary>
        public struct IndexTuple
        {
            /// <summary> Column. </summary>
            public int IndexWidth;
            /// <summary> Row. </summary>
            public int IndexHeight;
            
            /// <summary> Built from its width and height. </summary>
            public IndexTuple(int indexWidth, int indexHeight)
            {
                IndexWidth = indexWidth;
                IndexHeight = indexHeight;
            }
        }
        
        /// <summary> Walks the same range as coordinate pairs instead of flat indices. </summary>
        public struct Enumerator2 : IEnumerator<IndexTuple>
        {
            private readonly int _length1;
            private readonly int _length;
            
            private int _index;

            /// <summary> The cell the walk is on. </summary>
            public IndexTuple Current { get; private set; }

            /// <summary> Built from its 1 and 2. </summary>
            public Enumerator2(int length1, int length2)
            {
                _length1 = length1;
                _length = length1 * length2;
                _index = -1;
                Current = default;
            }

            /// <summary> Advances to the next step, answering false at the end. </summary>
            public bool MoveNext()
            {
                if (++_index >= _length)
                    return false;

                var indexHeight = _index / _length1;
                var indexWidth = _index - indexHeight * _length1;
                Current = new IndexTuple(indexWidth, indexHeight);
                return true;
            }

            /// <summary> Back to the values the constructor writes. </summary>
            public void Reset() => _index = -1;
            object IEnumerator.Current => Current;
            /// <summary> Nothing to release; the walk allocates nothing. </summary>
            public void Dispose() { }
        }
    }
}
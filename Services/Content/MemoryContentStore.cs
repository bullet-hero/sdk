using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BH.SDK.Services.Content
{
    // The store that IS the test double, and the one the server reads an upload into. Both uses
    // want the same thing: a level's worth of files with no disk under them, addressed exactly the
    // way the real one is - so a pipeline proven here is proven, not approximated.
    //
    // THE CAP IS NOT TIDINESS. On the server this store is what a tar.gz is unpacked INTO, and a
    // gzip bomb is a few kilobytes that decompresses to as much as the process will accept. The
    // archive layer has its own limits; this is the last one, held by the thing actually allocating
    // the bytes, and it is checked while a blob is being written rather than only when it is
    // committed - by then the memory is already spent.
    //
    // Every operation completes synchronously, always, which is the reason the interface is written
    // in ValueTask: on this implementation the async machinery allocates nothing at all.

    /// <summary> An <see cref="IContentStore"/> over blobs held in memory. </summary>
    public sealed class MemoryContentStore : IContentStore
    {
        /// <summary> What a store accepts when the caller states no limit of its own - generous for
        /// a level, far below what an unbounded decompression would take. </summary>
        public const long DefaultMaxTotalBytes = 512L * 1024 * 1024;

        // Ordinal, case included: a package written on Linux can carry "Logo.png" and "logo.png" as
        // two entries, and a store that folded them would silently lose one.
        private readonly Dictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        private readonly long _maxTotalBytes;
        private long _totalBytes;

        /// <summary> Built from its "memory" and default max total bytes. </summary>
        public MemoryContentStore(string name = "memory", long maxTotalBytes = DefaultMaxTotalBytes)
        {
            if (maxTotalBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxTotalBytes));

            Name = string.IsNullOrEmpty(name) ? "memory" : name;
            _maxTotalBytes = maxTotalBytes;
        }

        /// <summary> What this store is called in a message; never part of a path. </summary>
        public string Name { get; }

        /// <summary> How many blobs the store holds. </summary>
        public int Count => _blobs.Count;

        /// <summary> What the store holds in total, in bytes. </summary>
        public long TotalBytes => _totalBytes;

        /// <summary> The most the store will hold before refusing a write. </summary>
        public long MaxTotalBytes => _maxTotalBytes;

        /// <summary> The bytes of one blob, without opening a stream. </summary>
        public bool TryGetBytes(string path, out byte[] bytes)
        {
            ContentPath.Require(path, nameof(path));
            return _blobs.TryGetValue(path, out bytes);
        }

        /// <summary> Writes one blob directly, for a caller that already holds its bytes. </summary>
        public void Write(string path, byte[] bytes)
        {
            ContentPath.Require(path, nameof(path));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            Commit(path, bytes);
        }

        /// <summary> Whether anything is stored under that path. </summary>
        public ValueTask<bool> ExistsAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ContentPath.Require(path, nameof(path));
            return new ValueTask<bool>(_blobs.ContainsKey(path));
        }

        /// <summary> Every path under a prefix. </summary>
        public ValueTask<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var results = new List<string>();
            foreach (var path in _blobs.Keys)
                if (ContentPath.HasPrefix(path, prefix))
                    results.Add(path);

            results.Sort(StringComparer.Ordinal);
            return new ValueTask<IReadOnlyList<string>>(results);
        }

        /// <summary> Reads one entry. </summary>
        public ValueTask<Stream> OpenReadAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ContentPath.Require(path, nameof(path));

            if (!_blobs.TryGetValue(path, out var bytes))
                throw new FileNotFoundException($"No '{path}' in store '{Name}'.", path);

            // Non-writable, over the stored array rather than a copy: a reader that could write
            // would be editing the store through a handle it was never given for that.
            Stream stream = new MemoryStream(bytes, 0, bytes.Length, writable: false);
            return new ValueTask<Stream>(stream);
        }

        /// <summary> Writes one entry, filed only once the stream is closed. </summary>
        public ValueTask<Stream> OpenWriteAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ContentPath.Require(path, nameof(path));

            // Replacing starts by releasing what the old blob held, so overwriting the same entry
            // in a loop does not count every version against the cap.
            Release(path);

            Stream stream = new CommittingStream(this, path);
            return new ValueTask<Stream>(stream);
        }

        /// <summary> Removes one entry; a missing one is not an error. </summary>
        public ValueTask DeleteAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ContentPath.Require(path, nameof(path));

            Release(path);
            return default;
        }

        /// <summary> How many bytes one entry holds. </summary>
        public ValueTask<long> GetLengthAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ContentPath.Require(path, nameof(path));

            if (!_blobs.TryGetValue(path, out var bytes))
                throw new FileNotFoundException($"No '{path}' in store '{Name}'.", path);

            return new ValueTask<long>(bytes.Length);
        }

        private void Release(string path)
        {
            if (!_blobs.TryGetValue(path, out var bytes)) return;

            _totalBytes -= bytes.Length;
            _blobs.Remove(path);
        }

        private void EnsureRoom(long extraBytes)
        {
            if (_totalBytes + extraBytes <= _maxTotalBytes) return;

            throw new IOException($"Store '{Name}' would hold {_totalBytes + extraBytes} bytes, " +
                                  $"over its {_maxTotalBytes} byte limit.");
        }

        private void Commit(string path, byte[] bytes)
        {
            Release(path);
            EnsureRoom(bytes.Length);

            _blobs[path] = bytes;
            _totalBytes += bytes.Length;
        }

        // MemoryStream's own write path is what enforces the cap: overriding the three entry points
        // it actually has - a span, an array segment and a single byte - covers WriteAsync and
        // CopyTo too, since both route through them. Subclassing rather than wrapping means every
        // other member (seeking, Position, ToArray) stays MemoryStream's, which is what the callers
        // above expect a writable stream to behave like.

        /// <summary> Holds a write in memory and files it under its key only when the stream is closed, so an
        /// abandoned write leaves nothing behind. </summary>
        private sealed class CommittingStream : MemoryStream
        {
            private readonly MemoryContentStore _owner;
            private readonly string _path;
            private bool _committed;

            /// <summary> Built from its owner and path. </summary>
            public CommittingStream(MemoryContentStore owner, string path)
            {
                _owner = owner;
                _path = path;
            }

            /// <summary> Buffers the bytes; nothing is filed until close. </summary>
            public override void Write(byte[] buffer, int offset, int count)
            {
                _owner.EnsureRoom(Length + count);
                base.Write(buffer, offset, count);
            }

            /// <summary> The same for a span. </summary>
            public override void Write(ReadOnlySpan<byte> buffer)
            {
                _owner.EnsureRoom(Length + buffer.Length);
                base.Write(buffer);
            }

            /// <summary> The same for one byte. </summary>
            public override void WriteByte(byte value)
            {
                _owner.EnsureRoom(Length + 1);
                base.WriteByte(value);
            }

            /// <summary> Files what was written under its key - so an abandoned write leaves nothing behind. </summary>
            protected override void Dispose(bool disposing)
            {
                // Once, and before the buffer goes away: a stream disposed twice must not commit a
                // second copy, and this is what makes "the blob is complete on Dispose" a contract
                // rather than a convention.
                if (disposing && !_committed)
                {
                    _committed = true;
                    _owner.Commit(_path, ToArray());
                }

                base.Dispose(disposing);
            }
        }
    }
}

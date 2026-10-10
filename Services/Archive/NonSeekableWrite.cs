using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BH.SDK.Services.Archive
{
    // Lies about seeking on purpose: ZipOutputStream writes a different file (patched header versus data
    // descriptor) when it can seek, and the container must not depend on WHERE it was written.
    // Every zip goes through this wrapper. Write-only by construction.

    /// <summary> Hides a destination's seekability, so a writer always takes its streaming path. </summary>
    public sealed class NonSeekableWrite : Stream
    {
        private readonly Stream _inner;
        private readonly bool _leaveOpen;
        private long _written;

        /// <summary> Wraps a destination, optionally owning it. </summary>
        public NonSeekableWrite(Stream inner, bool leaveOpen = true)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _leaveOpen = leaveOpen;
        }

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => _inner.CanWrite;

        /// <summary> How much has gone through, since the destination cannot be asked. </summary>
        public override long Length => _written;

        /// <inheritdoc/>
        public override long Position
        {
            get => _written;
            set => throw new NotSupportedException("This destination does not seek, by design.");
        }

        /// <inheritdoc/>
        public override void Flush() => _inner.Flush();

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            _inner.FlushAsync(cancellationToken);

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Write-only.");

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException("This destination does not seek, by design.");

        /// <inheritdoc/>
        public override void SetLength(long value) =>
            throw new NotSupportedException("This destination does not seek, by design.");

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            _written += count;
        }

        /// <inheritdoc/>
        public override async Task WriteAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            await _inner.WriteAsync(buffer, offset, count, cancellationToken);
            _written += count;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}

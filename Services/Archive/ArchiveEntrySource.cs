using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Archive
{
    // Where one entry's bytes come from, as "how to open it", not the bytes: an opener streams a 40 MB
    // track straight in and keeps ABSOLUTE PATHS OUT of this layer (the export that collects passes it
    // in). Docs/ARCHIVE.md.

    /// <summary> One entry to pack, and how to open its content when the pack reaches it. </summary>
    public sealed class ArchiveEntrySource
    {
        private readonly Func<CancellationToken, ValueTask<Stream>> _open;
        private readonly Func<CancellationToken, ValueTask<long>> _length;

        private ArchiveEntrySource(string path, Func<CancellationToken, ValueTask<Stream>> open,
            Func<CancellationToken, ValueTask<long>> length)
        {
            Path = ContentPath.Require(path, nameof(path));
            _open = open;
            _length = length;
        }

        /// <summary> The name this entry gets inside the archive. </summary>
        public string Path { get; }

        /// <summary> An entry whose bytes the caller already holds. </summary>
        public static ArchiveEntrySource FromBytes(string path, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            return new ArchiveEntrySource(path,
                _ => new ValueTask<Stream>(new MemoryStream(bytes, 0, bytes.Length, writable: false)),
                _ => new ValueTask<long>(bytes.Length));
        }

        /// <summary> An entry read out of a store when the pack reaches it. </summary>
        public static ArchiveEntrySource FromStore(string path, IContentStore store, string storePath = null)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));

            storePath = storePath ?? path;
            ContentPath.Require(storePath, nameof(storePath));

            return new ArchiveEntrySource(path,
                token => store.OpenReadAsync(storePath, token),
                token => store.GetLengthAsync(storePath, token));
        }

        /// <summary> An entry opened by the caller - the escape hatch for content this layer has no
        /// business knowing how to reach. </summary>
        public static ArchiveEntrySource FromOpener(string path,
            Func<CancellationToken, ValueTask<Stream>> open, Func<CancellationToken, ValueTask<long>> length)
        {
            if (open == null) throw new ArgumentNullException(nameof(open));
            if (length == null) throw new ArgumentNullException(nameof(length));

            return new ArchiveEntrySource(path, open, length);
        }

        /// <summary> How many bytes this entry will occupy. A tar header states the size before the
        /// content, so it has to be known without reading the content first. </summary>
        public ValueTask<long> GetLengthAsync(CancellationToken token) => _length(token);

        /// <summary> Opens this entry's content for reading. </summary>
        public ValueTask<Stream> OpenReadAsync(CancellationToken token) => _open(token);
    }
}
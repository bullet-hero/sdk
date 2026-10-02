using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Models.Profile;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Profile
{
    // ONE STORE PER CATEGORY, never one rooted at the profile itself: a store lists everything beneath
    // its root, and a store at the profile root would walk profile-backups/ - the previous backup and
    // an import's staging - on every export. The host hands each folder category its own store and the
    // settings document as bytes (it serializes the settings the game is running with, which is what
    // the player expects to carry, not a file that may be up to a debounce behind).
    //
    // Within a category the entries are sorted ordinally, so the same profile packs to the same bytes.

    /// <summary> Packs a profile into a zip: the manifest first, then each category in
    /// <see cref="ProfileLayout.PackOrder"/>. </summary>
    public static class ProfileArchiveWriter
    {
        /// <summary> What a pack wrote. </summary>
        public readonly struct Outcome
        {
            public Outcome(int files, long bytes)
            {
                Files = files;
                Bytes = bytes;
            }

            /// <summary> Entries written, the manifest included. </summary>
            public int Files { get; }

            /// <summary> Uncompressed bytes written, the manifest included. </summary>
            public long Bytes { get; }
        }

        /// <summary> Packs <paramref name="manifest"/>'s categories. A folder category with no store is
        /// packed empty; settings are packed from <paramref name="settings"/> when given. </summary>
        public static async Task<Outcome> PackAsync(ProfileManifest manifest,
            IReadOnlyDictionary<ProfileCategory, IContentStore> folders, byte[] settings,
            SerializationService serialization, Stream destination, IProgress<long> progress = null,
            CancellationToken token = default)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var entries = new List<ArchiveEntrySource>();
            var total = 0L;

            var manifestBytes = serialization.SerializeEnvelope(manifest, SerializationType.Json);
            entries.Add(ArchiveEntrySource.FromBytes(FileNames.ProfileManifestFileName, manifestBytes));
            total += manifestBytes.Length;

            foreach (var category in ProfileLayout.PackOrder)
            {
                if ((manifest.Categories & category) == 0) continue;

                if (ProfileLayout.IsFile(category))
                {
                    if (settings == null) continue;
                    entries.Add(ArchiveEntrySource.FromBytes(ProfileLayout.RootOf(category), settings));
                    total += settings.Length;
                    continue;
                }

                if (folders == null || !folders.TryGetValue(category, out var store) || store == null) continue;

                var listing = new List<string>(await store.ListAsync(string.Empty, token));
                listing.Sort(StringComparer.Ordinal);

                foreach (var relative in listing)
                {
                    if (ProfileLayout.IsTransient(category, relative)) continue;
                    total += await store.GetLengthAsync(relative, token);
                    entries.Add(Counted(ProfileLayout.Combine(category, relative), store, relative, progress));
                }
            }

            await ZipService.PackAsync(entries, destination, ProfileArchive.Policy, token);
            return new Outcome(entries.Count, total);
        }

        private static ArchiveEntrySource Counted(string path, IContentStore store, string relative,
            IProgress<long> progress)
        {
            if (progress == null) return ArchiveEntrySource.FromStore(path, store, relative);

            return ArchiveEntrySource.FromOpener(path,
                async token => new CountingReadStream(await store.OpenReadAsync(relative, token), progress),
                token => store.GetLengthAsync(relative, token));
        }

        // Reports every byte read out of a store, which is the honest fraction of a pack: deflate's
        // output size is not known in advance, the input's is.
        private sealed class CountingReadStream : Stream
        {
            private readonly Stream _inner;
            private readonly IProgress<long> _progress;

            public CountingReadStream(Stream inner, IProgress<long> progress)
            {
                _inner = inner;
                _progress = progress;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;

            public override long Position
            {
                get => _inner.Position;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var read = _inner.Read(buffer, offset, count);
                if (read > 0) _progress.Report(read);
                return read;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
                CancellationToken cancellationToken)
            {
                var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
                if (read > 0) _progress.Report(read);
                return read;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing) _inner.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}

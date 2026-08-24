using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BH.SDK.Services.Content
{
    // The half of the store contract that touches a disk, and the first file IO the SDK has ever
    // done. Everything about it is deliberately dull: ContentPath decides what a name may be, the
    // root decides where it lands, and nothing here interprets content.
    //
    // A WRITE CREATES THE DIRECTORIES IT NEEDS, INCLUDING THE ROOT. Export picks a folder that may
    // not exist yet and unpack writes a tree several levels deep; making the caller pre-create each
    // one means the same loop in every caller. A READ never creates anything - a missing blob is a
    // missing blob.
    //
    // Every method completes synchronously except the two that hand back a stream, and even those
    // only open a handle. That is not a defect of the async contract: the interface is async for
    // the implementation that genuinely needs it (the server's own store over a database), and
    // ValueTask is what makes saying so cost nothing here.

    /// <summary> An <see cref="IContentStore"/> over a directory on disk. </summary>
    public sealed class DirectoryContentStore : IContentStore
    {
        private readonly DirectoryInfo _root;
        private readonly string _rootFullPath;

        public DirectoryContentStore(DirectoryInfo root, string name = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));

            // Resolved once, with a trailing separator, so the containment check below is a string
            // comparison rather than a walk. GetFullPath also resolves "levels/../levels", which a
            // caller can legitimately have built out of its own configuration.
            _rootFullPath = Path.GetFullPath(root.FullName);
            if (_rootFullPath[_rootFullPath.Length - 1] != Path.DirectorySeparatorChar)
                _rootFullPath += Path.DirectorySeparatorChar;

            Name = string.IsNullOrEmpty(name) ? root.Name : name;
        }

        public DirectoryContentStore(string rootPath, string name = null)
            : this(new DirectoryInfo(rootPath), name) { }

        public string Name { get; }

        /// <summary> The directory this store is rooted at. For a host that has to name it. </summary>
        public DirectoryInfo Root => _root;

        public ValueTask<bool> ExistsAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new ValueTask<bool>(File.Exists(Resolve(path, nameof(path))));
        }

        public ValueTask<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var results = new List<string>();
            if (_root.Exists)
            {
                var rootLength = _rootFullPath.Length;
                foreach (var file in Directory.EnumerateFiles(_rootFullPath, "*", SearchOption.AllDirectories))
                {
                    token.ThrowIfCancellationRequested();

                    var relative = Path.GetFullPath(file).Substring(rootLength)
                        .Replace(Path.DirectorySeparatorChar, ContentPath.Separator)
                        .Replace(Path.AltDirectorySeparatorChar, ContentPath.Separator);

                    // A file the store cannot ADDRESS is not listed. It exists on disk and nothing
                    // here can name it, so reporting it would hand callers a path that fails at
                    // every other method on this interface.
                    if (!ContentPath.IsValid(relative)) continue;
                    if (!ContentPath.HasPrefix(relative, prefix)) continue;

                    results.Add(relative);
                }
            }

            results.Sort(StringComparer.Ordinal);
            return new ValueTask<IReadOnlyList<string>>(results);
        }

        public ValueTask<Stream> OpenReadAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var full = Resolve(path, nameof(path));
            if (!File.Exists(full))
                throw new FileNotFoundException($"No '{path}' in store '{Name}'.", full);

            Stream stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true);
            return new ValueTask<Stream>(stream);
        }

        public ValueTask<Stream> OpenWriteAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var full = Resolve(path, nameof(path));
            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            Stream stream = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true);
            return new ValueTask<Stream>(stream);
        }

        public ValueTask DeleteAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var full = Resolve(path, nameof(path));
            if (File.Exists(full)) File.Delete(full);
            return default;
        }

        public ValueTask<long> GetLengthAsync(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var full = Resolve(path, nameof(path));
            var info = new FileInfo(full);
            if (!info.Exists)
                throw new FileNotFoundException($"No '{path}' in store '{Name}'.", full);

            return new ValueTask<long>(info.Length);
        }

        // ContentPath already refuses "..", rooted paths and drive letters, so this cannot fail on
        // any name it accepted. It runs anyway, and that is the point of "rooted by construction":
        // the guarantee is a property of the type, held by the type, rather than an argument that
        // the validator upstream is exhaustive. A symlink INSIDE the root pointing out is the one
        // case no path check can see - refusing to unpack a link entry at all is what covers it,
        // and that lives in the archive layer.
        private string Resolve(string path, string parameterName)
        {
            ContentPath.Require(path, parameterName);

            var full = Path.GetFullPath(Path.Combine(_rootFullPath, path));
            if (!full.StartsWith(_rootFullPath, StringComparison.Ordinal))
                throw new ArgumentException($"'{path}' resolves outside store '{Name}'.", parameterName);

            return full;
        }
    }
}

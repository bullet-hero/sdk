using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BH.SDK.Services.Content
{
    // A named set of blobs, ROOTED BY CONSTRUCTION: escaping the folder is a property of the type, not a
    // check per call site. Files and memory are two implementations, never two APIs. ValueTask on
    // members, Task on pipeline entry points, no IAsyncEnumerable (IL2CPP risk). Docs/ARCHIVE.md.

    /// <summary> A rooted, named set of blobs addressed by relative path. </summary>
    public interface IContentStore
    {
        /// <summary> What this store is, for a report or a log line to name ("level folder",
        /// "archive"). Never a path a player should not see. </summary>
        string Name { get; }

        /// <summary> Whether a blob exists at this path. </summary>
        ValueTask<bool> ExistsAsync(string path, CancellationToken token);

        /// <summary> Every blob under a prefix, matched on segment boundaries; an empty prefix
        /// lists the whole store. Ordinal-sorted, so two listings of the same content agree and a
        /// pack of it is reproducible. </summary>
        ValueTask<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken token);

        /// <summary> Opens a blob for reading. Throws <see cref="FileNotFoundException"/> when
        /// there is none - ask <see cref="ExistsAsync"/> first when that is a possibility. </summary>
        ValueTask<Stream> OpenReadAsync(string path, CancellationToken token);

        /// <summary> Opens a blob for writing, replacing whatever was there. The blob is only
        /// complete once the returned stream is disposed. </summary>
        ValueTask<Stream> OpenWriteAsync(string path, CancellationToken token);

        /// <summary> Removes a blob. Removing one that is not there is not an error. </summary>
        ValueTask DeleteAsync(string path, CancellationToken token);

        /// <summary> Size of a blob in bytes. Throws <see cref="FileNotFoundException"/> when there
        /// is none, exactly like <see cref="OpenReadAsync"/>. </summary>
        ValueTask<long> GetLengthAsync(string path, CancellationToken token);
    }
}

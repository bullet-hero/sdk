using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BH.SDK.Services.Content
{
    // A named set of blobs, and the reason the SDK's file rule could be lifted without the library
    // learning about disks. "Against files" and "against data already in memory" are TWO
    // IMPLEMENTATIONS, never two parallel APIs: the editor hands over a DirectoryContentStore
    // rooted at the level folder, a test hands over a MemoryContentStore, and the server's own
    // DbContentStore is one more class with nothing else to change. A second API would mean every
    // pipeline above this is written twice and only one half is ever exercised by tests.
    //
    // A STORE IS ROOTED BY CONSTRUCTION. There is nothing outside the root to address, so "does
    // this escape the folder" stops being a check every call site has to remember and becomes a
    // property of the type - which is what makes the tar-slip defence structural rather than
    // vigilant.
    //
    // ValueTask, not Task, on every member here. They complete synchronously very often (a
    // MemoryContentStore always does, and even a directory store only opens a handle), they are
    // called once per archive entry in a loop, and each is awaited exactly once - which is the
    // precise profile ValueTask exists for. The pipeline entry points above (PackAsync, WriteAsync,
    // ReadAsync) return Task instead, because a caller is entitled to store one, pass it on or
    // combine it through Task.WhenAll, none of which ValueTask permits.
    //
    // IAsyncEnumerable is deliberately absent from ListAsync: a store listing is a level folder's
    // worth of names, and an async stream is extra IL2CPP risk for nothing.

    /// <summary> A rooted, named set of blobs addressed by relative path. </summary>
    public interface IContentStore
    {
        /// <summary> What this store is, for a report or a log line to name ("level folder",
        /// "package"). Never a path a player should not see. </summary>
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

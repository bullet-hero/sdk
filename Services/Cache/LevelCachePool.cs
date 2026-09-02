using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Cache
{
    // =========================================================================================
    // DEAD CODE, KEPT ON PURPOSE - A DELETION CANDIDATE.
    //
    // Nothing constructs, registers or calls this. The level cache is disconnected from the game:
    // `RootScope` does not register `LevelCacheService`, and `LevelLoaderService` no longer hooks
    // it on any read, write or delete. It is left in the tree for ONE reason - the session that
    // builds the `.blob` format is meant to read it before deleting it, because it is a worked
    // example of what a hand-written codec for this model looks like and which of its types are
    // polymorphic. See docs/issues/ROSLYN_PLAN.md, and LOADING_HISTORY.md section 10.
    //
    // WHY IT IS GOING RATHER THAN BEING FIXED: everything here is what a fast FORMAT does, plus
    // the cost of deciding when it is stale, an invalidation hook on every write path, and a
    // second hand-written serializer to keep in step with the real one. A format has none of
    // those problems, because it cannot disagree with the file - it IS the file.
    //
    // DO NOT WIRE IT BACK UP to make something faster. If loading is slow, that is the format's
    // job now.
    // =========================================================================================

    // A POOL OF PARSED LEVELS, NOT A FILE CACHE, and the difference is which question it answers.
    // The expensive thing is turning bytes into a `Level`, and a player does that to the same level
    // tens of times - once per attempt, and again every time they come back to it tomorrow. So
    // there are two tiers and they cover two different repeats: the MEMORY tier covers the same
    // session, the STORE tier covers the next one.
    //
    // BOTH TIERS HOLD BYTES, NEVER A `Level`. A pooled graph would have to be handed out, and every
    // consumer of a level MUTATES it - the editor edits it, the runtime is handed it as the thing
    // it plays. Handing the same instance to two callers is the class of bug that shows up a week
    // later as a level with somebody else's edits in it, and the alternative - handing out
    // `Copy()` - costs 131 ms on a heavy level, most of what the decode itself costs. Bytes cannot
    // be aliased, so the question does not arise.
    //
    // THE STORE IS AN `IContentStore` FOR THE REASON THE PACKAGE PIPELINE USES ONE: this library
    // does not learn about disks. The consumer hands over a directory store, a test hands over a
    // memory store, and a server hands over whatever it keeps blobs in - see that interface's own
    // header. It is OPTIONAL: a pool with none is a session-lifetime pool and nothing else, which
    // is exactly right for a host that has no writable location.
    //
    // NOTHING HERE THROWS FOR A CACHE THAT CANNOT ANSWER. A miss, a corrupt payload, a store that
    // refuses to open - all of them mean the caller loads the level the ordinary way, which is the
    // only behaviour that makes a cache safe to add to a path that already worked.

    /// <summary> Keeps parsed levels, in memory for this session and in a store for the next.
    /// </summary>
    public sealed class LevelCachePool
    {
        /// <summary> Where a payload lives inside the store. </summary>
        public const string Extension = ".levelcache";

        /// <summary> How many payloads the memory tier keeps by default. </summary>
        public const int DefaultMemoryEntries = 3;

        /// <summary> How many bytes the memory tier keeps by default - one heavy level's payload is
        /// a few megabytes, and this is what stops a browse through a folder of them from becoming
        /// a memory leak with a friendly name. </summary>
        public const long DefaultMemoryBytes = 64L * 1024 * 1024;

        private readonly SerializationService _serialization;
        private readonly IContentStore _store;
        private readonly int _maxEntries;
        private readonly long _maxBytes;

        // Insertion-ordered by hand rather than by a LinkedList: the cap is a handful of entries, so
        // a list scan is cheaper than the bookkeeping, and the order has to survive a re-Put of a
        // key that is already in (which is a refresh, not a promotion - nothing here reorders on
        // READ, since a pool this small has nothing to evict wisely).
        private readonly List<LevelCacheKey> _order = new();
        private readonly Dictionary<LevelCacheKey, byte[]> _memory = new();
        private long _memoryBytes;

        /// <summary> How many payloads the memory tier is holding. </summary>
        public int Count => _memory.Count;

        /// <summary> How many bytes it is holding. </summary>
        public long Bytes => _memoryBytes;

        /// <summary> Whether anything is written for the next session. </summary>
        public bool HasStore => _store != null;

        public LevelCachePool(SerializationService serialization, IContentStore store = null,
            int maxEntries = DefaultMemoryEntries, long maxBytes = DefaultMemoryBytes)
        {
            _serialization = serialization ?? throw new ArgumentNullException(nameof(serialization));
            _store = store;
            _maxEntries = maxEntries > 0 ? maxEntries : DefaultMemoryEntries;
            _maxBytes = maxBytes > 0 ? maxBytes : DefaultMemoryBytes;
        }

        // Synchronous on purpose, and it is the whole point of the memory tier: a host can ask this
        // on the frame it needs the level, before it decides to start an async load at all.

        /// <summary> Hands back a level from the memory tier alone. False when it is not there.
        /// </summary>
        public bool TryGet(LevelCacheKey key, out Level level)
        {
            level = null;
            if (!key.IsValid) return false;
            if (!_memory.TryGetValue(key, out var payload)) return false;

            if (LevelCacheCodec.TryRead(_serialization, payload, out level)) return true;

            // A payload this process wrote and cannot read back is a defect rather than staleness,
            // so it is dropped instead of being tried again on every load.
            Forget(key);
            return false;
        }

        /// <summary> Hands back a level from either tier, reading the store when the memory tier
        /// misses. Null when neither answers. </summary>
        public async Task<Level> GetAsync(LevelCacheKey key, CancellationToken token)
        {
            if (TryGet(key, out var pooled)) return pooled;
            if (_store == null || !key.IsValid) return null;

            var payload = await ReadStoreAsync(key, token).ConfigureAwait(false);
            if (payload == null) return null;

            if (!LevelCacheCodec.TryRead(_serialization, payload, out var level)) return null;

            Remember(key, payload);
            return level;
        }

        /// <summary> Encodes a level into both tiers. Does nothing for a key that names no source.
        /// </summary>
        public async Task PutAsync(LevelCacheKey key, Level level, CancellationToken token)
        {
            if (!key.IsValid || level == null) return;

            byte[] payload;
            try
            {
                payload = LevelCacheCodec.Write(_serialization, level);
            }
            catch (Exception)
            {
                // A level this codec cannot express is a level that is never cached. It still
                // loads, every time, exactly as it did before this existed.
                return;
            }

            // THE PAYLOAD IS READ BACK AND COMPARED BEFORE IT IS KEPT, and this is not belt and
            // braces - it is the difference between a cache that can be WRONG and one that can only
            // be ABSENT. Everything else here degrades safely: a miss, a refusal, a store that will
            // not open all end in an ordinary load. Storing a payload that decodes into a different
            // level is the one failure that does not, and it outlives the session that caused it -
            // the player then gets that level back on every launch until something invalidates it.
            //
            // It costs one decode and one deep compare on the WRITE path only, which is the path
            // that has just spent seconds parsing the file. That is the cheapest moment in the
            // whole feature to buy this, and `Level.Equals` is the model's own answer to "are these
            // the same level" rather than a second opinion invented here.
            if (!LevelCacheCodec.TryRead(_serialization, payload, out var verified)
                || !level.Equals(verified))
            {
                return;
            }

            Remember(key, payload);

            if (_store == null) return;

            try
            {
                await using var stream = await _store.OpenWriteAsync(PathOf(key), token).ConfigureAwait(false);
                await stream.WriteAsync(payload, 0, payload.Length, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // A read-only or full store is a pool with one tier, not a failed load.
            }
        }

        /// <summary> Drops every payload for a source, whichever version of it. Called when the
        /// source is known to have changed - a save, a delete - so a stale payload does not sit in
        /// the store until something happens to notice its stamp. </summary>
        public async Task InvalidateAsync(string name, CancellationToken token)
        {
            if (string.IsNullOrEmpty(name)) return;

            for (var i = _order.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_order[i].Name, name, StringComparison.Ordinal))
                    Forget(_order[i]);
            }

            if (_store == null) return;

            try
            {
                // EVERY VERSION OF IT, never just the current one: what makes a payload stale is
                // that the source moved, so the blob to remove is addressed by a length and a stamp
                // this caller no longer knows. Listing by the source's own prefix is what finds them.
                var prefix = PrefixOf(name);
                var stored = await _store.ListAsync(string.Empty, token).ConfigureAwait(false);

                foreach (var path in stored)
                {
                    if (path.StartsWith(prefix, StringComparison.Ordinal))
                        await _store.DeleteAsync(path, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Same reasoning as a failed write: the worst a leftover payload can do is be
                // refused by its own key.
            }
        }

        /// <summary> Empties the memory tier. The store is left alone - it is what the next session
        /// reads. </summary>
        public void Clear()
        {
            _memory.Clear();
            _order.Clear();
            _memoryBytes = 0;
        }

        private async Task<byte[]> ReadStoreAsync(LevelCacheKey key, CancellationToken token)
        {
            var path = PathOf(key);

            try
            {
                if (!await _store.ExistsAsync(path, token).ConfigureAwait(false)) return null;

                await using var stream = await _store.OpenReadAsync(path, token).ConfigureAwait(false);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, 81920, token).ConfigureAwait(false);
                return buffer.ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // THE KEY IS IN THE PATH, not only inside the payload, so a stale payload is never even
        // opened: a level whose length or stamp moved addresses a different blob. Reading a 3 MB
        // file to discover it is stale is exactly the cost this exists to avoid.
        //
        // THE NAME IS HASHED RATHER THAN USED, and that is correctness before tidiness. A source
        // identity is whatever the host tells two levels apart by - a folder path, here - and a path
        // carries separators, which inside a rooted store would address a FOLDER, plus characters a
        // file system refuses. A hash has neither problem and is a fixed width, which is what makes
        // the prefix below a prefix rather than a guess.
        private static string PathOf(LevelCacheKey key)
            => $"{PrefixOf(key.Name)}{key.Length:x}-{key.Stamp:x}-{key.Format:x}{Extension}";

        /// <summary> Everything ever written for one source shares this. </summary>
        private static string PrefixOf(string name) => $"{Hash(name):x16}.";

        // FNV-1a, written out rather than taken from string.GetHashCode(): that one is randomized
        // per process on modern runtimes, so a payload written in one session would be unfindable
        // in the next - which is the entire point of the store tier.
        private static ulong Hash(string name)
        {
            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;

            var hash = offset;
            for (var i = 0; i < name.Length; i++)
            {
                hash ^= name[i];
                hash *= prime;
            }

            return hash;
        }

        private void Remember(LevelCacheKey key, byte[] payload)
        {
            Forget(key);

            _memory[key] = payload;
            _order.Add(key);
            _memoryBytes += payload.LongLength;

            // Oldest first, and a single payload larger than the whole budget is kept anyway: it is
            // the level the player is opening right now, and evicting it would make the pool empty
            // exactly when it is needed.
            while (_order.Count > 1 && (_memory.Count > _maxEntries || _memoryBytes > _maxBytes))
                Forget(_order[0]);
        }

        private void Forget(LevelCacheKey key)
        {
            if (!_memory.TryGetValue(key, out var payload)) return;

            _memoryBytes -= payload.LongLength;
            _memory.Remove(key);
            _order.Remove(key);
        }
    }
}

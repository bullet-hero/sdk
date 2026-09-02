using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;

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

    /// <summary> What the cache's own payload is, and when a reader must refuse one. </summary>
    public static class LevelCacheFormat
    {
        /// <summary> First four bytes of every payload. </summary>
        public const uint Magic = 0x43_4C_48_42; // "BHLC"

        // BUMP THIS FOR ANY CHANGE TO WHAT EITHER CODEC WRITES, including one that looks additive.
        // There is no migration path and there must never be one: a payload this reader cannot
        // reproduce exactly is a cache MISS, and a miss costs one ordinary load. Forgetting to bump
        // it is the one way this feature can hand a level back wrong instead of slowly.
        /// <summary> The codec generation. A payload written by any other is refused. </summary>
        public const int Version = 1;
    }

    // TWO HALVES, AND THE SPLIT IS WHERE THE COST IS. `Game.Objects` is 18.1 MB of volcano's
    // 18.5 MB and the overwhelming majority of the parse, so it is written by hand
    // (`LevelObjectCodec`). Everything else - settings, resources, the four event aggregates, the
    // audio, the hints - is a few hundred kilobytes on the same level, and it goes through the REAL
    // serializer as an ordinary BSON envelope.
    //
    // That is not a compromise, it is what keeps this maintainable: the small half stays correct on
    // its own whenever the model changes, with no per-type code to update and no chance of the
    // cache and the format disagreeing about a field nobody remembered. The hand-written half is
    // bounded to the object tree, which is the part that changes rarely and pays enormously.
    //
    // THE OBJECTS ARE LIFTED OUT RATHER THAN COPIED. Serializing a level whose objects have been
    // swapped for an empty dictionary is O(1) to arrange and is restored in a `finally`, where
    // taking a `Level.Copy()` first would cost 131 ms on a level this size - most of what the write
    // is trying to be cheaper than. Nothing else may touch the level during that window, which is
    // true by construction: it is one synchronous call.
    //
    // PREFAB TEMPLATES CARRY OBJECTS TOO, and they go through the same codec for the same reason. A
    // level whose content is mostly prefabs would otherwise put its whole tree back on the slow
    // half.

    /// <summary> Writes and reads a whole <see cref="Level"/> as the cache's binary payload. </summary>
    public static class LevelCacheCodec
    {
        /// <summary> Encodes a level. Never mutates it: what is lifted out is put back before this
        /// returns, including when the write throws. </summary>
        public static byte[] Write(SerializationService serialization, Level level)
        {
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));
            if (level == null) throw new ArgumentNullException(nameof(level));

            var objects = level.Game?.Objects;
            var prefabs = CollectPrefabObjects(level);

            byte[] meta;
            try
            {
                if (level.Game != null) level.Game.Objects = new Dictionary<ObjectId, RectObject>();
                foreach (var prefab in EnumeratePrefabs(level))
                    prefab.Value.Objects = new Dictionary<ObjectId, RectObject>();

                meta = serialization.SerializeEnvelope(level, SerializationType.Bson);
            }
            finally
            {
                if (level.Game != null) level.Game.Objects = objects;
                foreach (var prefab in EnumeratePrefabs(level))
                {
                    if (prefabs.TryGetValue(prefab.Key, out var restored))
                        prefab.Value.Objects = restored;
                }
            }

            using var stream = new MemoryStream(meta.Length + 1024);
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(LevelCacheFormat.Magic);
                writer.Write(LevelCacheFormat.Version);

                writer.Write(meta.Length);
                writer.Write(meta);

                LevelObjectCodec.WriteObjects(writer, objects);

                writer.Write(prefabs.Count);
                foreach (var pair in prefabs)
                {
                    WriteGuid(writer, pair.Key.value);
                    LevelObjectCodec.WriteObjects(writer, pair.Value);
                }
            }

            return stream.ToArray();
        }

        // EVERY FAILURE IS `false`, AND THAT IS THE WHOLE SAFETY MODEL. A truncated payload, one
        // from another codec generation, one naming a prefab this level no longer has - all of them
        // are a cache that cannot answer, which costs an ordinary load. Throwing would make a
        // corrupt cache worse than no cache, which is the one thing a cache may never be.
        /// <summary> Decodes a payload. False for anything this reader cannot reproduce exactly.
        /// </summary>
        public static bool TryRead(SerializationService serialization, byte[] payload, out Level level)
        {
            level = null;
            if (serialization == null || payload == null || payload.Length < 12) return false;

            try
            {
                using var stream = new MemoryStream(payload, false);
                using var reader = new BinaryReader(stream, Encoding.UTF8, true);

                if (reader.ReadUInt32() != LevelCacheFormat.Magic) return false;
                if (reader.ReadInt32() != LevelCacheFormat.Version) return false;

                var metaLength = reader.ReadInt32();
                if (metaLength < 0 || metaLength > payload.Length) return false;

                var meta = reader.ReadBytes(metaLength);
                if (meta.Length != metaLength) return false;

                var read = serialization.DeserializeEnvelope<Level>(meta, SerializationType.Bson);
                if (read?.Game == null) return false;

                read.Game.Objects = LevelObjectCodec.ReadObjects(reader);

                var prefabCount = reader.ReadInt32();
                for (var i = 0; i < prefabCount; i++)
                {
                    var prefabId = new PrefabId(ReadGuid(reader));
                    var objects = LevelObjectCodec.ReadObjects(reader);

                    if (read.Resources?.Prefabs == null) return false;
                    if (!read.Resources.Prefabs.TryGetValue(prefabId, out var prefab)) return false;

                    prefab.Objects = objects;
                }

                level = read;
                return true;
            }
            catch (Exception)
            {
                // Deliberately every exception: a payload is arbitrary bytes off a disk, and the
                // list of ways arbitrary bytes can fail to be a level is not one worth enumerating.
                level = null;
                return false;
            }
        }

        private static Dictionary<PrefabId, Dictionary<ObjectId, RectObject>> CollectPrefabObjects(Level level)
        {
            var collected = new Dictionary<PrefabId, Dictionary<ObjectId, RectObject>>();
            foreach (var prefab in EnumeratePrefabs(level))
                collected[prefab.Key] = prefab.Value.Objects;

            return collected;
        }

        private static IEnumerable<KeyValuePair<PrefabId, Prefab>> EnumeratePrefabs(Level level)
        {
            var prefabs = level.Resources?.Prefabs;
            if (prefabs == null) yield break;

            foreach (var pair in prefabs)
                yield return pair;
        }

        private static void WriteGuid(BinaryWriter writer, Guid value)
        {
            Span<byte> bytes = stackalloc byte[16];
            value.TryWriteBytes(bytes);
            for (var i = 0; i < 16; i++) writer.Write(bytes[i]);
        }

        private static Guid ReadGuid(BinaryReader reader)
        {
            Span<byte> bytes = stackalloc byte[16];
            for (var i = 0; i < 16; i++) bytes[i] = reader.ReadByte();
            return new Guid(bytes);
        }
    }
}

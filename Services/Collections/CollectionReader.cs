using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Models.Collections;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Content;
using BH.SDK.Versions;

namespace BH.SDK.Services.Collections
{
    /// <summary> How reading a collection ended. </summary>
    public enum CollectionReadResult
    {
        /// <summary> Read; individual files may still have been skipped. </summary>
        Ok = 0,

        /// <summary> No collection.* at the root - this is not a collection. </summary>
        NotACollection = 1,

        /// <summary> The manifest was written by a newer build. </summary>
        NewerGeneration = 2,

        /// <summary> The manifest is not readable. </summary>
        Damaged = 3,
    }

    // A COLLECTION ARRIVES FROM ANYWHERE - a subscribed Workshop folder, an archive somebody sent, a
    // folder a player dropped in by hand - so this reads the way LevelArchiveReader does: every failure
    // is a value, and nothing is trusted to be what its name says. The MANIFEST decides whether there is
    // a collection at all; a single bad entry below it only costs that entry, reported in Skipped, the
    // same way the device library leaves out one file it cannot read rather than the whole library.

    /// <summary> Reads a collection out of a store - a folder on disk, an unpacked archive, memory. </summary>
    public static class CollectionReader
    {
        /// <summary> The outcome, and the content when there is one. </summary>
        public readonly struct Outcome
        {
            /// <summary> How it ended. </summary>
            public readonly CollectionReadResult Result;

            /// <summary> What was read; null unless <see cref="Result"/> is Ok. </summary>
            public readonly CollectionContent Content;

            /// <summary> Built from its parts. </summary>
            public Outcome(CollectionReadResult result, CollectionContent content)
            {
                Result = result;
                Content = content;
            }

            /// <summary> Whether there is content. </summary>
            public bool IsOk => Result == CollectionReadResult.Ok;
        }

        /// <summary> Whether the store holds a collection manifest at its root - the marker a Workshop
        /// item or a dropped folder is classified by, without reading it. </summary>
        public static async Task<bool> IsCollectionAsync(IContentStore store, CancellationToken token = default)
            => (await LocateManifestAsync(store, token)).Path != null;

        /// <summary> Reads the manifest and every entry. </summary>
        public static async Task<Outcome> ReadAsync(IContentStore store, SerializationService serialization,
            CancellationToken token = default)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));

            var (path, format) = await LocateManifestAsync(store, token);
            if (path == null) return new Outcome(CollectionReadResult.NotACollection, null);

            ResourceCollection manifest;
            try
            {
                manifest = serialization.DeserializeEnvelope<ResourceCollection>(
                    await ReadAllAsync(store, path, token), format);
            }
            catch (NewerGenerationException)
            {
                return new Outcome(CollectionReadResult.NewerGeneration, null);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return new Outcome(CollectionReadResult.Damaged, null);
            }

            if (manifest == null) return new Outcome(CollectionReadResult.Damaged, null);

            var resources = new LevelResources();
            var skipped = new List<CollectionSkippedEntry>();

            await ReadKindAsync<Prefab>(store, serialization, FileNames.PrefabsDirectory, skipped, token,
                (id, value) =>
                {
                    if (value.PrefabId.value != id) return false;
                    resources.Prefabs[value.PrefabId] = value;
                    return true;
                });
            await ReadKindAsync<ThemeData>(store, serialization, FileNames.ThemesDirectory, skipped, token,
                (id, value) =>
                {
                    if (value.ThemeId.value != id) return false;
                    resources.Themes[value.ThemeId] = value;
                    return true;
                });
            await ReadKindAsync<CompositeShape>(store, serialization, FileNames.ShapesDirectory, skipped, token,
                (id, value) =>
                {
                    if (value.ShapeId.value != id) return false;
                    resources.CompositeShapes[value.ShapeId] = value;
                    return true;
                });
            await ReadKindAsync<EffectData>(store, serialization, FileNames.EffectsDirectory, skipped, token,
                (id, value) =>
                {
                    if (value.EffectId.value != id) return false;
                    resources.Effects[value.EffectId] = value;
                    return true;
                });

            return new Outcome(CollectionReadResult.Ok,
                new CollectionContent(manifest, resources, format, store, skipped));
        }

        /// <summary> The store path a data resource of this kind and id is stored under. </summary>
        public static string EntryPath(string kindDirectory, Guid id, SerializationType format)
            => kindDirectory + ContentPath.Separator + id.ToString("D") + format.ToFileExtension();

        /// <summary> The manifest's store path in a given format. </summary>
        public static string ManifestPath(SerializationType format)
            => FileNames.CollectionFileBaseName + format.ToFileExtension();

        // The file name is the id, as in the device library: it is what the folder is keyed by, and a
        // payload that disagrees with its own name is left out rather than imported under either id.
        private static async Task ReadKindAsync<T>(IContentStore store, SerializationService serialization,
            string directory, List<CollectionSkippedEntry> skipped, CancellationToken token,
            Func<Guid, T, bool> accept)
        {
            var listing = await store.ListAsync(directory, token);
            foreach (var path in listing)
            {
                token.ThrowIfCancellationRequested();

                var name = path.Substring(directory.Length + 1);
                if (name.IndexOf(ContentPath.Separator) >= 0) continue; // nothing nests under a kind folder

                if (!SerializationTypeExtensions.TryFromFileExtension(Path.GetExtension(name), out var format))
                    continue;

                if (!Guid.TryParse(Path.GetFileNameWithoutExtension(name), out var id))
                {
                    skipped.Add(new CollectionSkippedEntry(path, CollectionEntryProblem.IdMismatch));
                    continue;
                }

                T value;
                try
                {
                    value = serialization.DeserializeEnvelope<T>(await ReadAllAsync(store, path, token), format);
                }
                catch (NewerGenerationException)
                {
                    skipped.Add(new CollectionSkippedEntry(path, CollectionEntryProblem.NewerGeneration));
                    continue;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    skipped.Add(new CollectionSkippedEntry(path, CollectionEntryProblem.Damaged));
                    continue;
                }

                if (value == null)
                    skipped.Add(new CollectionSkippedEntry(path, CollectionEntryProblem.Damaged));
                else if (!accept(id, value))
                    skipped.Add(new CollectionSkippedEntry(path, CollectionEntryProblem.IdMismatch));
            }
        }

        private static async Task<(string Path, SerializationType Format)> LocateManifestAsync(IContentStore store,
            CancellationToken token)
        {
            foreach (var format in SerializationTypeExtensions.ProbeOrder)
            {
                var path = ManifestPath(format);
                if (await store.ExistsAsync(path, token)) return (path, format);
            }

            return (null, default);
        }

        private static async Task<byte[]> ReadAllAsync(IContentStore store, string path, CancellationToken token)
        {
            using var content = await store.OpenReadAsync(path, token);
            using var buffer = new MemoryStream();

            await content.CopyToAsync(buffer, 81920, token);
            return buffer.ToArray();
        }
    }
}

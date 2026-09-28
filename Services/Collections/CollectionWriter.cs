using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Collections
{
    // A WRITE IS A WHOLE SNAPSHOT. What the content holds is what the store holds afterwards: every
    // data resource as its own envelope, the manifest, the media files the manifest names - and any
    // document or media file left over from an earlier write that the content no longer holds is
    // removed. Anything else in the store (a cover, a readme an author dropped in) is not this
    // writer's to touch.
    //
    // THE MANIFEST GOES LAST. A reader decides "is this a collection" by the manifest alone, so a write
    // interrupted half way leaves either the previous collection with some new files beside it, or
    // no manifest at all on a fresh one - never a manifest pointing at files that were not written.

    /// <summary> Writes a collection into a store. </summary>
    public static class CollectionWriter
    {
        /// <summary> Writes <paramref name="content"/> into <paramref name="target"/>. Media files are
        /// copied from <paramref name="media"/>, whose paths are the store paths they land under; a
        /// media path the manifest does not name is not written, and a file already in the target
        /// store must not be handed over again - a copy onto itself truncates it. </summary>
        public static async Task WriteAsync(IContentStore target, CollectionContent content,
            IReadOnlyList<ArchiveEntrySource> media, SerializationService serialization,
            SerializationType format, CancellationToken token = default)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));

            var written = new HashSet<string>(StringComparer.Ordinal);
            var resources = content.Resources;

            foreach (var prefab in resources.Prefabs.Values)
                await WriteEnvelopeAsync(target, CollectionReader.EntryPath(FileNames.PrefabsDirectory,
                    prefab.PrefabId.value, format), prefab, serialization, format, written, token);
            foreach (var theme in resources.Themes.Values)
                await WriteEnvelopeAsync(target, CollectionReader.EntryPath(FileNames.ThemesDirectory,
                    theme.ThemeId.value, format), theme, serialization, format, written, token);
            foreach (var shape in resources.CompositeShapes.Values)
                await WriteEnvelopeAsync(target, CollectionReader.EntryPath(FileNames.ShapesDirectory,
                    shape.ShapeId.value, format), shape, serialization, format, written, token);
            foreach (var effect in resources.Effects.Values)
                await WriteEnvelopeAsync(target, CollectionReader.EntryPath(FileNames.EffectsDirectory,
                    effect.EffectId.value, format), effect, serialization, format, written, token);

            var named = MediaPathsOf(content);
            if (media != null)
            {
                foreach (var file in media)
                {
                    if (!named.Contains(file.Path) || written.Contains(file.Path)) continue;

                    using (var source = await file.OpenReadAsync(token))
                    using (var destination = await target.OpenWriteAsync(file.Path, token))
                        await source.CopyToAsync(destination, 81920, token);

                    written.Add(file.Path);
                }
            }

            // Media the manifest names but nobody handed over may already be in the store - a rewrite
            // of a collection in place - and is kept rather than pruned.
            foreach (var path in named)
                if (!written.Contains(path) && await target.ExistsAsync(path, token))
                    written.Add(path);

            await PruneAsync(target, written, token);

            var manifestPath = CollectionReader.ManifestPath(format);
            await WriteEnvelopeAsync(target, manifestPath, content.Manifest, serialization, format, written, token);

            // One manifest per collection: a format change must not leave the old one beside it,
            // where the probe order could read the stale copy.
            foreach (var other in SerializationTypeExtensions.ProbeOrder)
            {
                var otherPath = CollectionReader.ManifestPath(other);
                if (otherPath != manifestPath) await target.DeleteAsync(otherPath, token);
            }
        }

        /// <summary> Every media path the manifest's file resources point at - the LevelPath sources,
        /// read relative to the collection root. </summary>
        public static HashSet<string> MediaPathsOf(CollectionContent content)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            if (content?.Manifest == null) return paths;

            foreach (var texture in content.Manifest.Textures.Values) AddSources(texture, paths);
            foreach (var font in content.Manifest.Fonts.Values) AddSources(font, paths);
            foreach (var audio in content.Manifest.Audios.Values) AddSources(audio, paths);
            return paths;
        }

        private static void AddSources(Resource resource, HashSet<string> paths)
        {
            if (resource?.Sources == null) return;

            foreach (var source in resource.Sources)
                if (source != null && source.UriType == ResourceUriType.LevelPath && ContentPath.IsValid(source.Uri))
                    paths.Add(source.Uri);
        }

        private static async Task WriteEnvelopeAsync<T>(IContentStore target, string path, T value,
            SerializationService serialization, SerializationType format, HashSet<string> written,
            CancellationToken token)
        {
            var bytes = serialization.SerializeEnvelope(value, format);
            using (var destination = await target.OpenWriteAsync(path, token))
                await destination.WriteAsync(bytes, 0, bytes.Length, token);

            written.Add(path);
        }

        // Only what this writer owns is pruned: the four kind folders and media/. A file elsewhere in
        // the store was put there by someone else.
        private static async Task PruneAsync(IContentStore target, HashSet<string> keep, CancellationToken token)
        {
            var owned = new[]
            {
                FileNames.PrefabsDirectory, FileNames.ThemesDirectory, FileNames.ShapesDirectory,
                FileNames.EffectsDirectory, FileNames.MediaDirectory,
            };

            foreach (var directory in owned)
            {
                var listing = await target.ListAsync(directory, token);
                foreach (var path in listing)
                    if (!keep.Contains(path))
                        await target.DeleteAsync(path, token);
            }
        }
    }
}

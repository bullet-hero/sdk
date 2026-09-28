using System;
using System.Collections.Generic;
using System.IO;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Services.Content;
using BH.SDK.Utils;

namespace BH.SDK.Services.Collections
{
    /// <summary> One media file an export has to copy into the collection. </summary>
    public readonly struct ExportFile
    {
        /// <summary> Where the bytes are now - a source of the level's resource, as the level stores it. </summary>
        public readonly ResourceKey Source;

        /// <summary> Store path inside the collection, under media/. </summary>
        public readonly string DestinationPath;

        /// <summary> Built from its parts. </summary>
        public ExportFile(ResourceKey source, string destinationPath)
        {
            Source = source;
            DestinationPath = destinationPath;
        }
    }

    /// <summary> What an export did to the collection, and what the host still has to copy. </summary>
    public sealed class CollectionExportResult
    {
        /// <summary> Every resource written into the collection, by its id there. </summary>
        public ResourceSet Written { get; } = new();

        /// <summary> Media the host copies from the level into the collection's store. </summary>
        public List<ExportFile> Files { get; } = new();

        /// <summary> How many resources came along that were not selected. </summary>
        public int DependencyCount { get; internal set; }
    }

    // EXPORT IS THE MIRROR OF IMPORT, with one decision made the other way round: a data resource the
    // collection already holds under the same id is REPLACED, not asked about. The collection being
    // written to is the author's own - read-only ones are never a target - so a second export of an
    // edited prefab is an update, which is the whole point of sending it again.
    //
    // A file resource gets a collection-local id and a file under media/, whatever kind of source the
    // level had for it: a LevelPath file and an AbsolutePath file are both bytes on this device and
    // both are copied in, since a collection has no other device's path to rely on. A URL or a
    // StreamingAssets source is carried as it is - there are no bytes to copy, and the collection works
    // wherever the level did.

    /// <summary> Writes part of a level's resources into a collection held in memory. </summary>
    public static class CollectionExportPlanner
    {
        /// <summary> Copies <paramref name="selection"/> and everything it needs from the level into
        /// <paramref name="target"/>, credits included. The level is never modified. </summary>
        public static CollectionExportResult Export(LevelResources source, LevelMeta sourceMeta,
            IEnumerable<ResourceRef> selection, CollectionContent target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var roots = new ResourceSet();
            if (selection != null)
                foreach (var reference in selection)
                    roots.Add(reference);

            var closure = ResourceClosure.Collect(source, roots);
            var result = new CollectionExportResult();

            var selected = 0;
            foreach (var reference in roots)
                if (ResourceClosure.Owns(source, reference))
                    selected++;
            result.DependencyCount = Math.Max(0, closure.Count - selected);

            var manifest = target.Manifest;
            var takenIds = new Dictionary<ResourceType, HashSet<int>>
            {
                [ResourceType.Texture] = Ints(manifest.Textures.Keys, id => id.value),
                [ResourceType.Font] = Ints(manifest.Fonts.Keys, id => id.value),
                [ResourceType.Audio] = Ints(manifest.Audios.Keys, id => id.value),
            };
            var takenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in CollectionWriter.MediaPathsOf(target))
                takenNames.Add(Path.GetFileName(path));

            var remap = new ResourceRemap();
            foreach (var reference in closure)
                if (!reference.Type.IsGuidAddressed())
                    remap.Add(reference, ResourceRef.Of(reference.Type,
                        ResourceNaming.NextUserId(takenIds[reference.Type]), Guid.Empty));

            var resources = target.Resources;
            foreach (var reference in closure)
            {
                switch (reference.Type)
                {
                    case ResourceType.Prefab:
                        var prefab = source.Prefabs[reference.AsPrefab].Copy();
                        remap.Apply(prefab);
                        resources.Prefabs[prefab.PrefabId] = prefab;
                        break;
                    case ResourceType.Effect:
                        var effect = source.Effects[reference.AsEffect].Copy();
                        remap.Apply(effect);
                        resources.Effects[effect.EffectId] = effect;
                        break;
                    case ResourceType.Theme:
                        var theme = source.Themes[reference.AsTheme].Copy();
                        resources.Themes[theme.ThemeId] = theme;
                        break;
                    case ResourceType.Shape:
                        var shape = source.CompositeShapes[reference.AsShape].Copy();
                        resources.CompositeShapes[shape.ShapeId] = shape;
                        break;
                    case ResourceType.Texture:
                        var texture = (TextureResource)source.Textures[reference.AsTexture].Copy();
                        texture.Sources = RewriteSources(texture.Sources, takenNames, result.Files);
                        remap.Apply(texture);
                        manifest.Textures[texture.TextureResourceId] = texture;
                        break;
                    case ResourceType.Font:
                        var font = (FontResource)source.Fonts[reference.AsFont].Copy();
                        font.Sources = RewriteSources(font.Sources, takenNames, result.Files);
                        remap.Apply(font);
                        manifest.Fonts[font.FontResourceId] = font;
                        break;
                    case ResourceType.Audio:
                        var audio = (AudioResource)source.Audios[reference.AsAudio].Copy();
                        audio.Sources = RewriteSources(audio.Sources, takenNames, result.Files);
                        remap.Apply(audio);
                        manifest.Audios[audio.AudioResourceId] = audio;
                        break;
                }

                result.Written.Add(remap.Map(reference));
            }

            CopyCredits(sourceMeta, closure, remap, manifest.ResourcesMeta);
            return result;
        }

        // A record the level holds for something exported is copied under the resource's id in the
        // collection, replacing a record the collection already had for that id.
        private static void CopyCredits(LevelMeta sourceMeta, ResourceSet closure, ResourceRemap remap,
            List<ResourceMeta> into)
        {
            if (sourceMeta?.ResourcesMeta == null) return;

            foreach (var record in sourceMeta.ResourcesMeta)
            {
                // A collection carries resources, never a level's cover.
                if (record == null || record.ResourceType.IsTypeAddressed()) continue;

                var reference = ResourceRef.Of(record.ResourceType, record.ResourceId.value, record.ResourceGuid);
                if (!closure.Contains(reference)) continue;

                var moved = remap.Map(reference);
                into.RemoveAll(existing => existing != null
                                           && ResourceRef.Of(existing.ResourceType, existing.ResourceId.value,
                                               existing.ResourceGuid) == moved);

                var copy = record.Copy();
                copy.ResourceId = new TypedResourceId(moved.Id);
                copy.ResourceGuid = moved.Guid;
                into.Add(copy);
            }
        }

        private static List<ResourceKey> RewriteSources(List<ResourceKey> sources, HashSet<string> takenNames,
            List<ExportFile> files)
        {
            var rewritten = new List<ResourceKey>();
            if (sources == null) return rewritten;

            foreach (var source in sources)
            {
                if (source == null) continue;
                if (source.UriType != ResourceUriType.LevelPath && source.UriType != ResourceUriType.AbsolutePath)
                {
                    rewritten.Add(source.Copy());
                    continue;
                }

                var name = ResourceNaming.UniqueFileName(Path.GetFileName(source.Uri), takenNames);
                var path = FileNames.MediaDirectory + ContentPath.Separator + name;
                files.Add(new ExportFile(source.Copy(), path));
                rewritten.Add(new ResourceKey(ResourceUriType.LevelPath, path));
            }

            return rewritten;
        }

        private static HashSet<int> Ints<T>(IEnumerable<T> ids, Func<T, int> value)
        {
            var set = new HashSet<int>();
            foreach (var id in ids) set.Add(value(id));
            return set;
        }
    }
}

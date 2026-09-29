using System;
using System.Collections.Generic;
using System.IO;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Resources;
using BH.SDK.Utils;

namespace BH.SDK.Services.Collections
{
    /// <summary> What importing one data resource would do to the level. </summary>
    public enum ImportEntryState
    {
        /// <summary> The level has no resource with this id - it is added. </summary>
        New = 0,

        /// <summary> The level already has this exact resource - nothing to add. </summary>
        Identical = 1,

        /// <summary> The level has a DIFFERENT resource under this id - the author decides. </summary>
        Conflict = 2,
    }

    /// <summary> The author's answer to one conflict. </summary>
    public enum ConflictAnswer
    {
        /// <summary> Keep what the level has; the import points at it. </summary>
        KeepLevels = 0,

        /// <summary> Add the incoming one under a fresh id; the import points at the copy. </summary>
        ImportAsCopy = 1,
    }

    /// <summary> One data resource of an import and what it would do. </summary>
    public readonly struct ImportEntry
    {
        /// <summary> Which resource, by its id in the collection. </summary>
        public readonly ResourceRef Ref;

        /// <summary> New, identical or conflicting. </summary>
        public readonly ImportEntryState State;

        /// <summary> Built from its parts. </summary>
        public ImportEntry(ResourceRef reference, ImportEntryState state)
        {
            Ref = reference;
            State = state;
        }
    }

    /// <summary> One media file to copy: from the collection's store into the level's folder. </summary>
    public readonly struct ImportFile
    {
        /// <summary> Store path inside the collection. </summary>
        public readonly string SourcePath;

        /// <summary> File name inside the level folder - free by construction. </summary>
        public readonly string DestinationFileName;

        /// <summary> Built from its parts. </summary>
        public ImportFile(string sourcePath, string destinationFileName)
        {
            SourcePath = sourcePath;
            DestinationFileName = destinationFileName;
        }
    }

    /// <summary> What to write into the level: the resources to add, the files to copy, the credits
    /// to record. Every model here is a copy - the collection is never modified. </summary>
    public sealed class CollectionImportResult
    {
        /// <summary> The resources to add, already renumbered and pointing at their final ids. </summary>
        public LevelResources Added { get; } = new();

        /// <summary> Media to copy into the level folder before the file resources can load. </summary>
        public List<ImportFile> Files { get; } = new();

        /// <summary> Credit records to append to the level's metadata. </summary>
        public List<ResourceMeta> Meta { get; } = new();

        /// <summary> Where each imported id ended up - a moved file resource's fresh id, a copy's fresh
        /// guid, or itself. What a caller assigning the import in the same step points at. </summary>
        public ResourceRemap Remap { get; internal set; } = new();

        /// <summary> How many resources, across every family. </summary>
        public int Count => Added.Textures.Count + Added.Fonts.Count + Added.Audios.Count
                            + Added.CompositeShapes.Count + Added.Themes.Count + Added.Effects.Count
                            + Added.Prefabs.Count;
    }

    // IMPORT IS PLANNED FIRST AND RESOLVED SECOND, because between the two sits a person. The plan
    // answers everything that has one answer - what the selection needs (ResourceClosure), which data
    // resources the level already holds identically, which file resources need a fresh id and a free
    // file name - and lists what does not: a data resource whose id the level already uses for
    // something different. The editor shows that list, the author answers each, and Resolve builds
    // the copies. Nothing here touches a disk or the level itself.
    //
    // "IDENTICAL" IS THE GENERATED Equals, not a byte comparison of two serializations: the model
    // contract already defines value equality member by member, and a second definition in terms of
    // serializer output would disagree with it the first time a default is written differently.
    //
    // A FILE RESOURCE IS IDENTIFIED BY ITS BYTES. Its id is local to where it lives, so the
    // collection's -1 says nothing about the level's -1, and no identity survives the crossing to
    // compare against. What does survive is the file: when the host hands in both sides'
    // MediaFingerprints and the level already holds a file resource of the same kind with the same
    // digest, the import points at THAT one - no id, no file, no copy of a 3 MB track under a "_1"
    // name. Without fingerprints (no store on either side) every file resource is new, which is only
    // wasteful, never wrong.

    /// <summary> Plans and resolves bringing part of a collection into a level. </summary>
    public sealed class CollectionImportPlanner
    {
        private readonly CollectionContent _source;
        private readonly LevelMeta _targetMeta;
        private readonly ResourceSet _closure;
        private readonly List<ImportEntry> _entries = new();
        private readonly Dictionary<ResourceRef, ResourceRef> _externalMoves = new();
        private readonly List<ImportFile> _files = new();
        private readonly Dictionary<ResourceRef, List<ResourceKey>> _externalSources = new();
        private readonly HashSet<ResourceRef> _reused = new();

        /// <summary> Every data resource of the closure and what importing it would do. </summary>
        public IReadOnlyList<ImportEntry> Entries => _entries;

        /// <summary> Everything the selection reaches inside the collection, selection included. </summary>
        public ResourceSet Closure => _closure;

        /// <summary> How many resources come along that were not selected. </summary>
        public int DependencyCount { get; }

        /// <summary> File resources the level already holds byte for byte: the import points at the
        /// level's own one and copies nothing. Keyed by the id in the collection. </summary>
        public IReadOnlyCollection<ResourceRef> Reused => _reused;

        /// <summary> Whether any entry needs the author's answer. </summary>
        public bool HasConflicts => _entries.Exists(entry => entry.State == ImportEntryState.Conflict);

        private CollectionImportPlanner(CollectionContent source, LevelMeta targetMeta, ResourceSet closure,
            int selected)
        {
            _source = source;
            _targetMeta = targetMeta;
            _closure = closure;
            DependencyCount = Math.Max(0, closure.Count - selected);
        }

        /// <summary> Plans importing <paramref name="selection"/> (and everything it needs) from
        /// <paramref name="source"/> into a level holding <paramref name="target"/>, whose folder already
        /// holds <paramref name="takenFileNames"/>. With both sides' <see cref="MediaFingerprints"/> a file
        /// resource the level already holds is reused rather than copied. </summary>
        public static CollectionImportPlanner Plan(LevelResources target, LevelMeta targetMeta,
            CollectionContent source, IEnumerable<ResourceRef> selection, IEnumerable<string> takenFileNames,
            IReadOnlyDictionary<ResourceRef, string> sourceFingerprints = null,
            IReadOnlyDictionary<ResourceRef, string> targetFingerprints = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));

            var roots = new ResourceSet();
            if (selection != null)
                foreach (var reference in selection)
                    roots.Add(reference);

            var closure = ResourceClosure.Collect(source.Resources, roots);
            var plan = new CollectionImportPlanner(source, targetMeta, closure, CountOwned(source, roots));

            var takenNames = new HashSet<string>(takenFileNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var takenIds = new Dictionary<ResourceType, HashSet<int>>
            {
                [ResourceType.Texture] = Ints(target.Textures.Keys, id => id.value),
                [ResourceType.Font] = Ints(target.Fonts.Keys, id => id.value),
                [ResourceType.Audio] = Ints(target.Audios.Keys, id => id.value),
            };

            var existing = ByDigest(targetFingerprints);

            foreach (var reference in closure)
            {
                if (reference.Type.IsGuidAddressed())
                {
                    plan._entries.Add(new ImportEntry(reference, StateOf(target, source.Resources, reference)));
                    continue;
                }

                if (sourceFingerprints != null && sourceFingerprints.TryGetValue(reference, out var digest)
                    && existing.TryGetValue((reference.Type, digest.ToLowerInvariant()), out var same))
                {
                    plan._externalMoves[reference] = same;
                    plan._reused.Add(reference);
                    continue;
                }

                var moved = ResourceRef.Of(reference.Type, ResourceNaming.NextUserId(takenIds[reference.Type]),
                    Guid.Empty);
                plan._externalMoves[reference] = moved;
                plan._externalSources[reference] = plan.RewriteSources(SourcesOf(source.Resources, reference),
                    takenNames);
            }

            return plan;
        }

        /// <summary> Builds the copies to add. A conflict with no answer keeps the level's. </summary>
        public CollectionImportResult Resolve(IReadOnlyDictionary<ResourceRef, ConflictAnswer> answers = null)
        {
            var remap = new ResourceRemap();
            foreach (var move in _externalMoves)
                remap.Add(move.Key, move.Value);

            var added = new HashSet<ResourceRef>();
            foreach (var entry in _entries)
            {
                if (entry.State == ImportEntryState.New)
                {
                    added.Add(entry.Ref);
                    continue;
                }

                if (entry.State != ImportEntryState.Conflict) continue;

                ConflictAnswer answer = ConflictAnswer.KeepLevels;
                if (answers != null) answers.TryGetValue(entry.Ref, out answer);
                if (answer != ConflictAnswer.ImportAsCopy) continue;

                remap.Add(entry.Ref, ResourceRef.Of(entry.Ref.Type, 0, Guid.NewGuid()));
                added.Add(entry.Ref);
            }

            foreach (var reference in _externalMoves.Keys)
                if (!_reused.Contains(reference))
                    added.Add(reference);

            var result = new CollectionImportResult { Remap = remap };
            var resources = _source.Resources;

            foreach (var reference in added)
            {
                switch (reference.Type)
                {
                    case ResourceType.Prefab:
                        var prefab = resources.Prefabs[reference.AsPrefab].Copy();
                        remap.Apply(prefab);
                        result.Added.Prefabs[prefab.PrefabId] = prefab;
                        break;
                    case ResourceType.Effect:
                        var effect = resources.Effects[reference.AsEffect].Copy();
                        remap.Apply(effect);
                        result.Added.Effects[effect.EffectId] = effect;
                        break;
                    case ResourceType.Theme:
                        var theme = resources.Themes[reference.AsTheme].Copy();
                        remap.Apply(theme);
                        result.Added.Themes[theme.ThemeId] = theme;
                        break;
                    case ResourceType.Shape:
                        var shape = resources.CompositeShapes[reference.AsShape].Copy();
                        remap.Apply(shape);
                        result.Added.CompositeShapes[shape.ShapeId] = shape;
                        break;
                    case ResourceType.Texture:
                        var texture = (TextureResource)resources.Textures[reference.AsTexture].Copy();
                        texture.Sources = _externalSources[reference];
                        remap.Apply(texture);
                        result.Added.Textures[texture.TextureResourceId] = texture;
                        break;
                    case ResourceType.Font:
                        var font = (FontResource)resources.Fonts[reference.AsFont].Copy();
                        font.Sources = _externalSources[reference];
                        remap.Apply(font);
                        result.Added.Fonts[font.FontResourceId] = font;
                        break;
                    case ResourceType.Audio:
                        var audio = (AudioResource)resources.Audios[reference.AsAudio].Copy();
                        audio.Sources = _externalSources[reference];
                        remap.Apply(audio);
                        result.Added.Audios[audio.AudioResourceId] = audio;
                        break;
                }
            }

            result.Files.AddRange(_files);

            // A reused file brings its credit along when the level has none for it yet.
            var credited = new HashSet<ResourceRef>(added);
            credited.UnionWith(_reused);
            AddCredits(result, credited, remap);
            return result;
        }

        // A credit follows its resource: a record the collection holds for something that is added is
        // copied under the resource's final id, unless the level already credits that very id.
        private void AddCredits(CollectionImportResult result, HashSet<ResourceRef> added, ResourceRemap remap)
        {
            var existing = new HashSet<ResourceRef>();
            if (_targetMeta?.ResourcesMeta != null)
                foreach (var record in _targetMeta.ResourcesMeta)
                    if (record != null)
                        existing.Add(ResourceRef.Of(record.ResourceType, record.ResourceId.value, record.ResourceGuid));

            foreach (var record in _source.Manifest.ResourcesMeta)
            {
                // A collection carries resources, never a level's cover.
                if (record == null || record.ResourceType.IsTypeAddressed()) continue;

                var reference = ResourceRef.Of(record.ResourceType, record.ResourceId.value, record.ResourceGuid);
                if (!added.Contains(reference)) continue;

                var moved = remap.Map(reference);
                if (existing.Contains(moved)) continue;

                var copy = record.Copy();
                copy.ResourceId = new Models.Primitives.Resources.TypedResourceId(moved.Id);
                copy.ResourceGuid = moved.Guid;
                result.Meta.Add(copy);
            }
        }

        // A LevelPath source is a file inside the collection, and becomes a file inside the level under
        // a free name; any other source (a URL, an absolute path) is carried as it is.
        private List<ResourceKey> RewriteSources(List<ResourceKey> sources, HashSet<string> takenNames)
        {
            var rewritten = new List<ResourceKey>();
            if (sources == null) return rewritten;

            foreach (var source in sources)
            {
                if (source == null) continue;
                if (source.UriType != ResourceUriType.LevelPath)
                {
                    rewritten.Add(source.Copy());
                    continue;
                }

                var name = ResourceNaming.UniqueFileName(Path.GetFileName(source.Uri), takenNames);
                _files.Add(new ImportFile(source.Uri, name));
                rewritten.Add(new ResourceKey(ResourceUriType.LevelPath, name));
            }

            return rewritten;
        }

        // One level resource per (kind, digest); the first wins when the level itself holds duplicates.
        private static Dictionary<(ResourceType, string), ResourceRef> ByDigest(
            IReadOnlyDictionary<ResourceRef, string> fingerprints)
        {
            var result = new Dictionary<(ResourceType, string), ResourceRef>();
            if (fingerprints == null) return result;

            foreach (var pair in fingerprints)
            {
                if (string.IsNullOrEmpty(pair.Value)) continue;
                var key = (pair.Key.Type, pair.Value.ToLowerInvariant());
                if (!result.ContainsKey(key)) result.Add(key, pair.Key);
            }

            return result;
        }

        private static ImportEntryState StateOf(LevelResources target, LevelResources source, ResourceRef reference)
        {
            switch (reference.Type)
            {
                case ResourceType.Prefab:
                    return Compare(target.Prefabs, source.Prefabs, reference.AsPrefab);
                case ResourceType.Effect:
                    return Compare(target.Effects, source.Effects, reference.AsEffect);
                case ResourceType.Theme:
                    return Compare(target.Themes, source.Themes, reference.AsTheme);
                case ResourceType.Shape:
                    return Compare(target.CompositeShapes, source.CompositeShapes, reference.AsShape);
                default:
                    return ImportEntryState.New;
            }
        }

        private static ImportEntryState Compare<TId, TModel>(Dictionary<TId, TModel> target,
            Dictionary<TId, TModel> source, TId id)
        {
            if (!target.TryGetValue(id, out var existing)) return ImportEntryState.New;
            return Equals(existing, source[id]) ? ImportEntryState.Identical : ImportEntryState.Conflict;
        }

        private static List<ResourceKey> SourcesOf(LevelResources resources, ResourceRef reference)
        {
            switch (reference.Type)
            {
                case ResourceType.Texture: return resources.Textures[reference.AsTexture].Sources;
                case ResourceType.Font: return resources.Fonts[reference.AsFont].Sources;
                case ResourceType.Audio: return resources.Audios[reference.AsAudio].Sources;
                default: return null;
            }
        }

        private static int CountOwned(CollectionContent source, ResourceSet roots)
        {
            var count = 0;
            foreach (var reference in roots)
                if (ResourceClosure.Owns(source.Resources, reference))
                    count++;
            return count;
        }

        private static HashSet<int> Ints<T>(IEnumerable<T> ids, Func<T, int> value)
        {
            var set = new HashSet<int>();
            foreach (var id in ids) set.Add(value(id));
            return set;
        }
    }
}

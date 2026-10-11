using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Agent;
using BH.SDK.Interop;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.LevelArchive
{
    // What goes in a package is computed from the MODEL, never from a directory listing. Collecting is
    // not optional: an AbsolutePath song is copied in and rekeyed to LevelPath in the exported copy
    // only; a URL is left as is. The ONE place that opens an absolute path. Docs/ARCHIVE.md.

    /// <summary> Decides what a level archive will contain. </summary>
    public static class LevelArchiveBuilder
    {
        private const string CodeMissingFile = "archive.resource_missing";
        private const string CodeCollected = "archive.resource_collected";
        private const string CodeUnreachable = "archive.resource_unreachable";
        private const string CodeBadPath = "archive.resource_bad_path";
        private const string CodeRenamed = "archive.resource_renamed";
        private const string CodeDropped = "archive.unreferenced_dropped";
        private const string CodeAiWithheld = "archive.ai_context_withheld";

        /// <summary> The plan of an unprotected export - a plain folder or a Workshop upload. </summary>
        public static Task<LevelArchivePlan> BuildAsync(Level level, LevelMeta meta,
            IContentStore levelStore, CancellationToken token = default) =>
            BuildAsync(level, meta, levelStore, LevelExportMode.Folder, token);

        /// <summary> Walks the level for everything it references and decides what the archive
        /// carries, what is collected into it, and what cannot travel. A protected mode never carries
        /// the level's <c>ai/</c> folder. </summary>
        public static async Task<LevelArchivePlan> BuildAsync(Level level, LevelMeta meta,
            IContentStore levelStore, LevelExportMode mode, CancellationToken token = default)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (levelStore == null) throw new ArgumentNullException(nameof(levelStore));

            var report = new InteropReport();

            // Copies from the first line, so nothing below can reach the caller's live model even by
            // accident - the rewriting further down is exactly what must not escape.
            var levelCopy = level.Copy();
            var metaCopy = meta.Copy();

            var context = new WalkContext(levelStore, report, token);

            // The logo first, then resources by id: a deterministic walk is what makes the collected
            // names deterministic, and those are part of the bytes a reproducible pack produces.
            await context.RouteAsync(metaCopy.LevelLogo, "metadata.logo");
            await RouteResourcesAsync(context, levelCopy.Resources);

            var aiWithheld = await RouteAiContextAsync(context, levelStore, mode, report, token);

            var dropped = await CountUnreferencedAsync(levelStore, context, aiWithheld, token);
            if (dropped > 0)
                report.Info(CodeDropped,
                    $"{dropped} file(s) in the level folder are referenced by nothing and were not packed.");

            var files = new List<ArchiveFile>(context.Files);
            files.Sort((left, right) => string.CompareOrdinal(left.ArchivePath, right.ArchivePath));

            return new LevelArchivePlan(levelCopy, metaCopy, files, report, dropped);
        }

        private static async Task RouteResourcesAsync(WalkContext context, LevelResources resources)
        {
            if (resources == null) return;

            foreach (var id in Sorted(resources.Textures.Keys, key => key.value))
                await RouteResourceAsync(context, resources.Textures[id], "texture", id.value);

            foreach (var id in Sorted(resources.Fonts.Keys, key => key.value))
                await RouteResourceAsync(context, resources.Fonts[id], "font", id.value);

            foreach (var id in Sorted(resources.Audios.Keys, key => key.value))
                await RouteResourceAsync(context, resources.Audios[id], "audio", id.value);
        }

        private static async Task RouteResourceAsync(WalkContext context, Resource resource,
            string kind, int id)
        {
            if (resource?.Sources == null) return;

            // Every source is routed, not just the first: a resource lists them as FALLBACKS for one
            // and the same asset, so packing only the first would ship a level whose backup copy is
            // a path that no longer exists on the machine it arrives at.
            for (var i = 0; i < resource.Sources.Count; i++)
                await context.RouteAsync(resource.Sources[i], $"{kind}[{id}].src[{i}]");
        }

        private static List<TKey> Sorted<TKey>(IEnumerable<TKey> keys, Func<TKey, int> order)
        {
            var sorted = new List<TKey>(keys);
            sorted.Sort((left, right) => order(left).CompareTo(order(right)));
            return sorted;
        }

        // Packed only with a manifest that shares it, and never in a protected mode (whose point is that
        // nothing readable travels). No manifest leaves ai/ to the unreferenced count. Docs/ARCHIVE.md.
        private static async Task<bool> RouteAiContextAsync(WalkContext context, IContentStore store,
            LevelExportMode mode, InteropReport report, CancellationToken token)
        {
            var manifestPath = AiFolderRules.StorePath(FileNames.AiManifestFile);
            if (!await store.ExistsAsync(manifestPath, token)) return false;

            string reason = null;
            if (mode.IsProtected())
            {
                reason = "a protected export never carries it";
            }
            else
            {
                var manifest = await ReadManifestAsync(store, manifestPath, token);
                if (manifest == null) reason = "its manifest could not be read";
                else if (!manifest.Share) reason = "the author chose not to share it";
            }

            if (reason != null)
            {
                report.Info(CodeAiWithheld, $"The level's AI context was not packed: {reason}.",
                    FileNames.AiDirectory);
                return true;
            }

            var sizes = new List<KeyValuePair<string, long>>();
            foreach (var path in await store.ListAsync(FileNames.AiDirectory, token))
            {
                var name = AiFolderRules.NameOf(path);
                if (name == null) continue;

                var length = await store.GetLengthAsync(path, token);
                sizes.Add(new KeyValuePair<string, long>(name, length));
            }

            foreach (var name in AiFolderRules.SelectShareable(sizes))
                context.PackStoreFile(AiFolderRules.StorePath(name));

            return false;
        }

        private static async Task<AiFolderManifest> ReadManifestAsync(IContentStore store, string path,
            CancellationToken token)
        {
            if (await store.GetLengthAsync(path, token) > AgentRules.MaxContextFileBytes) return null;

            using var stream = await store.OpenReadAsync(path, token);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();
            return AiFolderManifest.TryParse(text, out var manifest, out _) ? manifest : null;
        }

        // The regenerated documents, and those a DIFFERENT format left behind, are not "unreferenced";
        // neither is a withheld ai/ folder, which is reported on its own.
        private static async Task<int> CountUnreferencedAsync(IContentStore store, WalkContext context,
            bool aiWithheld, CancellationToken token)
        {
            var listing = await store.ListAsync(string.Empty, token);

            var dropped = 0;
            foreach (var path in listing)
            {
                if (context.IsPackedFromStore(path)) continue;
                if (IsDocument(path)) continue;
                if (aiWithheld && ContentPath.HasPrefix(path, FileNames.AiDirectory)) continue;

                dropped++;
            }

            return dropped;
        }

        private static bool IsDocument(string path)
        {
            if (path.EndsWith(FileNames.EncryptedExtension, StringComparison.Ordinal))
                path = path.Substring(0, path.Length - FileNames.EncryptedExtension.Length);

            if (path.IndexOf(ContentPath.Separator) >= 0) return false;

            var stem = Path.GetFileNameWithoutExtension(path);
            return string.Equals(stem, FileNames.LevelFileBaseName, StringComparison.Ordinal)
                   || string.Equals(stem, FileNames.MetadataFileBaseName, StringComparison.Ordinal);
        }

        // Routing state shared across every key: which SOURCES are taken (packed once) and which NAMES
        // (two different song.ogg must not merge).

        /// <summary> What one composition walk accumulates: the entries found, and the rewrites a collected
        /// out-of-folder resource needs. </summary>
        private sealed class WalkContext
        {
            private readonly IContentStore _store;
            private readonly InteropReport _report;
            private readonly CancellationToken _token;

            private readonly Dictionary<string, string> _archivePathBySource =
                new Dictionary<string, string>(StringComparer.Ordinal);

            private readonly HashSet<string> _takenNames = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _packedStorePaths = new HashSet<string>(StringComparer.Ordinal);

            /// <summary> Built from its store, report and token. </summary>
            public WalkContext(IContentStore store, InteropReport report, CancellationToken token)
            {
                _store = store;
                _report = report;
                _token = token;
            }

            /// <summary> What the archive will hold, in the order it was collected. </summary>
            public List<ArchiveFile> Files { get; } = new List<ArchiveFile>();

            /// <summary> Whether this path in the level's own folder is already being packed. </summary>
            public bool IsPackedFromStore(string storePath) => _packedStorePaths.Contains(storePath);

            /// <summary> Decides what happens to one resource reference: packed, collected in, left as a URL, or reported missing. </summary>
            public async Task RouteAsync(ResourceKey key, string path)
            {
                if (key == null || string.IsNullOrEmpty(key.Uri)) return;

                _token.ThrowIfCancellationRequested();

                switch (key.UriType)
                {
                    case ResourceUriType.LevelPath:
                        await PackAsync(key, path);
                        return;

                    case ResourceUriType.AbsolutePath:
                        Collect(key, path);
                        return;

                    case ResourceUriType.DirectUrl:
                    case ResourceUriType.StreamingAssets:
                        _report.Deferred(CodeUnreachable,
                            $"A {key.UriType} resource cannot travel inside an archive and was left " +
                            "pointing where it points now.", path);
                        return;

                    default:
                        _report.Dropped(CodeBadPath,
                            $"'{key.Uri}' has no usable location type and was left alone.", path);
                        return;
                }
            }

            private async Task PackAsync(ResourceKey key, string path)
            {
                var storePath = key.Uri;

                if (!ContentPath.TryValidate(storePath, out var error))
                {
                    _report.Dropped(CodeBadPath,
                        $"'{storePath}' is not a usable name inside an archive: {error}.", path);
                    return;
                }

                if (!await _store.ExistsAsync(storePath, _token))
                {
                    _report.Dropped(CodeMissingFile,
                        $"'{storePath}' is referenced by the level but is not in its folder.", path);
                    return;
                }

                var archivePath = Take(SourceId(storePath, external: false),
                    () => new ArchiveFile(Reserve(storePath, path), storePath, isExternal: false));

                _packedStorePaths.Add(storePath);
                key.Uri = archivePath;
            }

            /// <summary> Packs a file of the level's own folder that no resource names, under its own path.
            /// </summary>
            public void PackStoreFile(string storePath)
            {
                Take(SourceId(storePath, external: false),
                    () => new ArchiveFile(Reserve(storePath, storePath), storePath, isExternal: false));

                _packedStorePaths.Add(storePath);
            }

            private void Collect(ResourceKey key, string path)
            {
                var absolutePath = key.Uri;

                if (!File.Exists(absolutePath))
                {
                    _report.Dropped(CodeMissingFile,
                        $"'{absolutePath}' lives outside the level folder and is not there any more.", path);
                    return;
                }

                var archivePath = Take(SourceId(absolutePath, external: true), () =>
                {
                    var name = Reserve(Path.GetFileName(absolutePath), path);
                    _report.Info(CodeCollected,
                        $"'{absolutePath}' was copied into the archive as '{name}'.", path);
                    return new ArchiveFile(name, absolutePath, isExternal: true);
                });

                key.UriType = ResourceUriType.LevelPath;
                key.Uri = archivePath;
            }

            // One source is packed once. The second reference to it does not add a file and does not
            // take a second name - it is repointed at the copy that is already going in, which is
            // what makes a resource's fallback list cost nothing when its entries agree.
            private string Take(string sourceId, Func<ArchiveFile> create)
            {
                if (_archivePathBySource.TryGetValue(sourceId, out var existing)) return existing;

                var file = create();
                _archivePathBySource.Add(sourceId, file.ArchivePath);
                Files.Add(file);
                return file.ArchivePath;
            }

            // A Windows path is one file however it is spelled, so two collected keys differing only
            // in case are the same source. A store path is compared exactly - see ContentPath.
            private static string SourceId(string source, bool external) =>
                external ? "abs:" + source.ToLowerInvariant() : "store:" + source;

            // Legal store path AND within the tar header, enforced HERE so a folder export and an archive
            // export of one level carry identical names.
            private string Reserve(string preferred, string path)
            {
                var candidate = MakeUnique(Sanitize(preferred));

                _takenNames.Add(candidate);
                if (!string.Equals(candidate, preferred, StringComparison.Ordinal))
                    _report.Approximated(CodeRenamed,
                        $"'{preferred}' was renamed to '{candidate}' inside the archive.", path);

                return candidate;
            }

            private string MakeUnique(string candidate)
            {
                if (!_takenNames.Contains(candidate)) return candidate;

                var directory = DirectoryOf(candidate);
                var stem = Path.GetFileNameWithoutExtension(candidate);
                var extension = Path.GetExtension(candidate);

                for (var counter = 1; counter < int.MaxValue; counter++)
                {
                    var next = Truncate($"{directory}{stem}_{counter}{extension}");
                    if (!_takenNames.Contains(next)) return next;
                }

                // Unreachable in practice - a level would need two billion files of one name.
                throw new InvalidOperationException($"Cannot find a free archive name for '{candidate}'.");
            }

            private static string DirectoryOf(string path)
            {
                var slash = path.LastIndexOf(ContentPath.Separator);
                return slash < 0 ? string.Empty : path.Substring(0, slash + 1);
            }

            private static string Sanitize(string preferred)
            {
                if (string.IsNullOrEmpty(preferred)) return "file";

                var cleaned = preferred.Replace('\\', ContentPath.Separator);
                while (cleaned.Length > 0 && cleaned[0] == ContentPath.Separator)
                    cleaned = cleaned.Substring(1);

                if (!ContentPath.IsValid(cleaned)) cleaned = Path.GetFileName(cleaned);
                if (!ContentPath.IsValid(cleaned)) cleaned = "file";

                return Truncate(cleaned);
            }

            // Truncation keeps the EXTENSION (readers decide the kind by it); a path whose folders alone
            // overflow loses folders instead.
            private static string Truncate(string path)
            {
                if (ArchivePolicy.FitsName(path)) return path;

                var shortened = Shorten(DirectoryOf(path), path);
                return ArchivePolicy.FitsName(shortened) ? shortened : Shorten(string.Empty, path);
            }

            private static string Shorten(string directory, string path)
            {
                var extension = Path.GetExtension(path);
                var stem = Path.GetFileNameWithoutExtension(path);

                while (stem.Length > 1 && !ArchivePolicy.FitsName($"{directory}{stem}{extension}"))
                    stem = stem.Substring(0, stem.Length - 1);

                return $"{directory}{stem}{extension}";
            }
        }
    }
}
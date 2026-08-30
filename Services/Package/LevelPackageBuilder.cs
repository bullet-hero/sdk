using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Interop;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Package
{
    // WHAT GOES INTO A PACKAGE IS COMPUTED FROM THE MODEL, NEVER FROM A DIRECTORY LISTING. A level
    // folder accumulates: a texture the author swapped out, an export somebody unzipped into it, a
    // .bak from another tool. Packing the folder as it stands ships all of it, and packing only what
    // the level references is the only definition of "this level" that survives someone else's
    // habits. The listing is still read - to say how many files were left behind.
    //
    // COLLECTING IS NOT OPTIONAL, and the reason is one line of the editor: EditorCreateLevelView
    // sets UriType = AbsolutePath for the song a level is created around, so the single most common
    // way to start a level produces one whose music lives outside its folder. An export that packed
    // the folder as-is would ship those levels silently broken - the archive opens, the level loads,
    // and there is no sound. So an absolute path is COPIED in and its key rewritten to LevelPath,
    // in the exported copy alone.
    //
    // A URL IS LEFT EXACTLY AS IT IS. It cannot travel inside a package and it is not broken either
    // - it will resolve on the other machine as well as it does on this one, provided that machine
    // is online. Reporting it is the whole of what can be done about it.
    //
    // THIS IS THE ONE PLACE IN THE SDK THAT OPENS AN ABSOLUTE PATH. Everything else addresses a
    // rooted store, deliberately; collecting is the operation that by definition reaches outside
    // one, on a path the author's own level supplied, on the author's own machine.

    /// <summary> Decides what a level package will contain. </summary>
    public static class LevelPackageBuilder
    {
        private const string CodeMissingFile = "package.resource_missing";
        private const string CodeCollected = "package.resource_collected";
        private const string CodeUnreachable = "package.resource_unreachable";
        private const string CodeBadPath = "package.resource_bad_path";
        private const string CodeRenamed = "package.resource_renamed";
        private const string CodeDropped = "package.unreferenced_dropped";

        /// <summary> Walks the level for everything it references and decides what the package
        /// carries, what is collected into it, and what cannot travel. </summary>
        public static async Task<LevelPackagePlan> BuildAsync(Level level, LevelMeta meta,
            IContentStore levelStore, CancellationToken token = default)
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

            var dropped = await CountUnreferencedAsync(levelStore, context, token);
            if (dropped > 0)
                report.Info(CodeDropped,
                    $"{dropped} file(s) in the level folder are referenced by nothing and were not packed.");

            var files = new List<PackageFile>(context.Files);
            files.Sort((left, right) => string.CompareOrdinal(left.PackagePath, right.PackagePath));

            return new LevelPackagePlan(levelCopy, metaCopy, files, report, dropped);
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

        // The documents are regenerated by the writer rather than copied, so the files they will be
        // written as must not be counted as "unreferenced" - and neither must the ones a DIFFERENT
        // format left behind, or converting a level to Bson would make its old level.json look like
        // somebody's stray file forever.
        private static async Task<int> CountUnreferencedAsync(IContentStore store, WalkContext context,
            CancellationToken token)
        {
            var listing = await store.ListAsync(string.Empty, token);

            var dropped = 0;
            foreach (var path in listing)
            {
                if (context.IsPackedFromStore(path)) continue;
                if (IsDocument(path)) continue;

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

        // Routing state, kept together because two things have to be shared across every key in the
        // level: which SOURCES have already been taken (so one file referenced twice is packed once
        // and both keys point at the same copy) and which NAMES are taken (so two different songs
        // both called "song.ogg" do not become one).
        private sealed class WalkContext
        {
            private readonly IContentStore _store;
            private readonly InteropReport _report;
            private readonly CancellationToken _token;

            private readonly Dictionary<string, string> _packagePathBySource =
                new Dictionary<string, string>(StringComparer.Ordinal);

            private readonly HashSet<string> _takenNames = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _packedStorePaths = new HashSet<string>(StringComparer.Ordinal);

            public WalkContext(IContentStore store, InteropReport report, CancellationToken token)
            {
                _store = store;
                _report = report;
                _token = token;
            }

            public List<PackageFile> Files { get; } = new List<PackageFile>();

            /// <summary> Whether this path in the level's own folder is already being packed. </summary>
            public bool IsPackedFromStore(string storePath) => _packedStorePaths.Contains(storePath);

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
                            $"A {key.UriType} resource cannot travel inside a package and was left " +
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
                        $"'{storePath}' is not a usable name inside a package: {error}.", path);
                    return;
                }

                if (!await _store.ExistsAsync(storePath, _token))
                {
                    _report.Dropped(CodeMissingFile,
                        $"'{storePath}' is referenced by the level but is not in its folder.", path);
                    return;
                }

                var packagePath = Take(SourceId(storePath, external: false),
                    () => new PackageFile(Reserve(storePath, path), storePath, isExternal: false));

                _packedStorePaths.Add(storePath);
                key.Uri = packagePath;
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

                var packagePath = Take(SourceId(absolutePath, external: true), () =>
                {
                    var name = Reserve(Path.GetFileName(absolutePath), path);
                    _report.Info(CodeCollected,
                        $"'{absolutePath}' was copied into the package as '{name}'.", path);
                    return new PackageFile(name, absolutePath, isExternal: true);
                });

                key.UriType = ResourceUriType.LevelPath;
                key.Uri = packagePath;
            }

            // One source is packed once. The second reference to it does not add a file and does not
            // take a second name - it is repointed at the copy that is already going in, which is
            // what makes a resource's fallback list cost nothing when its entries agree.
            private string Take(string sourceId, Func<PackageFile> create)
            {
                if (_packagePathBySource.TryGetValue(sourceId, out var existing)) return existing;

                var file = create();
                _packagePathBySource.Add(sourceId, file.PackagePath);
                Files.Add(file);
                return file.PackagePath;
            }

            // A Windows path is one file however it is spelled, so two collected keys differing only
            // in case are the same source. A store path is compared exactly - see ContentPath.
            private static string SourceId(string source, bool external) =>
                external ? "abs:" + source.ToLowerInvariant() : "store:" + source;

            // A name has to survive two things at once: being a legal store path, and fitting the
            // 100 bytes a tar header holds. Both are enforced HERE rather than in the writer, so a
            // folder export and an archive export of the same level carry identical names - two
            // exports that disagree about what a file is called are two different levels.
            private string Reserve(string preferred, string path)
            {
                var candidate = MakeUnique(Sanitize(preferred));

                _takenNames.Add(candidate);
                if (!string.Equals(candidate, preferred, StringComparison.Ordinal))
                    _report.Approximated(CodeRenamed,
                        $"'{preferred}' was renamed to '{candidate}' inside the package.", path);

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
                throw new InvalidOperationException($"Cannot find a free package name for '{candidate}'.");
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

            // Truncation keeps the EXTENSION, because that is what every reader on the other side
            // uses to decide what the file is - a shortened stem is a cosmetic loss, a lost
            // extension is a texture nothing will open. A path whose FOLDERS alone overflow the
            // header loses the folders instead, since there is nothing else left to give.
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
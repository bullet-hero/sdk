using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Services.Content;
using BH.SDK.Utils;

namespace BH.SDK.Services.Collections
{
    // What a file resource IS when its id cannot say: its bytes. The first LevelPath source is the file;
    // a URL or absolute path has none and is never matched. The only IO an import plan needs.

    /// <summary> Content digests of the file resources whose bytes sit in a store. </summary>
    public static class MediaFingerprints
    {
        /// <summary> A "sha256:" digest per texture, font and audio of <paramref name="resources"/> whose
        /// file <paramref name="store"/> holds. Missing files are left out, never an error. </summary>
        public static async Task<Dictionary<ResourceRef, string>> ComputeAsync(LevelResources resources,
            IContentStore store, CancellationToken token = default)
        {
            var result = new Dictionary<ResourceRef, string>();
            if (resources == null || store == null) return result;

            foreach (var pair in resources.Textures)
                await AddAsync(result, ResourceRef.Of(pair.Key), pair.Value?.Sources, store, token);
            foreach (var pair in resources.Fonts)
                await AddAsync(result, ResourceRef.Of(pair.Key), pair.Value?.Sources, store, token);
            foreach (var pair in resources.Audios)
                await AddAsync(result, ResourceRef.Of(pair.Key), pair.Value?.Sources, store, token);

            return result;
        }

        private static async Task AddAsync(Dictionary<ResourceRef, string> result, ResourceRef reference,
            List<ResourceKey> sources, IContentStore store, CancellationToken token)
        {
            var path = FirstLevelPath(sources);
            if (path == null || !ContentPath.IsValid(path)) return;
            if (!await store.ExistsAsync(path, token)) return;

            using var stream = await store.OpenReadAsync(path, token);
            if (stream == null) return;
            result[reference] = ContentHashUtils.Sha256(stream);
        }

        private static string FirstLevelPath(List<ResourceKey> sources)
        {
            if (sources == null) return null;
            foreach (var source in sources)
                if (source != null && source.UriType == ResourceUriType.LevelPath && !string.IsNullOrEmpty(source.Uri))
                    return source.Uri;
            return null;
        }
    }
}

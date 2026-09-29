using System;
using System.Linq;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Publishing
{
    // A FINDING NAMES THE RESOURCE THE AUTHOR KNOWS, never only its address. "meta.resources[Texture:3]"
    // is what a log wants; an author reading a refusal wants the file they dropped in or the name they
    // gave the prefab. The record's own title wins, then what the resource itself is called, and the
    // address is only the last resort.

    /// <summary> What a publish finding calls a resource. </summary>
    internal static class ResourceNames
    {
        /// <summary> The name of the resource a record describes, looked up in
        /// <paramref name="resources"/> when the record's title says nothing. </summary>
        public static string Of(ResourceMeta meta, LevelResources resources)
        {
            var title = Plain(meta.ResourceTitle);
            if (!string.IsNullOrWhiteSpace(title)) return title;

            var id = meta.ResourceId.value;
            var guid = meta.ResourceGuid;
            return Find(meta.ResourceType, id, guid, resources) ?? Fallback(meta.ResourceType, id, guid);
        }

        /// <summary> A file resource's name: the file its first source points at. </summary>
        public static string OfFile(Resource resource, int id)
        {
            var uri = resource?.Sources?.FirstOrDefault(source => source != null && !string.IsNullOrEmpty(source.Uri))?.Uri;
            return uri != null ? FileName(uri) : "#" + id;
        }

        /// <summary> The address, for a resource nothing names. </summary>
        public static string Fallback(ResourceType type, int id, Guid guid)
            => type.IsTypeAddressed() ? type.ToString()
                : type.IsGuidAddressed() ? guid.ToString("D")
                : "#" + id;

        /// <summary> The last segment of a path or a URL. Not Path.GetFileName, which throws on a URL's
        /// characters under some runtimes. </summary>
        public static string FileName(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return string.Empty;
            var cut = Math.Max(uri.LastIndexOf('/'), uri.LastIndexOf('\\'));
            return cut >= 0 && cut < uri.Length - 1 ? uri.Substring(cut + 1) : uri;
        }

        private static string Find(ResourceType type, int id, Guid guid, LevelResources resources)
        {
            if (resources == null) return null;
            switch (type)
            {
                case ResourceType.Texture:
                    return resources.Textures.Where(p => p.Key.value == id).Select(p => OfFile(p.Value, id)).FirstOrDefault();
                case ResourceType.Font:
                    return resources.Fonts.Where(p => p.Key.value == id).Select(p => OfFile(p.Value, id)).FirstOrDefault();
                case ResourceType.Audio:
                    return resources.Audios.Where(p => p.Key.value == id).Select(p => OfFile(p.Value, id)).FirstOrDefault();
                case ResourceType.Theme:
                    return Named(resources.Themes.Where(p => p.Key.value == guid).Select(p => p.Value?.Name).FirstOrDefault());
                case ResourceType.Effect:
                    return Named(resources.Effects.Where(p => p.Key.value == guid).Select(p => p.Value?.Name).FirstOrDefault());
                case ResourceType.Shape:
                    return Named(resources.CompositeShapes.Where(p => p.Key.value == guid).Select(p => p.Value?.ShapeName).FirstOrDefault());
                case ResourceType.Prefab:
                    return Named(resources.Prefabs.Where(p => p.Key.value == guid).Select(p => p.Value?.Root?.Name).FirstOrDefault());
                default:
                    return null;
            }
        }

        private static string Named(string name) => string.IsNullOrWhiteSpace(name) ? null : name;

        private static string Plain(IString value) => value switch
        {
            StringValue plain => plain.Value,
            StringLocalized localized => localized.Strings?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s?.Value))?.Value,
            _ => null,
        };
    }
}

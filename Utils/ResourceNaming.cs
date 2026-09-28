using System;
using System.Collections.Generic;
using System.IO;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;

namespace BH.SDK.Utils
{
    // THE TWO THINGS EVERY MOVE OF A FILE RESOURCE HAS TO INVENT: a fresh level-local id, and a file
    // name nothing in the destination already uses. Both rules were private to the editor's
    // LevelResourcesService; an import planner running in the SDK (and one day on a server) needs the
    // same answers, so they live here and the editor's copy stays a caller of the same arithmetic.

    /// <summary> Fresh ids and free file names for file resources. </summary>
    public static class ResourceNaming
    {
        /// <summary> A file resource id below every id already taken - the next free one in the
        /// user-defined (negative) range. Records it as taken. </summary>
        public static int NextUserId(ISet<int> taken)
        {
            if (taken == null) throw new ArgumentNullException(nameof(taken));

            var min = TypedResourceId.MaxUserDefinedValue + 1; // one above -1, i.e. 0
            foreach (var id in taken)
                if (id < min)
                    min = id;

            var next = min - 1;
            taken.Add(next);
            return next;
        }

        /// <summary> <paramref name="fileName"/>, or <c>name_1.ext</c>, <c>name_2.ext</c>... - the first
        /// one <paramref name="taken"/> does not hold, compared ordinally ignoring case, since a player's
        /// file system may. Records it as taken. </summary>
        public static string UniqueFileName(string fileName, ISet<string> taken)
        {
            if (taken == null) throw new ArgumentNullException(nameof(taken));
            if (string.IsNullOrEmpty(fileName)) fileName = "resource";

            var candidate = fileName;
            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            for (var i = 1; Contains(taken, candidate); i++)
                candidate = $"{name}_{i}{extension}";

            taken.Add(candidate);
            return candidate;
        }

        /// <summary> What to call a resource in a list: a data resource's own name, a file resource's
        /// first file name. Empty when it has neither, or the set does not hold it. </summary>
        public static string DisplayName(LevelResources resources, ResourceRef reference)
        {
            if (resources == null) return string.Empty;

            switch (reference.Type)
            {
                case ResourceType.Prefab:
                    return resources.Prefabs.TryGetValue(reference.AsPrefab, out var prefab) ? prefab.Root?.Name ?? string.Empty : string.Empty;
                case ResourceType.Theme:
                    return resources.Themes.TryGetValue(reference.AsTheme, out var theme) ? theme.Name ?? string.Empty : string.Empty;
                case ResourceType.Shape:
                    return resources.CompositeShapes.TryGetValue(reference.AsShape, out var shape) ? shape.ShapeName ?? string.Empty : string.Empty;
                case ResourceType.Effect:
                    return resources.Effects.TryGetValue(reference.AsEffect, out var effect) ? effect.Name ?? string.Empty : string.Empty;
                case ResourceType.Texture:
                    return resources.Textures.TryGetValue(reference.AsTexture, out var texture) ? FirstFile(texture) : string.Empty;
                case ResourceType.Font:
                    return resources.Fonts.TryGetValue(reference.AsFont, out var font) ? FirstFile(font) : string.Empty;
                case ResourceType.Audio:
                    return resources.Audios.TryGetValue(reference.AsAudio, out var audio) ? FirstFile(audio) : string.Empty;
                default:
                    return string.Empty;
            }
        }

        private static string FirstFile(Resource resource)
        {
            if (resource?.Sources == null) return string.Empty;
            foreach (var source in resource.Sources)
                if (source != null && !string.IsNullOrEmpty(source.Uri))
                    return Path.GetFileName(source.Uri);
            return string.Empty;
        }

        private static bool Contains(ISet<string> taken, string candidate)
        {
            foreach (var existing in taken)
                if (string.Equals(existing, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}

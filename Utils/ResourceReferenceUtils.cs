using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Utils
{
    // What still POINTS at a resource: a COUNT, over every scope including Prefab.Root (a field no
    // dictionary holds), null ids never counted, Modifications left out (Docs/RESOURCE_GRAPH.md).

    /// <summary> How much of a level still points at one of its resources. </summary>
    public static class ResourceReferenceUtils
    {
        /// <summary> Objects drawing with this image, plus effect presets emitting it. </summary>
        public static int CountTextureReferences(Level level, TextureResourceId textureId)
            => textureId.IsValid() ? Count(level, ResourceRef.Of(textureId)) : 0;

        /// <summary> Text objects set in this typeface. </summary>
        public static int CountFontReferences(Level level, FontResourceId fontId)
            => fontId.IsValid() ? Count(level, ResourceRef.Of(fontId)) : 0;

        /// <summary> Audio tracks playing this clip. </summary>
        public static int CountAudioReferences(Level level, AudioResourceId audioId)
            => audioId.IsValid() ? Count(level, ResourceRef.Of(audioId)) : 0;

        // BOTH SLOTS COUNT, and they are two references rather than one: a shape answers what an
        // object is DRAWN as and what it is HIT as, and an object using the same custom shape for
        // both would otherwise report as one.

        /// <summary> Objects drawn as this shape, plus objects colliding as it, plus effect presets
        /// emitting it as their particle mesh. </summary>
        public static int CountShapeReferences(Level level, ShapeId shapeId)
            => shapeId.IsEnabled() ? Count(level, ResourceRef.Of(shapeId)) : 0;

        // A ThemeRef colour stores a slot INDEX into the active theme, so only the theme track names a
        // ThemeId.

        /// <summary> Keyframes on the level's theme track that switch to this palette. </summary>
        public static int CountThemeReferences(Level level, ThemeId themeId)
            => themeId.IsEnabled() ? Count(level, ResourceRef.Of(themeId)) : 0;

        /// <summary> Objects emitting this particle preset. </summary>
        public static int CountEffectReferences(Level level, EffectId effectId)
            => effectId.IsEnabled() ? Count(level, ResourceRef.Of(effectId)) : 0;

        /// <summary> Placements of this template, anywhere in the level - inside other templates
        /// included, which is how a nested prefab is placed at all. </summary>
        public static int CountPrefabReferences(Level level, PrefabId prefabId)
            => prefabId.IsEnabled() ? Count(level, ResourceRef.Of(prefabId)) : 0;

        private static int Count(Level level, ResourceRef target)
        {
            if (level == null) return 0;

            var count = 0;
            ResourceGraph.Walk(level, reference =>
            {
                if (reference == target) count++;
                return reference;
            }, modifications: false);
            return count;
        }
    }
}

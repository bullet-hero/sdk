using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Utils
{
    // WHAT STILL POINTS AT THIS RESOURCE, and until now nothing in the project could answer it. The
    // validator answers the OPPOSITE direction - LevelGraphAnalyzer's UnresolvedReference finds a
    // reference with no resource behind it - which is the state a delete CREATES, discovered on the
    // next load rather than at the press. So removing a texture eight objects draw with was silent,
    // and what it cost was found later, in a level that had since been edited.
    //
    // A COUNT RATHER THAN A LIST, and that is a decision about what the caller can do with it: the
    // question is "is this safe to delete, and how much is at stake", not "which object number 4172
    // is". A list of ids is unreadable in a confirmation and would have to be resolved into names
    // this layer has no business knowing.
    //
    // EVERY SCOPE IS WALKED, not only Level.Game: a template's objects are ordinary authored content
    // that reference the same resource dictionaries, and a texture used by nothing but a prefab is
    // very much still used. Prefab.Root is walked with them and is the one object no dictionary
    // holds - it is a FIELD, which is the same trap PrefabSearchEntries' count and LayerMath's lane
    // sweep each had to be taught.
    //
    // A NULL ID IS NEVER COUNTED. Null is how an object says it draws no texture, collides with no
    // shape and points at no template, so counting it would report every plain object in the level
    // as a reference to whatever the author was about to delete.

    /// <summary> How much of a level still points at one of its resources. </summary>
    public static class ResourceReferenceUtils
    {
        /// <summary> Objects drawing with this image, plus effect presets emitting it. </summary>
        public static int CountTextureReferences(Level level, TextureResourceId textureId)
        {
            if (level == null || !textureId.IsValid()) return 0;

            var count = 0;
            foreach (var obj in EnumerateObjects(level))
                if (obj is ShapeObject shape && shape.TextureResourceId == textureId)
                    count++;

            if (level.Resources?.Effects != null)
            {
                foreach (var effect in level.Resources.Effects.Values)
                    if (effect?.Core != null && effect.Core.TextureResourceId == textureId)
                        count++;
            }

            return count;
        }

        /// <summary> Text objects set in this typeface. </summary>
        public static int CountFontReferences(Level level, FontResourceId fontId)
        {
            if (level == null || !fontId.IsValid()) return 0;

            var count = 0;
            foreach (var obj in EnumerateObjects(level))
                if (obj is TextObject text && text.FontResourceId == fontId)
                    count++;

            return count;
        }

        /// <summary> Audio tracks playing this clip. </summary>
        public static int CountAudioReferences(Level level, AudioResourceId audioId)
        {
            if (level?.Audio?.Tracks == null || !audioId.IsValid()) return 0;

            var count = 0;
            foreach (var track in level.Audio.Tracks.Values)
                if (track != null && track.AudioResourceId == audioId)
                    count++;

            return count;
        }

        // BOTH SLOTS COUNT, and they are two references rather than one: a shape answers what an
        // object is DRAWN as and what it is HIT as, and an object using the same custom shape for
        // both would otherwise report as one.

        /// <summary> Objects drawn as this shape, plus objects colliding as it. </summary>
        public static int CountShapeReferences(Level level, ShapeId shapeId)
        {
            if (level == null || !shapeId.IsEnabled()) return 0;

            var count = 0;
            foreach (var obj in EnumerateObjects(level))
            {
                if (obj is not ShapeObject shape) continue;

                if (shape.ShapeId == shapeId) count++;
                if (shape.ColliderId == shapeId) count++;
            }

            return count;
        }

        // A ThemeRef COLOUR IS NOT A REFERENCE TO ONE THEME. It stores a slot INDEX into whichever
        // theme is active at that frame (see ColorType.ThemeRef), so it survives a theme being
        // deleted exactly as it survives one being added - what it resolves against is the track
        // below, and only the track names a ThemeId.

        /// <summary> Keyframes on the level's theme track that switch to this palette. </summary>
        public static int CountThemeReferences(Level level, ThemeId themeId)
        {
            var themes = level?.Game?.Events?.Themes;
            if (themes == null || !themeId.IsEnabled()) return 0;

            var count = 0;
            for (var i = 0; i < themes.Count; i++)
                if (themes[i] != null && themes[i].ThemeId == themeId)
                    count++;

            return count;
        }

        /// <summary> Objects emitting this particle preset. </summary>
        public static int CountEffectReferences(Level level, EffectId effectId)
        {
            if (level == null || !effectId.IsEnabled()) return 0;

            var count = 0;
            foreach (var obj in EnumerateObjects(level))
                if (obj is EffectObject effect && effect.EffectId == effectId)
                    count++;

            return count;
        }

        /// <summary> Placements of this template, anywhere in the level - inside other templates
        /// included, which is how a nested prefab is placed at all. </summary>
        public static int CountPrefabReferences(Level level, PrefabId prefabId)
        {
            if (level == null || !prefabId.IsEnabled()) return 0;

            var count = 0;
            foreach (var obj in EnumerateObjects(level))
                if (obj is PrefabObject placement && placement.PrefabId == prefabId)
                    count++;

            return count;
        }

        // Every authored object in the file, in no particular order: the level's own, then each
        // template's root and its contents. A template with a null Objects dictionary is legal data
        // in the middle of being built and is skipped rather than throwing.
        private static IEnumerable<RectObject> EnumerateObjects(Level level)
        {
            if (level.Game?.Objects != null)
            {
                foreach (var obj in level.Game.Objects.Values)
                    if (obj != null)
                        yield return obj;
            }

            if (level.Resources?.Prefabs == null) yield break;

            foreach (var prefab in level.Resources.Prefabs.Values)
            {
                if (prefab == null) continue;
                if (prefab.Root != null) yield return prefab.Root;
                if (prefab.Objects == null) continue;

                foreach (var obj in prefab.Objects.Values)
                    if (obj != null)
                        yield return obj;
            }
        }
    }
}

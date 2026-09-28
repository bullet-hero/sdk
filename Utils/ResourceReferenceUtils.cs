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
    // THE WALK ITSELF IS ResourceGraph's, shared with the closure and the remap an import runs, so a
    // reference one of them learns about is one all three see. Modifications are left out here: in a
    // live level they are already applied to the materialized copies, which are counted instead.
    //
    // A NULL ID IS NEVER COUNTED. Null is how an object says it draws no texture, collides with no
    // shape and points at no template, so counting it would report every plain object in the level
    // as a reference to whatever the author was about to delete.

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

        // A ThemeRef COLOUR IS NOT A REFERENCE TO ONE THEME. It stores a slot INDEX into whichever
        // theme is active at that frame (see ColorType.ThemeRef), so it survives a theme being
        // deleted exactly as it survives one being added - what it resolves against is the track
        // below, and only the track names a ThemeId.

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

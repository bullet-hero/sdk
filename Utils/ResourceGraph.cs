using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Utils
{
    // EVERY PLACE THE FORMAT POINTS AT A RESOURCE, WRITTEN ONCE. Three questions walk the same edges -
    // "what still uses this" (ResourceReferenceUtils, the delete confirmation), "what does this need"
    // (ResourceClosure, a collection export or import) and "point these somewhere else" (ResourceRemap,
    // an import into a level whose ids are taken) - and three hand-written walks would drift exactly
    // where it hurts: a field one of them forgot is a dependency left behind, or a reference that
    // survives an import still pointing at the old id. So the walk is one method per owner, and each
    // edge is a read AND a write: the callback returns the reference to store, the walk writes it back
    // only when it changed, and a reader simply returns what it was given.
    //
    // A NULL ID IS NEVER VISITED - it is how an object says it draws no texture - and a game-defined
    // id is visited like any other: whether it names something the level owns is the CALLER's
    // question, answered by looking it up, because an id's value cannot say which tier it is in.
    //
    // MODIFICATIONS ARE OPTIONAL. A placement's id overrides are real references - a closure that
    // skipped them would ship a prefab whose placement swaps in a shape nobody packed - but in a live
    // level they are also already applied to the materialized copies, so a COUNT that walked both
    // would report every overridden reference twice.

    /// <summary> Visits one resource reference and returns the reference to store in its place. </summary>
    public delegate ResourceRef ResourceEdge(ResourceRef reference);

    /// <summary> Every resource reference the format holds, walked once per owner. </summary>
    public static class ResourceGraph
    {
        /// <summary> A level's own content: its objects, its theme track, its audio tracks, and every
        /// effect preset and prefab template it owns. </summary>
        public static void Walk(Level level, ResourceEdge edge, bool modifications)
        {
            if (level == null) return;

            if (level.Game?.Objects != null)
                foreach (var obj in level.Game.Objects.Values)
                    Walk(obj, edge, modifications);

            var themes = level.Game?.Events?.Themes;
            if (themes != null)
            {
                foreach (var key in themes)
                {
                    if (key == null || !key.ThemeId.IsEnabled()) continue;
                    var result = edge(ResourceRef.Of(key.ThemeId));
                    if (result.Guid != key.ThemeId.value) key.ThemeId = result.AsTheme;
                }
            }

            if (level.Audio?.Tracks != null)
            {
                foreach (var track in level.Audio.Tracks.Values)
                {
                    if (track == null || !track.AudioResourceId.IsValid()) continue;
                    var result = edge(ResourceRef.Of(track.AudioResourceId));
                    if (result.Id != track.AudioResourceId.value) track.AudioResourceId = result.AsAudio;
                }
            }

            if (level.Resources?.Effects != null)
                foreach (var effect in level.Resources.Effects.Values)
                    Walk(effect, edge);

            if (level.Resources?.Prefabs != null)
                foreach (var prefab in level.Resources.Prefabs.Values)
                    Walk(prefab, edge, modifications);
        }

        /// <summary> A template: its root and every object it holds, nested placements included. </summary>
        public static void Walk(Prefab prefab, ResourceEdge edge, bool modifications)
        {
            if (prefab == null) return;

            // The root is a FIELD, never an entry in Objects - see Prefab.
            Walk(prefab.Root, edge, modifications);

            if (prefab.Objects == null) return;
            foreach (var obj in prefab.Objects.Values)
                Walk(obj, edge, modifications);
        }

        /// <summary> An effect preset: its particle mesh and its particle texture. </summary>
        public static void Walk(EffectData effect, ResourceEdge edge)
        {
            var core = effect?.Core;
            if (core == null) return;

            if (core.ParticleShapeId.IsEnabled())
            {
                var result = edge(ResourceRef.Of(core.ParticleShapeId));
                if (result.Guid != core.ParticleShapeId.value) core.ParticleShapeId = result.AsShape;
            }

            if (core.TextureResourceId.IsValid())
            {
                var result = edge(ResourceRef.Of(core.TextureResourceId));
                if (result.Id != core.TextureResourceId.value) core.TextureResourceId = result.AsTexture;
            }
        }

        /// <summary> One object, by its concrete type, plus a placement's id overrides when asked. </summary>
        public static void Walk(RectObject obj, ResourceEdge edge, bool modifications)
        {
            switch (obj)
            {
                case null:
                    return;

                case ShapeObject shape:
                    if (shape.ShapeId.IsEnabled())
                    {
                        var result = edge(ResourceRef.Of(shape.ShapeId));
                        if (result.Guid != shape.ShapeId.value) shape.ShapeId = result.AsShape;
                    }
                    if (shape.ColliderId.IsEnabled())
                    {
                        var result = edge(ResourceRef.Of(shape.ColliderId));
                        if (result.Guid != shape.ColliderId.value) shape.ColliderId = result.AsShape;
                    }
                    if (shape.TextureResourceId.IsValid())
                    {
                        var result = edge(ResourceRef.Of(shape.TextureResourceId));
                        if (result.Id != shape.TextureResourceId.value) shape.TextureResourceId = result.AsTexture;
                    }
                    return;

                case TextObject text:
                    if (text.FontResourceId.IsValid())
                    {
                        var result = edge(ResourceRef.Of(text.FontResourceId));
                        if (result.Id != text.FontResourceId.value) text.FontResourceId = result.AsFont;
                    }
                    return;

                case EffectObject effect:
                    if (effect.EffectId.IsEnabled())
                    {
                        var result = edge(ResourceRef.Of(effect.EffectId));
                        if (result.Guid != effect.EffectId.value) effect.EffectId = result.AsEffect;
                    }
                    return;

                case PrefabObject placement:
                    if (placement.PrefabId.IsEnabled())
                    {
                        var result = edge(ResourceRef.Of(placement.PrefabId));
                        if (result.Guid != placement.PrefabId.value) placement.PrefabId = result.AsPrefab;
                    }
                    if (modifications) WalkModifications(placement, edge);
                    return;
            }
        }

        // An override's value is untyped (Modification.Value) and may still be a raw JSON number or
        // token after a load, so it is converted through ModificationValues - the same reader the apply
        // path uses - and written back as the typed id only when the edge moved it.
        private static void WalkModifications(PrefabObject placement, ResourceEdge edge)
        {
            if (placement.Modifications == null) return;

            foreach (var modification in placement.Modifications.Values)
            {
                if (modification == null) continue;

                switch (modification.Key.Field)
                {
                    case ModificationFields.ShapeId:
                    case ModificationFields.ColliderId:
                        if (ModificationValues.TryConvert<ShapeId>(modification.Value, out var shape) && shape.IsEnabled())
                        {
                            var result = edge(ResourceRef.Of(shape));
                            if (result.Guid != shape.value) modification.Value = result.AsShape;
                        }
                        break;

                    case ModificationFields.TextureResourceId:
                        if (ModificationValues.TryConvert<TextureResourceId>(modification.Value, out var texture) && texture.IsValid())
                        {
                            var result = edge(ResourceRef.Of(texture));
                            if (result.Id != texture.value) modification.Value = result.AsTexture;
                        }
                        break;

                    case ModificationFields.FontResourceId:
                        if (ModificationValues.TryConvert<FontResourceId>(modification.Value, out var font) && font.IsValid())
                        {
                            var result = edge(ResourceRef.Of(font));
                            if (result.Id != font.value) modification.Value = result.AsFont;
                        }
                        break;

                    case ModificationFields.EffectId:
                        if (ModificationValues.TryConvert<EffectId>(modification.Value, out var effect) && effect.IsEnabled())
                        {
                            var result = edge(ResourceRef.Of(effect));
                            if (result.Guid != effect.value) modification.Value = result.AsEffect;
                        }
                        break;
                }
            }
        }

        /// <summary> Every reference a level holds, once per edge - an object using one shape to draw
        /// and to collide contributes it twice. Modifications excluded, see the header. </summary>
        public static List<ResourceRef> Collect(Level level)
        {
            var found = new List<ResourceRef>();
            Walk(level, reference =>
            {
                found.Add(reference);
                return reference;
            }, modifications: false);
            return found;
        }
    }
}

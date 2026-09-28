using System.Collections.Generic;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Resources;

namespace BH.SDK.Utils
{
    // RENUMBERING ON THE WAY IN. A texture, font or audio clip is addressed by a level-local int, so
    // two sources will both have a `-1` and an import has to give the incoming one a fresh number. A
    // guid resource travels as-is - except when the author answers a conflict with "import as a copy",
    // which mints a fresh guid for the same reason. Either way every reference to the moved resource
    // has to follow it, and ResourceGraph is the list of where those references are; this only
    // decides what they become.

    /// <summary> A set of id moves, applied to resources and to everything that points at them. </summary>
    public sealed class ResourceRemap
    {
        private readonly Dictionary<ResourceRef, ResourceRef> _moves = new();

        /// <summary> How many moves are recorded. </summary>
        public int Count => _moves.Count;

        /// <summary> Records that <paramref name="from"/> becomes <paramref name="to"/>. The two must
        /// be the same family. </summary>
        public void Add(ResourceRef from, ResourceRef to)
        {
            if (from.Type != to.Type)
                throw new System.ArgumentException($"Cannot move {from} to a different family ({to}).");

            if (from != to) _moves[from] = to;
        }

        /// <summary> Where a reference ends up; itself when it does not move. </summary>
        public ResourceRef Map(ResourceRef reference) => _moves.TryGetValue(reference, out var to) ? to : reference;

        /// <summary> Rewrites a template's references and, when it moves, its own id. </summary>
        public void Apply(Prefab prefab)
        {
            if (prefab == null) return;

            prefab.PrefabId = Map(ResourceRef.Of(prefab.PrefabId)).AsPrefab;
            ResourceGraph.Walk(prefab, Map, modifications: true);
        }

        /// <summary> Rewrites an effect preset's references and, when it moves, its own id. </summary>
        public void Apply(EffectData effect)
        {
            if (effect == null) return;

            effect.EffectId = Map(ResourceRef.Of(effect.EffectId)).AsEffect;
            ResourceGraph.Walk(effect, Map);
        }

        /// <summary> Moves a theme's own id. A theme references nothing. </summary>
        public void Apply(ThemeData theme)
        {
            if (theme != null) theme.ThemeId = Map(ResourceRef.Of(theme.ThemeId)).AsTheme;
        }

        /// <summary> Moves a shape's own id. A shape references nothing. </summary>
        public void Apply(CompositeShape shape)
        {
            if (shape != null) shape.ShapeId = Map(ResourceRef.Of(shape.ShapeId)).AsShape;
        }

        /// <summary> Moves a texture's own id. </summary>
        public void Apply(TextureResource texture)
        {
            if (texture != null) texture.TextureResourceId = Map(ResourceRef.Of(texture.TextureResourceId)).AsTexture;
        }

        /// <summary> Moves a font's own id. </summary>
        public void Apply(FontResource font)
        {
            if (font != null) font.FontResourceId = Map(ResourceRef.Of(font.FontResourceId)).AsFont;
        }

        /// <summary> Moves an audio clip's own id. </summary>
        public void Apply(AudioResource audio)
        {
            if (audio != null) audio.AudioResourceId = Map(ResourceRef.Of(audio.AudioResourceId)).AsAudio;
        }
    }
}

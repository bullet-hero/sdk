using System;
using System.Collections;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Utils
{
    // ONE ADDRESS FOR SEVEN ID TYPES. The file resources are level-local ints and the data resources
    // are guids, and anything that walks "whatever this points at" - a closure, a remap, a count -
    // needs to hold both in one set. ResourceType already names the seven families (and is what a
    // ResourceMeta record is keyed by), so it is reused rather than mirrored by a second enum. The
    // cover (ResourceType.LevelLogo) fills neither slot and is never null: its type is its address,
    // so a record for it maps to one reference, and nothing that walks resources ever produces it.

    /// <summary> A typed reference to one user resource of any family. </summary>
    public readonly struct ResourceRef : IEquatable<ResourceRef>
    {
        /// <summary> Which family, and so which of the two slots below is used. </summary>
        public readonly ResourceType Type;

        /// <summary> The id of a file resource; 0 for a data resource. </summary>
        public readonly int Id;

        /// <summary> The id of a data resource; empty for a file resource. </summary>
        public readonly Guid Guid;

        private ResourceRef(ResourceType type, int id, Guid guid)
        {
            Type = type;
            Id = id;
            Guid = guid;
        }

        /// <summary> A texture reference. </summary>
        public static ResourceRef Of(TextureResourceId id) => new(ResourceType.Texture, id.value, Guid.Empty);

        /// <summary> A font reference. </summary>
        public static ResourceRef Of(FontResourceId id) => new(ResourceType.Font, id.value, Guid.Empty);

        /// <summary> An audio clip reference. </summary>
        public static ResourceRef Of(AudioResourceId id) => new(ResourceType.Audio, id.value, Guid.Empty);

        /// <summary> A custom shape reference. </summary>
        public static ResourceRef Of(ShapeId id) => new(ResourceType.Shape, 0, id.value);

        /// <summary> A theme reference. </summary>
        public static ResourceRef Of(ThemeId id) => new(ResourceType.Theme, 0, id.value);

        /// <summary> An effect preset reference. </summary>
        public static ResourceRef Of(EffectId id) => new(ResourceType.Effect, 0, id.value);

        /// <summary> A prefab template reference. </summary>
        public static ResourceRef Of(PrefabId id) => new(ResourceType.Prefab, 0, id.value);

        /// <summary> Built from a family and whichever slot it uses. </summary>
        public static ResourceRef Of(ResourceType type, int id, Guid guid)
            => type.IsTypeAddressed() ? new ResourceRef(type, 0, Guid.Empty)
                : type.IsGuidAddressed() ? new ResourceRef(type, 0, guid)
                : new ResourceRef(type, id, Guid.Empty);

        /// <summary> True when the reference points at nothing - the unset id of its family. </summary>
        public bool IsNull => !Type.IsTypeAddressed() && (Type.IsGuidAddressed() ? Guid == Guid.Empty : Id == 0);

        /// <summary> Back to the typed id. </summary>
        public TextureResourceId AsTexture => new(Id);

        /// <summary> Back to the typed id. </summary>
        public FontResourceId AsFont => new(Id);

        /// <summary> Back to the typed id. </summary>
        public AudioResourceId AsAudio => new(Id);

        /// <summary> Back to the typed id. </summary>
        public ShapeId AsShape => new(Guid);

        /// <summary> Back to the typed id. </summary>
        public ThemeId AsTheme => new(Guid);

        /// <summary> Back to the typed id. </summary>
        public EffectId AsEffect => new(Guid);

        /// <summary> Back to the typed id. </summary>
        public PrefabId AsPrefab => new(Guid);

        /// <summary> Family and slot alike. </summary>
        public bool Equals(ResourceRef other) => Type == other.Type && Id == other.Id && Guid == other.Guid;

        /// <summary> Family and slot alike. </summary>
        public override bool Equals(object obj) => obj is ResourceRef other && Equals(other);

        /// <summary> Family and slot alike. </summary>
        public override int GetHashCode() => HashCode.Combine((byte)Type, Id, Guid);

        /// <summary> Family and slot alike. </summary>
        public static bool operator ==(ResourceRef a, ResourceRef b) => a.Equals(b);

        /// <summary> Family and slot alike. </summary>
        public static bool operator !=(ResourceRef a, ResourceRef b) => !a.Equals(b);

        /// <summary> "Texture:-1", "Theme:{guid}" or "LevelLogo". </summary>
        public override string ToString()
            => Type.IsTypeAddressed() ? Type.ToString() : Type.IsGuidAddressed() ? $"{Type}:{Guid}" : $"{Type}:{Id}";
    }

    /// <summary> A set of resource references, across every family. </summary>
    public sealed class ResourceSet : IEnumerable<ResourceRef>
    {
        private readonly HashSet<ResourceRef> _refs = new();

        /// <summary> How many distinct references. </summary>
        public int Count => _refs.Count;

        /// <summary> Adds one; a null reference is never added. False when it was already there or null. </summary>
        public bool Add(ResourceRef reference) => !reference.IsNull && _refs.Add(reference);

        /// <summary> Whether it holds that reference. </summary>
        public bool Contains(ResourceRef reference) => _refs.Contains(reference);

        /// <summary> Removes one. </summary>
        public bool Remove(ResourceRef reference) => _refs.Remove(reference);

        /// <summary> Every reference of one family. </summary>
        public IEnumerable<ResourceRef> OfType(ResourceType type)
        {
            foreach (var reference in _refs)
                if (reference.Type == type)
                    yield return reference;
        }

        /// <summary> In no particular order. </summary>
        public IEnumerator<ResourceRef> GetEnumerator() => _refs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

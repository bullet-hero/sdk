using System;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Models.Primitives
{
    // THE INDEX IS WRITTEN UNCONDITIONALLY, even at -1, and the eight bytes that saves are refused
    // on purpose. This key has TWO writers that must agree byte for byte - the reflective one
    // (Newtonsoft over the [JsonProperty]s below, reached through DictionaryModificationsConverter)
    // and JsonPrimitives.Write. Omitting a default means DefaultValueHandling on one side and an if
    // on the other, which is exactly the divergence JsonParityTests exists to catch. The string this
    // replaced already paid for those bytes several times over.
    //
    // The members stay settable auto-properties rather than becoming a readonly struct, and that is
    // load-bearing for the same reason: Newtonsoft binds a constructor by matching parameter names
    // against the resolved PROPERTY names, which here are "id", "f" and "i". A readonly struct would
    // bind nothing and read back three defaults, silently.

    /// <summary>
    /// Address of a single overridable field inside a prefab placement: which object, which field,
    /// and which element of it. Used as the key of PrefabObject.Modifications, which is what makes
    /// "one override per (object, field) pair" a structural guarantee instead of a rule to enforce.
    /// </summary>
    public struct ModificationKey : IModel<ModificationKey>, IComparable<ModificationKey>
    {
        /// <summary> An index addressing the field as a whole rather than one element of it. </summary>
        public const int WholeField = -1;

        /// <summary> Object being overridden, addressed by its id inside the TEMPLATE, not the
        /// materialized outer id - so the key survives re-materialization. </summary>
        [JsonProperty(Names.ObjectId)]
        public ObjectId ObjectId { get; set; }

        /// <summary> Which field, as a stable <see cref="ModificationFields"/> number - not the
        /// spelling of a JSON key, so renaming one costs an override nothing. </summary>
        [JsonProperty(Names.FieldShort)]
        public int Field { get; set; }

        /// <summary> Which element of a collection field, or <see cref="WholeField"/> for the field
        /// whole. Legal only where ModificationTable.IsCollection answers true. </summary>
        [JsonProperty(Names.IndexShort)]
        public int Index { get; set; }

        /// <summary> An override of a whole field - the shape all but element overrides take. </summary>
        public ModificationKey(ObjectId objectId, int field) : this(objectId, field, WholeField)
        {
        }

        /// <summary> An override of one element of a collection field. </summary>
        public ModificationKey(ObjectId objectId, int field, int index)
        {
            ObjectId = objectId;
            Field = field;
            Index = index;
        }

        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            ObjectId = ObjectId.Null;
            Field = ModificationFields.None;
            Index = WholeField;
        }

        /// <summary> The untyped spelling of <c>Copy</c>. </summary>
        public readonly object Clone() => Copy();

        /// <summary> A deep copy, sharing nothing mutable with this one. </summary>
        public readonly ModificationKey Copy() => new(ObjectId, Field, Index);

        /// <summary> Becomes the source, replacing everything this instance held. </summary>
        public void Update(ModificationKey src)
        {
            ObjectId = src.ObjectId;
            Field = src.Field;
            Index = src.Index;
        }

        /// <summary> Takes the source's contents in place, so nothing pointing inside this instance is invalidated. </summary>
        public void Pull(ModificationKey src)
        {
            ObjectId = src.ObjectId;
            Field = src.Field;
            Index = src.Index;
        }

        /// <summary> Member by member. </summary>
        public readonly bool Equals(ModificationKey other)
            => ObjectId.Equals(other.ObjectId) && Field == other.Field && Index == other.Index;

        /// <summary> The same, boxed. </summary>
        public readonly override bool Equals(object obj) => obj is ModificationKey other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        public readonly override int GetHashCode() => HashCode.Combine(ObjectId, Field, Index);

        /// <summary> Ordered by object, then field, then element, so a placement's overrides list stably. </summary>
        public readonly int CompareTo(ModificationKey other)
        {
            var cmp = ObjectId.value.CompareTo(other.ObjectId.value);
            if (cmp != 0) return cmp;

            cmp = Field.CompareTo(other.Field);
            return cmp != 0 ? cmp : Index.CompareTo(other.Index);
        }

        /// <summary> One line, for a log - the field in hex, the way ModificationFields declares it. </summary>
        public readonly override string ToString()
            => Index == WholeField
                ? $"{ObjectId}/0x{Field:X4}"
                : $"{ObjectId}/0x{Field:X4}[{Index}]";
    }
}

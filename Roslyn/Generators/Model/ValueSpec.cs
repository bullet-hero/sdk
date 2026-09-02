using System;

namespace BH.SDK.Roslyn.Model
{
    // ONE VALUE, AND HOW IT IS WRITTEN. MemberShape answers what a member IS to Copy and Equals - a
    // list, a dictionary, a nested model - and stops there, because those three only ever need to
    // know the shape. An encoding needs the LEAF: the element inside the list, the key of the
    // dictionary, the enum's underlying width. That is what this is, and it is deliberately a
    // separate axis rather than more MemberShape members - the cross product of shape and leaf is
    // where a flat enum turns into forty cases nobody can read.

    /// <summary> How one leaf value is written and read. </summary>
    internal enum ValueKind
    {
        None,
        Bool,
        Byte,
        SByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        String,
        Guid,
        DateTime,
        /// <summary> System.Version - immutable, so written as the text it round-trips through. </summary>
        Version,
        /// <summary> Written at the width its underlying type has, never widened. </summary>
        Enum,
        /// <summary> An id wrapping one int behind a single-argument constructor. </summary>
        PrimitiveInt,
        /// <summary> An id wrapping one Guid. </summary>
        PrimitiveGuid,
        /// <summary> An id wrapping one float. </summary>
        PrimitiveFloat,
        /// <summary> A struct BlobPrimitives writes by hand - FrameSpan, ModificationKey, RunProfile,
        /// Pixel. Each is there for a reason its own header gives. </summary>
        Struct,
        /// <summary> A model whose declared type is sealed: no tag needed, one presence byte. </summary>
        ModelSealed,
        /// <summary> A model reached through a base class or a family interface: the generated
        /// dispatcher writes a tag first, since the reader has to know what to construct. </summary>
        ModelPolymorphic,
    }

    /// <summary> A leaf's type and how to encode it. </summary>
    internal readonly struct ValueSpec : IEquatable<ValueSpec>
    {
        public ValueSpec(string type, ValueKind kind, ValueKind underlying = ValueKind.None,
            string accessor = "", string version = "", string family = "")
        {
            Type = type;
            Kind = kind;
            Underlying = underlying;
            Accessor = accessor;
            Version = version;
            Family = family;
        }

        /// <summary> Fully qualified, global::-prefixed. </summary>
        public string Type { get; }
        public ValueKind Kind { get; }
        /// <summary> Enum only: the width it actually occupies. </summary>
        public ValueKind Underlying { get; }

        /// <summary> Id wrapper only: the member holding the wrapped value. It is RESOLVED rather
        /// than assumed, because every id here implements IPrimitiveInt.Value EXPLICITLY and casting
        /// a struct to an interface boxes it - once per value, in the hottest loop this format has.
        /// The public field beside it costs nothing. </summary>
        public string Accessor { get; }

        /// <summary> "major.minor" when this leaf's TYPE is a [DataVersion] aggregate, empty
        /// otherwise. A versioned member is wrapped in its own envelope by whoever HOLDS it, not by
        /// itself - which is what leaves the top-level wrapper to VersionedEnvelopeConverter, and
        /// with it the migration path an older file still needs. </summary>
        public string Version { get; }

        /// <summary> The value-family interface this leaf's type implements, when it has one.
        /// A member declared as the CONCRETE type is still written `[tag, payload]` in JSON -
        /// ConverterRouter resolves by the value's RUNTIME type and the family converter matches
        /// any implementor, so Marker.Color4 is `[0,{...}]` even though it can only ever be a
        /// Color4Value. The blob needs no such thing: sealed means the type IS the declared one. </summary>
        public string Family { get; }

        public bool IsNone => Kind == ValueKind.None;

        public bool Equals(ValueSpec other) => Type == other.Type && Kind == other.Kind
            && Underlying == other.Underlying && Accessor == other.Accessor
            && Version == other.Version && Family == other.Family;

        public override bool Equals(object obj) => obj is ValueSpec other && Equals(other);

        public override int GetHashCode() => unchecked((Type?.GetHashCode() ?? 0) * 397
            ^ (int)Kind * 31 ^ (int)Underlying ^ (Accessor?.GetHashCode() ?? 0)
            ^ (Version?.GetHashCode() ?? 0) ^ (Family?.GetHashCode() ?? 0));
    }
}

using System;

namespace BH.SDK.Roslyn.Model
{
    // One value and how it is written: the LEAF (element, key, enum width), a separate axis from
    // MemberShape.

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

    // Lockstep: mirrors ModelGenerations.Invalid, because this assembly references no SDK.

    /// <summary> The generation numbers the generator itself needs to name. </summary>
    internal static class ModelGenerationValues
    {
        /// <summary> No generation: a leaf whose type is not a versioning boundary. </summary>
        public const int Invalid = -1;
    }

    /// <summary> A leaf's type and how to encode it. </summary>
    internal readonly struct ValueSpec : IEquatable<ValueSpec>
    {
        /// <summary> One value's type as the emitters see it - what it is, and what it wraps where that matters. </summary>
        public ValueSpec(string type, ValueKind kind, ValueKind underlying = ValueKind.None,
            string accessor = "", int generation = ModelGenerationValues.Invalid, string family = "",
            string domain = "")
        {
            Type = type;
            Kind = kind;
            Underlying = underlying;
            Accessor = accessor;
            Generation = generation;
            Family = family;
            Domain = domain;
        }

        /// <summary> Fully qualified, global::-prefixed. </summary>
        public string Type { get; }
        /// <summary> Which encoding the emitters use for it. </summary>
        public ValueKind Kind { get; }
        /// <summary> Enum only: the width it actually occupies. </summary>
        public ValueKind Underlying { get; }

        /// <summary> Id wrapper only: the member holding the wrapped value. It is RESOLVED rather
        /// than assumed, because every id here implements IPrimitiveInt.Value EXPLICITLY and casting
        /// a struct to an interface boxes it - once per value, in the hottest loop this format has.
        /// The public field beside it costs nothing. </summary>
        public string Accessor { get; }

        /// <summary> The generation when this leaf's TYPE is a [ModelGeneration] aggregate,
        /// Invalid otherwise. A versioned member is wrapped in its own envelope by whoever HOLDS it,
        /// not by itself - which is what leaves the top-level wrapper to VersionedEnvelopeConverter,
        /// and with it the migration path an older file still needs. </summary>
        public int Generation { get; }

        /// <summary> The domain name beside that generation, empty otherwise. The reader needs BOTH to
        /// migrate: the generation says a file disagrees, and only the domain says what to resolve it
        /// against. It was already read here and thrown away while a nested envelope could only refuse. </summary>
        public string Domain { get; }

        /// <summary> The value-family interface this leaf implements, if any. A concrete member is still
        /// written [tag, payload] in JSON (ConverterRouter matches by runtime type); the blob needs no such
        /// thing - sealed means the type IS the declared one. </summary>
        public string Family { get; }

        /// <summary> True when nothing here can encode the type. </summary>
        public bool IsNone => Kind == ValueKind.None;

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
        public bool Equals(ValueSpec other) => Type == other.Type && Kind == other.Kind
            && Underlying == other.Underlying && Accessor == other.Accessor
            && Generation == other.Generation && Family == other.Family && Domain == other.Domain;

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is ValueSpec other && Equals(other);

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
        public override int GetHashCode() => unchecked((Type?.GetHashCode() ?? 0) * 397
            ^ (int)Kind * 31 ^ (int)Underlying ^ (Accessor?.GetHashCode() ?? 0)
            ^ Generation ^ (Family?.GetHashCode() ?? 0) ^ (Domain?.GetHashCode() ?? 0));
    }
}

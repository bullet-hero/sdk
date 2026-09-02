using System;

namespace BH.SDK.Roslyn.Model
{
    // Everything the emitter needs, and NOTHING that holds a compiler symbol. An incremental
    // generator's pipeline caches by value, and a cached ISymbol pins a whole Compilation in memory
    // and never compares equal twice - so the extraction step flattens to plain strings and enums
    // here, once, and the emitter never touches the semantic model again.

    /// <summary> How a member is copied, compared and merged - the one decision that drives every
    /// generated body. </summary>
    internal enum MemberShape
    {
        /// <summary> A value, a string, an enum, a Version - assigned, never copied. </summary>
        Value,
        /// <summary> A model held by its own concrete type: copied, and PULLED in place. </summary>
        Model,
        /// <summary> A model held by a polymorphic interface: copied, and pulled through PullFrom,
        /// since a Vector2Value cannot become a RandomVector2. </summary>
        PolymorphicModel,
        /// <summary> List of models. </summary>
        ModelList,
        /// <summary> List of values - copied by the list constructor, not per item. </summary>
        ValueList,
        /// <summary> Array of models. </summary>
        ModelArray,
        /// <summary> Array of unmanaged values - blitted, because it may hold millions. </summary>
        UnmanagedArray,
        /// <summary> Dictionary whose values are models. </summary>
        ModelDictionary,
        /// <summary> Dictionary of plain values, copied by the dictionary constructor. </summary>
        ValueDictionary,
    }

    /// <summary> One property the generator writes for. </summary>
    internal readonly struct MemberSpec : IEquatable<MemberSpec>
    {
        public MemberSpec(string name, string type, MemberShape shape, bool isValueType,
            bool keyIsModel, bool mergeOnPull, string pullDispatcher,
            ValueSpec value, ValueSpec element, ValueSpec key,
            string jsonName, bool assignable, bool jsonIgnored, string keyProperty)
        {
            JsonName = jsonName;
            Assignable = assignable;
            JsonIgnored = jsonIgnored;
            KeyProperty = keyProperty;
            Name = name;
            Type = type;
            Shape = shape;
            IsValueType = isValueType;
            KeyIsModel = keyIsModel;
            MergeOnPull = mergeOnPull;
            PullDispatcher = pullDispatcher;
            Value = value;
            Element = element;
            Key = key;
        }

        public string Name { get; }
        /// <summary> Fully qualified, global::-prefixed. </summary>
        public string Type { get; }
        public MemberShape Shape { get; }
        public bool IsValueType { get; }
        /// <summary> Dictionary only: the KEY is not unmanaged, so copying it goes through the
        /// managed overload and its ICopyable constraint. </summary>
        public bool KeyIsModel { get; }
        /// <summary> Pull merges this collection key by key instead of replacing it. </summary>
        public bool MergeOnPull { get; }
        /// <summary> Merge only: the generated dispatcher that pulls one value, or null when the
        /// value type is sealed and ModelUtils.PullFrom already knows how. </summary>
        public string PullDispatcher { get; }

        /// <summary> The member itself, when it is one value rather than a collection. </summary>
        public ValueSpec Value { get; }
        /// <summary> A list's or array's element, or a dictionary's VALUE. </summary>
        public ValueSpec Element { get; }
        /// <summary> A dictionary's key. </summary>
        public ValueSpec Key { get; }

        /// <summary> The name this member carries on the wire - its [JsonProperty], or its own name
        /// when it has none. Newtonsoft's contract resolves it the same way. </summary>
        public string JsonName { get; }

        /// <summary> Has a setter, so it takes part in Copy, Update, Pull, Reset, Equals and the
        /// blob. A get-only one is WRITTEN to JSON and never read back - Resource.Type is the only
        /// one, and dropping it would change the wire format. </summary>
        public bool Assignable { get; }

        /// <summary> [JsonIgnore]: in the contract, skipped by the writer. </summary>
        public bool JsonIgnored { get; }

        /// <summary> Keyed dictionary: the property on the VALUE the key is recovered from, so the
        /// collection writes as a bare array. Null means the pair form. </summary>
        public string KeyProperty { get; }

        public bool Equals(MemberSpec other) => Name == other.Name && Type == other.Type
            && Shape == other.Shape && IsValueType == other.IsValueType
            && KeyIsModel == other.KeyIsModel && MergeOnPull == other.MergeOnPull
            && PullDispatcher == other.PullDispatcher && Value.Equals(other.Value)
            && Element.Equals(other.Element) && Key.Equals(other.Key)
            && JsonName == other.JsonName && Assignable == other.Assignable
            && JsonIgnored == other.JsonIgnored && KeyProperty == other.KeyProperty;

        public override bool Equals(object obj) => obj is MemberSpec other && Equals(other);

        public override int GetHashCode() => unchecked(
            (Name?.GetHashCode() ?? 0) * 397 ^ (Type?.GetHashCode() ?? 0) * 31 ^ (int)Shape
            ^ (IsValueType ? 1 : 0) ^ (KeyIsModel ? 2 : 0) ^ (MergeOnPull ? 4 : 0)
            ^ (PullDispatcher?.GetHashCode() ?? 0));
    }

    /// <summary> A polymorphic family this model belongs to - IVector2, IColor4X4Key, and so on.
    /// Each one adds a second, interface-typed copy of the whole contract. </summary>
    internal readonly struct FamilySpec : IEquatable<FamilySpec>
    {
        public FamilySpec(string interfaceType) => InterfaceType = interfaceType;

        /// <summary> Fully qualified, global::-prefixed. </summary>
        public string InterfaceType { get; }

        public bool Equals(FamilySpec other) => InterfaceType == other.InterfaceType;
        public override bool Equals(object obj) => obj is FamilySpec other && Equals(other);
        public override int GetHashCode() => InterfaceType?.GetHashCode() ?? 0;
    }

    /// <summary> One [GenerateModel] type, flattened. </summary>
    internal sealed class ModelSpec : IEquatable<ModelSpec>
    {
        public ModelSpec(string @namespace, string name, string qualifiedName, string accessibility,
            bool isSealed, bool isAbstract, string baseModel, EquatableArray<MemberSpec> members,
            EquatableArray<FamilySpec> families, string hintName,
            int typeTag, string domain, int major, int minor)
        {
            IsAbstract = isAbstract;
            TypeTag = typeTag;
            Domain = domain;
            Major = major;
            Minor = minor;
            Namespace = @namespace;
            Name = name;
            QualifiedName = qualifiedName;
            Accessibility = accessibility;
            IsSealed = isSealed;
            BaseModel = baseModel;
            Members = members;
            Families = families;
            HintName = hintName;
        }

        public string Namespace { get; }
        public string Name { get; }
        /// <summary> Fully qualified, global::-prefixed. </summary>
        public string QualifiedName { get; }
        public string Accessibility { get; }
        /// <summary> Sealed decides `virtual` versus nothing, and it is the ONLY thing that does -
        /// asking "does anything derive from me" would need the whole compilation and would break
        /// per-type caching. A sealed type cannot be derived from; a non-sealed one might be. </summary>
        public bool IsSealed { get; }
        /// <summary> An abstract model cannot be instantiated, so Reset/Clone/Copy are DECLARED
        /// here and answered by whichever subtype the generator reaches next. The chain helpers
        /// stay concrete - they are what the subtype's own bodies call. </summary>
        public bool IsAbstract { get; }
        /// <summary> The [GenerateModel] base this type derives from, or null. </summary>
        public string BaseModel { get; }
        public EquatableArray<MemberSpec> Members { get; }
        public EquatableArray<FamilySpec> Families { get; }
        public string HintName { get; }

        /// <summary> What GetModelType() answers, as a number, or -1 for a type that has no such
        /// method. It is the blob's polymorphic tag, and reusing the format's OWN discriminator
        /// rather than inventing a second one is what keeps the two encodings describing the same
        /// thing - a JSON [tag, payload] and a blob [tag][payload] carry the same tag. </summary>
        public int TypeTag { get; }

        /// <summary> The [DataVersion] domain this type is the root of, or null. A versioned type
        /// writes its own envelope, exactly as it does in JSON. </summary>
        public string Domain { get; }
        public int Major { get; }
        public int Minor { get; }

        public bool Equals(ModelSpec other) => other is not null
            && Namespace == other.Namespace && Name == other.Name
            && QualifiedName == other.QualifiedName && Accessibility == other.Accessibility
            && IsSealed == other.IsSealed && IsAbstract == other.IsAbstract
            && BaseModel == other.BaseModel
            && Members.Equals(other.Members) && Families.Equals(other.Families)
            && HintName == other.HintName && TypeTag == other.TypeTag
            && Domain == other.Domain && Major == other.Major && Minor == other.Minor;

        public override bool Equals(object obj) => obj is ModelSpec other && Equals(other);

        public override int GetHashCode() => unchecked(
            QualifiedName.GetHashCode() * 397 ^ Members.GetHashCode() * 31
            ^ Families.GetHashCode() ^ (IsSealed ? 1 : 0) ^ (BaseModel?.GetHashCode() ?? 0));
    }
}

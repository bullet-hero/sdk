using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BH.SDK.Roslyn.Model
{
    /// <summary> Turns one [GenerateModel] symbol into a <see cref="ModelSpec"/>, or into
    /// diagnostics saying why it cannot. </summary>
    internal static class ModelSpecFactory
    {
        public const string GenerateAttribute = "BH.SDK.Models.Attributes.GenerateModelAttribute";
        public const string IgnoreAttribute = "BH.SDK.Models.Attributes.GenerateModelIgnoreAttribute";
        public const string MergeAttribute = "BH.SDK.Models.Attributes.GenerateModelMergeAttribute";
        public const string KeyedAttribute = "BH.SDK.Models.Attributes.GenerateModelKeyedAttribute";

        /// <summary> A type with no GetModelType() cannot be a polymorphic value. </summary>
        public const int NoTypeTag = -1;

        public static ModelSpec Create(INamedTypeSymbol type, TypeDeclarationSyntax declaration,
            List<Diagnostic> diagnostics)
        {
            if (!declaration.Modifiers.Any(m => m.ValueText == "partial"))
            {
                diagnostics.Add(Diagnostic.Create(ModelDiagnostics.NotPartial,
                    declaration.Identifier.GetLocation(), type.Name));
                return null;
            }

            if (!type.IsAbstract && !HasParameterlessConstructor(type))
            {
                diagnostics.Add(Diagnostic.Create(ModelDiagnostics.NoParameterlessConstructor,
                    declaration.Identifier.GetLocation(), type.Name));
                return null;
            }

            var baseModel = ResolveBaseModel(type, declaration, diagnostics, out var baseFailed);
            var members = ResolveMembers(type, declaration, diagnostics);
            // Both halves are collected before either refusal is acted on, so one run names every
            // problem in the type rather than one problem per rebuild.
            if (baseFailed || members is null) return null;

            return new ModelSpec(
                type.ContainingNamespace.IsGlobalNamespace
                    ? string.Empty
                    : type.ContainingNamespace.ToDisplayString(),
                type.Name,
                Qualified(type),
                type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal",
                type.IsSealed,
                type.IsAbstract,
                baseModel,
                EquatableArray.From(members),
                EquatableArray.From(ResolveFamilies(type)),
                HintName(type),
                ResolveTypeTag(type),
                ResolveDomain(type, out var major, out var minor),
                major,
                minor);
        }

        #region Shape resolution

        private static List<MemberSpec> ResolveMembers(INamedTypeSymbol type,
            TypeDeclarationSyntax declaration, List<Diagnostic> diagnostics)
        {
            var members = new List<MemberSpec>();
            var failed = false;

            // Declared HERE only. A base's own members are written by the base's own generated
            // body and reached through base.Copy/Update/Pull/Equals, exactly as the hand-written
            // code chains today - restating them would write each one twice.
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.IsIndexer) continue;
                if (property.GetMethod is null) continue;
                if (property.DeclaredAccessibility != Accessibility.Public) continue;
                if (HasAttribute(property, IgnoreAttribute)) continue;

                // AN ABSTRACT PROPERTY IS WRITTEN BY WHICHEVER SUBTYPE OVERRIDES IT, never here.
                // It has no value to write, and Newtonsoft puts the override in the DERIVED type's
                // block anyway - Resource.Type lands between TextureResource's own members and
                // Resource's `src`, not after it. Emitting it in both places wrote it twice.
                if (property.IsAbstract) continue;

                // A get-only property is WRITE-ONLY on the wire and takes part in nothing else.
                // Newtonsoft writes it (the contract is OptOut in practice - the resolver sets
                // MemberSerialization after the properties are already collected), so dropping it
                // would change the format; Resource.Type is the one that does this.
                var assignable = property.SetMethod != null;
                if (JsonIgnored(property) && property.SetMethod is null) continue;

                var merge = HasAttribute(property, MergeAttribute);
                var shape = Classify(property.Type, out var keyIsModel, out var pullDispatcher);

                if (shape is null)
                {
                    failed = true;
                    diagnostics.Add(Diagnostic.Create(ModelDiagnostics.UnsupportedMember,
                        MemberLocation(property, declaration), type.Name, property.Name,
                        property.Type.ToDisplayString()));
                    continue;
                }

                if (merge && shape != MemberShape.ModelDictionary && shape != MemberShape.ValueDictionary)
                {
                    failed = true;
                    diagnostics.Add(Diagnostic.Create(ModelDiagnostics.MergeOnNonDictionary,
                        MemberLocation(property, declaration), type.Name, property.Name));
                    continue;
                }

                Leaves(property.Type, shape.Value, out var value, out var element, out var key);

                members.Add(new MemberSpec(property.Name, Qualified(property.Type), shape.Value,
                    property.Type.IsValueType, keyIsModel, merge, merge ? pullDispatcher : null,
                    value, element, key,
                    JsonName(property), assignable, JsonIgnored(property), KeyProperty(property)));
            }

            return failed ? null : members;
        }

        /// <summary> The one decision that drives every generated body. Null means "no encoding",
        /// which becomes an error naming the member rather than a member quietly dropped. </summary>
        private static MemberShape? Classify(ITypeSymbol type, out bool keyIsModel,
            out string pullDispatcher)
        {
            keyIsModel = false;
            pullDispatcher = null;

            if (type is IArrayTypeSymbol array)
            {
                if (IsModelReference(array.ElementType)) return MemberShape.ModelArray;
                if (array.ElementType.IsUnmanagedType) return MemberShape.UnmanagedArray;
                return null;
            }

            // A struct is copied by assignment even when it is a model - FrameSpan and
            // ModificationKey both are, and both say so themselves with `this = src` in Update.
            if (type.IsValueType) return MemberShape.Value;
            if (type.SpecialType == SpecialType.System_String) return MemberShape.Value;

            if (!(type is INamedTypeSymbol named)) return null;

            // System.Version is a reference type with no mutable state - the only one here, and
            // treating it as a value is what the hand-written code already does.
            if (named.ToDisplayString() == "System.Version") return MemberShape.Value;

            if (named.IsGenericType)
            {
                var definition = named.ConstructedFrom.ToDisplayString();

                if (definition == "System.Collections.Generic.List<T>")
                {
                    var item = named.TypeArguments[0];
                    if (IsModelReference(item)) return MemberShape.ModelList;
                    if (IsValueLike(item)) return MemberShape.ValueList;
                    return null;
                }

                if (definition == "System.Collections.Generic.Dictionary<TKey, TValue>")
                {
                    var key = named.TypeArguments[0];
                    var value = named.TypeArguments[1];

                    if (IsModelReference(value))
                    {
                        // Which of the three dictionary copies applies is decided by the KEY, and
                        // "unmanaged" is the real question rather than "is it a model":
                        // ModificationKey is a struct holding a string, so it is neither, and only
                        // the managed copy's ICopyable constraint fits it.
                        keyIsModel = !key.IsUnmanagedType;
                        if (keyIsModel && !IsCopyable(key)) return null;
                        // A sealed value type needs no dispatcher: ModelUtils.PullFrom already
                        // knows how to merge it. A non-sealed one might be any of its subtypes,
                        // and pulling through the base writes the base half and drops the rest.
                        pullDispatcher = value.IsSealed ? null : DispatcherName(value);
                        return MemberShape.ModelDictionary;
                    }

                    if (IsValueLike(key) && IsValueLike(value)) return MemberShape.ValueDictionary;
                    return null;
                }
            }

            if (IsModelReference(type))
                return type.TypeKind == TypeKind.Interface
                    ? MemberShape.PolymorphicModel
                    : MemberShape.Model;

            return null;
        }

        #endregion

        #region Symbol predicates

        /// <summary> A model addressed by its own type - the only shape the contract is total for.
        /// A polymorphic interface satisfies it too: IVector2 implements IModel of IVector2. </summary>
        public static bool IsModelReference(ITypeSymbol type) =>
            !type.IsValueType && ImplementsSelf(type, "IModel");

        /// <summary> Carries its own Copy(), so the managed dictionary copy can take it. </summary>
        private static bool IsCopyable(ITypeSymbol type) => ImplementsSelf(type, "ICopyable");

        // Matched by NAME AND NAMESPACE rather than by a formatted display string, because the
        // display of a generic definition is not stable across the shapes these interfaces take:
        // ICopyable is declared `ICopyable<out T>` and renders its variance, IModel does not.
        // Comparing formatted text made a covariant interface silently invisible once already.
        private static bool ImplementsSelf(ITypeSymbol type, string interfaceName) =>
            type.AllInterfaces.Any(i =>
                i.IsGenericType
                && i.Name == interfaceName
                && i.ContainingNamespace?.ToDisplayString() == "BH.SDK.Models.Interfaces"
                && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type));

        /// <summary> Copied by assignment: a value, a string, an enum, System.Version. </summary>
        private static bool IsValueLike(ITypeSymbol type) => type.IsValueType
            || type.SpecialType == SpecialType.System_String
            || type.ToDisplayString() == "System.Version";

        private static bool HasParameterlessConstructor(INamedTypeSymbol type) =>
            type.InstanceConstructors.Any(c => c.Parameters.Length == 0
                && c.DeclaredAccessibility != Accessibility.Private);

        /// <summary> What the member is called on the wire. Newtonsoft's contract resolves this
        /// the same way, and JsonMemberContractTests compares the two for every model. </summary>
        private static string JsonName(IPropertySymbol property)
        {
            foreach (var attribute in property.GetAttributes())
            {
                if (attribute.AttributeClass?.Name != "JsonPropertyAttribute") continue;
                if (attribute.ConstructorArguments.Length == 0) continue;
                if (attribute.ConstructorArguments[0].Value is string name) return name;
            }

            return property.Name;
        }

        // [JsonIgnore] IS INHERITED, and it is carried on the abstract declaration rather than on
        // each override: BaseDeviceControlsSettings marks GeneralMode and Device, and the four
        // device classes that override them do not repeat it. Newtonsoft walks the chain, so this
        // has to as well - reading only the override wrote two members no level file has.
        private static bool JsonIgnored(IPropertySymbol property)
        {
            for (var current = property; current != null; current = current.OverriddenProperty)
                if (current.GetAttributes().Any(a => a.AttributeClass?.Name == "JsonIgnoreAttribute"))
                    return true;

            return false;
        }

        private static string KeyProperty(IPropertySymbol property)
        {
            foreach (var attribute in property.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != KeyedAttribute) continue;
                if (attribute.ConstructorArguments.Length == 0) continue;
                if (attribute.ConstructorArguments[0].Value is string name) return name;
            }

            return null;
        }

        private static bool HasAttribute(ISymbol symbol, string metadataName) =>
            symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == metadataName);

        private static string ResolveBaseModel(INamedTypeSymbol type,
            TypeDeclarationSyntax declaration, List<Diagnostic> diagnostics, out bool failed)
        {
            failed = false;

            var baseType = type.BaseType;
            if (baseType is null || baseType.SpecialType == SpecialType.System_Object) return null;
            if (!IsModelReference(baseType)) return null;

            if (!HasAttribute(baseType, GenerateAttribute))
            {
                // Not "generate without the chain": the derived body would then write its own half
                // and silently drop the base's, which is the exact defect being designed out.
                failed = true;
                diagnostics.Add(Diagnostic.Create(ModelDiagnostics.BaseNotGenerated,
                    declaration.Identifier.GetLocation(), type.Name, baseType.Name));
                return null;
            }

            return Qualified(baseType);
        }

        /// <summary> Polymorphic families this type joins and its base does not - each adds a
        /// second, interface-typed copy of the whole contract. Filtering by the base is what stops
        /// a derived type re-implementing an interface its base already answered. </summary>
        private static List<FamilySpec> ResolveFamilies(INamedTypeSymbol type)
        {
            var inherited = type.BaseType is null
                ? new List<INamedTypeSymbol>()
                : type.BaseType.AllInterfaces.ToList();

            return type.AllInterfaces
                .Where(i => IsModelReference(i)
                            && !SymbolEqualityComparer.Default.Equals(i, type)
                            && !inherited.Contains(i, SymbolEqualityComparer.Default))
                .Select(i => new FamilySpec(Qualified(i)))
                .ToList();
        }

        #endregion

        #region Leaves

        /// <summary> Splits a member into the values an ENCODING has to write: itself, its
        /// element, its key. Copy and Equals never need this - they hand a whole collection to a
        /// helper - which is why it is resolved separately rather than folded into MemberShape. </summary>
        private static void Leaves(ITypeSymbol type, MemberShape shape,
            out ValueSpec value, out ValueSpec element, out ValueSpec key)
        {
            value = default;
            element = default;
            key = default;

            switch (shape)
            {
                case MemberShape.Value:
                case MemberShape.Model:
                case MemberShape.PolymorphicModel:
                    value = Leaf(type);
                    return;

                case MemberShape.ModelList:
                case MemberShape.ValueList:
                    element = Leaf(((INamedTypeSymbol)type).TypeArguments[0]);
                    return;

                case MemberShape.ModelArray:
                case MemberShape.UnmanagedArray:
                    element = Leaf(((IArrayTypeSymbol)type).ElementType);
                    return;

                case MemberShape.ModelDictionary:
                case MemberShape.ValueDictionary:
                    var arguments = ((INamedTypeSymbol)type).TypeArguments;
                    key = Leaf(arguments[0]);
                    element = Leaf(arguments[1]);
                    return;
            }
        }

        /// <summary> How ONE value is encoded. </summary>
        private static ValueSpec Leaf(ITypeSymbol type)
        {
            var name = Qualified(type);

            if (type.TypeKind == TypeKind.Enum)
            {
                var underlying = ((INamedTypeSymbol)type).EnumUnderlyingType;
                // Written at the width the enum declares, never widened: a byte enum is one byte,
                // and this format has fifty of them.
                return new ValueSpec(name, ValueKind.Enum, Primitive(underlying).Kind);
            }

            var primitive = Primitive(type);
            if (!primitive.IsNone) return new ValueSpec(name, primitive.Kind);

            var display = type.ToDisplayString();
            if (display == "System.Guid") return new ValueSpec(name, ValueKind.Guid);
            if (display == "System.DateTime") return new ValueSpec(name, ValueKind.DateTime);
            if (display == "System.Version") return new ValueSpec(name, ValueKind.Version);

            if (Implements(type, "IPrimitiveInt"))
                return new ValueSpec(name, ValueKind.PrimitiveInt, ValueKind.None,
                    Accessor(type, SpecialType.System_Int32));
            if (Implements(type, "IPrimitiveGuid"))
                return new ValueSpec(name, ValueKind.PrimitiveGuid, ValueKind.None,
                    Accessor(type, SpecialType.None));
            if (Implements(type, "IPrimitiveFloat"))
                return new ValueSpec(name, ValueKind.PrimitiveFloat, ValueKind.None,
                    Accessor(type, SpecialType.System_Single));

            if (IsModelReference(type))
                return new ValueSpec(name,
                    type.IsSealed ? ValueKind.ModelSealed : ValueKind.ModelPolymorphic,
                    ValueKind.None, string.Empty, LeafVersion(type), LeafFamily(type));

            if (type.IsValueType) return new ValueSpec(name, ValueKind.Struct);

            return default;
        }

        /// <summary> The value family this type belongs to, if any - the interface a converter
        /// matches it by, and therefore the tag it is written behind even when the member declares
        /// the concrete type. </summary>
        private static string LeafFamily(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Interface) return string.Empty;

            foreach (var candidate in type.AllInterfaces)
                if (IsModelReference(candidate))
                    return Qualified(candidate);

            return string.Empty;
        }

        /// <summary> The [DataVersion] a member's own type carries, as it is written. </summary>
        private static string LeafVersion(ITypeSymbol type)
        {
            if (!(type is INamedTypeSymbol named)) return string.Empty;

            var domain = ResolveDomain(named, out var major, out var minor);
            return domain is null ? string.Empty : major + "." + minor;
        }

        private static ValueSpec Primitive(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean: return new ValueSpec(string.Empty, ValueKind.Bool);
                case SpecialType.System_Byte: return new ValueSpec(string.Empty, ValueKind.Byte);
                case SpecialType.System_SByte: return new ValueSpec(string.Empty, ValueKind.SByte);
                case SpecialType.System_Int16: return new ValueSpec(string.Empty, ValueKind.Short);
                case SpecialType.System_UInt16: return new ValueSpec(string.Empty, ValueKind.UShort);
                case SpecialType.System_Int32: return new ValueSpec(string.Empty, ValueKind.Int);
                case SpecialType.System_UInt32: return new ValueSpec(string.Empty, ValueKind.UInt);
                case SpecialType.System_Int64: return new ValueSpec(string.Empty, ValueKind.Long);
                case SpecialType.System_UInt64: return new ValueSpec(string.Empty, ValueKind.ULong);
                case SpecialType.System_Single: return new ValueSpec(string.Empty, ValueKind.Float);
                case SpecialType.System_Double: return new ValueSpec(string.Empty, ValueKind.Double);
                case SpecialType.System_String: return new ValueSpec(string.Empty, ValueKind.String);
                default: return default;
            }
        }

        /// <summary> The unboxed way in to an id's wrapped value: the one public field or
        /// property of the right type. Empty means there is none, and the generated cast to the
        /// interface is then a compile error naming the type rather than a silent boxing loop. </summary>
        private static string Accessor(ITypeSymbol type, SpecialType wrapped)
        {
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public) continue;

                if (member is IFieldSymbol field && Matches(field.Type, wrapped)) return field.Name;
                if (member is IPropertySymbol property && property.GetMethod != null
                    && Matches(property.Type, wrapped))
                    return property.Name;
            }

            return string.Empty;
        }

        private static bool Matches(ITypeSymbol type, SpecialType wrapped) => wrapped == SpecialType.None
            ? type.ToDisplayString() == "System.Guid"
            : type.SpecialType == wrapped;

        private static bool Implements(ITypeSymbol type, string interfaceName) =>
            type.AllInterfaces.Any(i => i.Name == interfaceName
                && i.ContainingNamespace?.ToDisplayString() == "BH.SDK.Models.Interfaces.Primitives");

        #endregion

        /// <summary> Reads the constant GetModelType() answers with. Every implementation in this
        /// format is `=> SomeEnum.Member;`, and the semantic model resolves that to its underlying
        /// number - so the tag is READ from the model rather than assigned by the generator, and a
        /// member that is renumbered moves both encodings at once. A body that is not a constant is
        /// simply not a tag, and the type then cannot be a polymorphic value. </summary>
        private static int ResolveTypeTag(INamedTypeSymbol type)
        {
            foreach (var method in type.GetMembers("GetModelType").OfType<IMethodSymbol>())
            {
                if (method.Parameters.Length != 0 || method.IsStatic) continue;

                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    if (!(reference.GetSyntax() is MethodDeclarationSyntax declaration)) continue;
                    if (declaration.ExpressionBody is null) continue;

                    var name = declaration.ExpressionBody.Expression.ToString();
                    var dot = name.LastIndexOf('.');
                    if (dot < 0) continue;

                    var member = name.Substring(dot + 1);
                    var field = method.ReturnType.GetMembers(member).OfType<IFieldSymbol>()
                        .FirstOrDefault(f => f.HasConstantValue);
                    if (field is null) continue;

                    return System.Convert.ToInt32(field.ConstantValue);
                }
            }

            return NoTypeTag;
        }

        /// <summary> The [DataVersion] this type carries, if any. </summary>
        private static string ResolveDomain(INamedTypeSymbol type, out int major, out int minor)
        {
            major = 0;
            minor = 0;

            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.Name != "DataVersionAttribute") continue;
                if (attribute.ConstructorArguments.Length < 3) continue;

                var domain = attribute.ConstructorArguments[0].Value as string;
                if (domain is null) continue;

                major = System.Convert.ToInt32(attribute.ConstructorArguments[1].Value);
                minor = System.Convert.ToInt32(attribute.ConstructorArguments[2].Value);
                return domain;
            }

            return null;
        }

        #region Names

        public static string Qualified(ITypeSymbol type) =>
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        /// <summary> The generated dispatcher that pulls one value of a non-sealed model type. </summary>
        public static string DispatcherName(ITypeSymbol type) =>
            "global::BH.SDK.Models.Generated." + type.Name + "ModelPull.PullValue";

        private static string HintName(INamedTypeSymbol type) =>
            type.ToDisplayString().Replace('<', '_').Replace('>', '_').Replace(", ", "_")
            + ".Model.g.cs";

        private static Location MemberLocation(IPropertySymbol property, TypeDeclarationSyntax fallback)
            => property.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();

        #endregion
    }
}

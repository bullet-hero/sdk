using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BH.SDK.Roslyn.Modification
{
    // WHAT IMPLEMENTS THIS INTERFACE, answered at compile time because the runtime cannot ask.
    // ModificationUtils walks the model tree by reflection to learn which [JsonProperty] names a
    // type has, and that walk stops dead at an interface: IVector2 declares no serialized property
    // of its own, so nothing below a polymorphic value was ever registered THROUGH it. The gap was
    // papered over by a hand-written list of every implementation in ModificationUtils' static
    // constructor - forty-odd type arguments across eleven families - and it had gone stale exactly
    // as such a list does: Color3Value, Color3ThemeRef, Color3MinMax and both IFontSizeKey and
    // IColor4X4Key implementations were missing, so an override addressed at one of them resolved
    // to nothing and was silently dropped.
    //
    // WHY NOT REFLECTION AT RUNTIME. Scanning the assembly for implementations at static init would
    // close the same hole in ten lines, and it is what the consuming project's ReflectionUtils does.
    // It is refused here for the reason the SDK refuses it everywhere: this assembly is meant to run
    // on a server and under IL2CPP, where a type nothing references statically can be stripped - the
    // scan would then find fewer implementations on the platform that ships than in the Editor, and
    // the failure is the silent one this generator exists to end.
    //
    // WHY NOT THE CONVERTERS' OWN SWITCH. Each of these interfaces already has a JsonConverter whose
    // GetType(enum) names every implementation, which is a real single source of truth and was
    // weighed as one - but it lives in the serialization layer, answers a DIFFERENT question (which
    // type does this tag mean), and reaching it from Utils would make the modification system depend
    // on the converter set. What this needs is the inverse relation, and no code holds it.
    //
    // IT KEYS ON [GenerateModel] rather than scanning every class, which is what keeps it on
    // Roslyn's fast path (ForAttributeWithMetadataName). That is sound because a polymorphic value
    // IS a model - it has to be, to be copied, compared and serialized - and it is CHECKED rather
    // than assumed: BH.SDK.Tests' ModificationRegistryTests reflects over the real assembly and
    // fails if the generated table is missing an implementation reflection can see.

    /// <summary> Writes the interface-to-implementations table ModificationUtils walks through. </summary>
    [Generator]
    public sealed class ModificationRegistryGenerator : IIncrementalGenerator
    {
        internal const string GenerateAttribute = "BH.SDK.Models.Attributes.GenerateModelAttribute";

        // The namespace every polymorphic model interface lives under, and the reason this is a
        // PREFIX test with the root excluded: BH.SDK.Models.Interfaces itself holds IModel,
        // ICopyable, IResetable and the rest of the contracts every model implements, and expanding
        // those would make the table every type against every other. The three sub-namespaces -
        // Values, Effects, Keyframes - are exactly the polymorphic families.
        private const string InterfaceRoot = "BH.SDK.Models.Interfaces";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var pairs = context.SyntaxProvider.ForAttributeWithMetadataName(
                GenerateAttribute,
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, _) => Transform(ctx));

            context.RegisterSourceOutput(pairs.Collect(), static (production, collected) =>
            {
                var table = Merge(collected);
                if (table.Count == 0) return;

                production.AddSource("ModificationImplementations.g.cs",
                    SourceText.From(Emit(table), Encoding.UTF8));
            });
        }

        private static ImplementationSpec Transform(GeneratorAttributeSyntaxContext context)
        {
            if (!(context.TargetSymbol is INamedTypeSymbol type)) return ImplementationSpec.Empty;

            // An abstract type is never the runtime type of a value, so registering it would only
            // add a walk of members its concrete children declare again.
            if (type.IsAbstract || type.IsValueType) return ImplementationSpec.Empty;

            var interfaces = type.AllInterfaces
                .Where(IsPolymorphicFamily)
                .Select(Display)
                .Distinct()
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToImmutableArray();

            return interfaces.Length == 0
                ? ImplementationSpec.Empty
                : new ImplementationSpec(Display(type), interfaces);
        }

        // A generic interface cannot be one of these families - IModel<T> and ICopyable<T> are the
        // shapes that would slip through a namespace test alone, and both are closed out here as
        // well as by the namespace, since a family interface is always non-generic.
        private static bool IsPolymorphicFamily(INamedTypeSymbol symbol)
        {
            if (symbol.IsGenericType) return false;

            var space = symbol.ContainingNamespace?.ToDisplayString();
            if (space is null) return false;

            return space.Length > InterfaceRoot.Length
                   && space.StartsWith(InterfaceRoot + ".", System.StringComparison.Ordinal);
        }

        private static string Display(ISymbol symbol)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Ordered by name on both axes, and that is what makes the output stable: the order specs
        // arrive in is Roslyn's own and changes between builds, so an unsorted table would rewrite
        // the file - and re-trigger every downstream compile - for no change at all.
        private static SortedDictionary<string, SortedSet<string>> Merge(
            ImmutableArray<ImplementationSpec> specs)
        {
            var table = new SortedDictionary<string, SortedSet<string>>(System.StringComparer.Ordinal);

            foreach (var spec in specs)
            {
                if (spec.Type is null) continue;

                foreach (var contract in spec.Interfaces)
                {
                    if (!table.TryGetValue(contract, out var implementations))
                    {
                        implementations = new SortedSet<string>(System.StringComparer.Ordinal);
                        table.Add(contract, implementations);
                    }

                    implementations.Add(spec.Type);
                }
            }

            return table;
        }

        private static string Emit(SortedDictionary<string, SortedSet<string>> table)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("// Written by BH.SDK.Roslyn's ModificationRegistryGenerator. Do not edit.");
            builder.AppendLine();
            builder.AppendLine("namespace BH.SDK.Utils");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary> Every concrete implementation of each polymorphic");
            builder.AppendLine("    /// model interface, so a reflective walk can descend through one. </summary>");
            builder.AppendLine("    public static class ModificationImplementations");
            builder.AppendLine("    {");
            builder.AppendLine("        private static readonly global::System.Type[] None = "
                               + "global::System.Array.Empty<global::System.Type>();");
            builder.AppendLine();
            builder.AppendLine("        /// <summary> The implementations of <paramref name=\"contract\"/>, "
                               + "or an empty array for anything that is not one of the families. </summary>");
            builder.AppendLine("        public static global::System.Type[] Of(global::System.Type contract)");
            builder.AppendLine("        {");

            foreach (var pair in table)
            {
                builder.AppendLine($"            if (contract == typeof({pair.Key}))");
                builder.AppendLine("                return new global::System.Type[]");
                builder.AppendLine("                {");

                foreach (var implementation in pair.Value)
                    builder.AppendLine($"                    typeof({implementation}),");

                builder.AppendLine("                };");
                builder.AppendLine();
            }

            builder.AppendLine("            return None;");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        /// <summary> One model and the family interfaces it implements. </summary>
        private readonly struct ImplementationSpec : System.IEquatable<ImplementationSpec>
        {
            public static readonly ImplementationSpec Empty =
                new ImplementationSpec(null, ImmutableArray<string>.Empty);

            public ImplementationSpec(string type, ImmutableArray<string> interfaces)
            {
                Type = type;
                Interfaces = interfaces;
            }

            public string Type { get; }
            public ImmutableArray<string> Interfaces { get; }

            public bool Equals(ImplementationSpec other)
            {
                if (!string.Equals(Type, other.Type, System.StringComparison.Ordinal)) return false;
                if (Interfaces.Length != other.Interfaces.Length) return false;

                for (var i = 0; i < Interfaces.Length; i++)
                {
                    if (!string.Equals(Interfaces[i], other.Interfaces[i], System.StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj) => obj is ImplementationSpec other && Equals(other);

            public override int GetHashCode() => Type?.GetHashCode() ?? 0;
        }
    }
}

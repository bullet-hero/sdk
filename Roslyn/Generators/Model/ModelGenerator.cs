using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BH.SDK.Roslyn.Model
{
    // WHY A GENERATOR AT ALL. Every model here carried Copy/Clone/Reset/Update/Pull/Equals/
    // GetHashCode by hand - 208 Copy bodies, 279 typed Equals, 206 Update, 206 Pull - and the
    // failure mode is not a compile error but a member quietly missing from one of them. The SDK's
    // own CLAUDE.md names `Equals(object obj) => obj is T value && Equals(value)` the single
    // easiest place in this codebase to introduce a silent bug, because pasting it from a sibling
    // class compiles and only makes the boxed comparison always false.
    //
    // IT REACHES FURTHER THAN THE WHOLE PROJECT AND DECLINES ANYWAY. Unity scopes an analyzer to
    // the asmdef owning its folder plus every assembly referencing it, and BH.SDK is
    // autoReferenced - so this runs on essentially everything. [GenerateModel] is what bounds it:
    // a compilation holding none is one the incremental predicate never matches, at no cost.

    /// <summary> Writes the IModel contract for every [GenerateModel] type. </summary>
    [Generator]
    public sealed class ModelGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var models = context.SyntaxProvider.ForAttributeWithMetadataName(
                ModelSpecFactory.GenerateAttribute,
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, _) => Transform(ctx));

            context.RegisterSourceOutput(models, static (production, result) =>
            {
                foreach (var diagnostic in result.Diagnostics) production.ReportDiagnostic(diagnostic);
                if (result.Spec is null) return;

                production.AddSource(result.Spec.HintName,
                    SourceText.From(ModelEmitter.Emit(result.Spec), Encoding.UTF8));
            });

            // The one output that cannot be per type: which subtypes a base has is a whole-
            // compilation question, and pulling a scope dictionary needs the answer.
            context.RegisterSourceOutput(models.Collect(), static (production, results) =>
                EmitDispatchers(production, results));
        }

        #region Transform

        private static ModelResult Transform(GeneratorAttributeSyntaxContext context)
        {
            var diagnostics = new List<Diagnostic>();

            if (!(context.TargetSymbol is INamedTypeSymbol type)
                || !(context.TargetNode is TypeDeclarationSyntax declaration))
                return new ModelResult(null, ImmutableArray<Diagnostic>.Empty);

            if (type.IsValueType)
            {
                diagnostics.Add(Diagnostic.Create(ModelDiagnostics.ValueTypeModel,
                    declaration.Identifier.GetLocation(), type.Name));
                return new ModelResult(null, diagnostics.ToImmutableArray());
            }

            var spec = ModelSpecFactory.Create(type, declaration, diagnostics);
            return new ModelResult(spec, diagnostics.ToImmutableArray());
        }

        #endregion

        #region Dispatchers

        // A leaf needs a dispatcher when it is reached through a base or an interface - and also
        // when it is a CONCRETE member of a value family, because JSON tags it either way.
        private static void Require(HashSet<string> needed, ValueSpec value)
        {
            if (value.Kind == ValueKind.ModelPolymorphic) needed.Add(value.Type);
            else if (value.Kind == ValueKind.ModelSealed && value.Family.Length > 0) needed.Add(value.Family);
        }

        // The generated replacement for LevelUtils.PullObject - the hand-kept switch its own header
        // calls out as one a new RectObject subtype has to remember to extend. There is nothing to
        // remember now: the cases are whatever [GenerateModel] types derive from the base.

        private static void EmitDispatchers(SourceProductionContext production,
            ImmutableArray<ModelResult> results)
        {
            var specs = results.Select(r => r.Spec).OfType<ModelSpec>().ToList();
            if (specs.Count == 0) return;

            var byQualifiedName = new Dictionary<string, ModelSpec>();
            foreach (var spec in specs) byQualifiedName[spec.QualifiedName] = spec;

            // A polymorphic ROOT is anything a member can be declared as while holding one of
            // several concrete types: a base class with subtypes, or one of the value families'
            // interfaces. Both need the same two answers - which tag, and which constructor.
            var roots = new Dictionary<string, List<ModelSpec>>();

            void Add(string root, ModelSpec spec)
            {
                if (!roots.TryGetValue(root, out var list))
                {
                    list = new List<ModelSpec>();
                    roots[root] = list;
                }

                list.Add(spec);
            }

            foreach (var spec in specs)
            {
                if (spec.BaseModel != null) Add(spec.BaseModel, spec);
                foreach (var family in spec.Families) Add(family.InterfaceType, spec);
            }

            if (roots.Count == 0) return;

            // A DISPATCHER IS ONLY NEEDED WHERE SOMETHING IS ACTUALLY READ THROUGH A BASE. Most of
            // these hierarchies are never met polymorphically: every keyframe member is a
            // List<PosKey>, every audio effect is its own concrete field, every resource dictionary
            // is typed by its subtype. Only RectObject and the value-family interfaces are, and
            // demanding a discriminator from the rest would be demanding it for nothing.
            var needed = new HashSet<string>();
            foreach (var spec in specs)
            foreach (var member in spec.Members)
            {
                Require(needed, member.Value);
                Require(needed, member.Element);
                Require(needed, member.Key);
            }

            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("// Written by BH.SDK.Roslyn's ModelGenerator. Do not edit.");
            builder.AppendLine();
            builder.AppendLine("namespace BH.SDK.Models.Generated");
            builder.AppendLine("{");

            foreach (var pair in roots.OrderBy(p => p.Key))
            {
                var root = pair.Key;
                var name = SimpleName(root);
                var derived = pair.Value.OrderBy(s => s.QualifiedName).ToList();
                byQualifiedName.TryGetValue(root, out var rootSpec);

                EmitPullDispatcher(builder, root, name, derived, rootSpec);
                if (needed.Contains(root))
                {
                    EmitBlobDispatcher(production, builder, root, name, derived, rootSpec);
                    EmitJsonDispatcher(builder, root, name, derived, rootSpec);
                }
            }

            builder.AppendLine("}");

            production.AddSource("ModelDispatchers.g.cs",
                SourceText.From(builder.ToString(), Encoding.UTF8));
        }

        // The generated replacement for LevelUtils.PullObject - the hand-kept switch its own header
        // called out as one a new RectObject subtype had to remember to extend. There is nothing to
        // remember now: the cases are whatever [GenerateModel] types derive from the base.
        private static void EmitPullDispatcher(StringBuilder builder, string root, string name,
            List<ModelSpec> derived, ModelSpec rootSpec)
        {
            // A family interface has no Pull of its own to merge into - ModelUtils.PullFrom is what
            // a polymorphic FIELD goes through - so only a class hierarchy gets one.
            if (rootSpec is null || rootSpec.QualifiedName != root) return;

            builder.AppendLine("    /// <summary> Merges one " + name + " into another while their");
            builder.AppendLine("    /// concrete types agree, and returns what the scope must now hold. </summary>");
            builder.AppendLine("    internal static class " + name + "ModelPull");
            builder.AppendLine("    {");
            builder.AppendLine("        public static " + root + " PullValue(" + root + " target, " + root +
                               " source)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (source is null) return null;");
            builder.AppendLine(
                "            // Identity cannot survive a type change, so the field takes a copy instead.");
            builder.AppendLine(
                "            if (target is null || target.GetType() != source.GetType()) return source.Copy();");
            builder.AppendLine();
            builder.AppendLine("            switch (target)");
            builder.AppendLine("            {");

            foreach (var spec in derived)
                if (spec.BaseModel == root)
                    builder.AppendLine("                case " + spec.QualifiedName + " typed: typed.Pull(("
                                       + spec.QualifiedName + ")source); break;");

            builder.AppendLine("                // The base type itself - Pull through it is total for its own half.");
            builder.AppendLine("                default: target.Pull(source); break;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return target;");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        // WHAT MAKES A POLYMORPHIC VALUE READABLE. The writer knows the concrete type and the
        // reader does not, so a tag goes first - and the tag is the model's OWN GetModelType(),
        // read out of the enum member that method names, never a second numbering invented here.
        // 0xFF is null, which is why no discriminator in this format may reach 255.
        private static void EmitBlobDispatcher(SourceProductionContext production, StringBuilder builder,
            string root, string name, List<ModelSpec> derived, ModelSpec rootSpec)
        {
            // EVERY candidate needs a tag or NONE of them can be read back, so an untagged one
            // is an error naming it rather than a dispatcher quietly missing a case. Without this
            // the emission compiles everywhere except at the one member that reaches the missing
            // class, which is a confusing way to say "this type has no discriminator".
            var untagged = derived.Where(s => s.TypeTag < 0).ToList();
            if (rootSpec != null && !rootSpec.IsAbstract && rootSpec.TypeTag < 0) untagged.Add(rootSpec);

            if (untagged.Count > 0)
            {
                foreach (var spec in untagged)
                    production.ReportDiagnostic(Diagnostic.Create(ModelDiagnostics.NoTypeTag,
                        Location.None, spec.Name, name));
                return;
            }

            var cases = derived.ToList();
            if (rootSpec != null && !rootSpec.IsAbstract) cases.Add(rootSpec);
            if (cases.Count == 0) return;

            foreach (var spec in cases)
            {
                if (spec.TypeTag <= byte.MaxValue - 1) continue;
                production.ReportDiagnostic(Diagnostic.Create(ModelDiagnostics.TagOutOfRange,
                    Location.None, spec.Name, spec.TypeTag));
                return;
            }

            builder.AppendLine("    /// <summary> The tag-and-payload encoding of a " + name + ". </summary>");
            builder.AppendLine("    internal static class " + name + "Blob");
            builder.AppendLine("    {");
            builder.AppendLine("        private const byte NullTag = 0xFF;");
            builder.AppendLine();
            builder.AppendLine(
                "        public static void Write(ref global::BH.SDK.Serialization.Blob.BlobWriter writer, "
                + root + " value)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (value is null) { writer.WriteByte(NullTag); return; }");
            builder.AppendLine();
            builder.AppendLine("            switch (value)");
            builder.AppendLine("            {");

            // The base type matches every subtype, so its own case has to come last.
            foreach (var spec in cases.Where(s => s.QualifiedName != root)
                         .Concat(cases.Where(s => s.QualifiedName == root)))
            {
                builder.AppendLine("                case " + spec.QualifiedName + " typed:");
                builder.AppendLine("                    writer.WriteByte(" + spec.TypeTag + ");");
                builder.AppendLine("                    typed.Write(ref writer);");
                builder.AppendLine("                    return;");
            }

            builder.AppendLine("                default:");
            builder.AppendLine("                    throw new global::BH.SDK.Serialization.Blob.BlobFormatException(");
            builder.AppendLine("                        $\"{value.GetType().Name} is not a known " + name + "\");");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public static " + root +
                               " Read(ref global::BH.SDK.Serialization.Blob.BlobReader reader)");
            builder.AppendLine("        {");
            builder.AppendLine("            var tag = reader.ReadByte();");
            builder.AppendLine("            if (tag == NullTag) return null;");
            builder.AppendLine();
            // Declared as IBinaryModel rather than as the root: a FAMILY root is an interface
            // that knows nothing about bytes, and it must not - IModel is what a model is to the
            // program, IBinaryModel is what it is to a file, and the whole point of the split is
            // that neither drags the other in.
            builder.AppendLine("            global::BH.SDK.Serialization.Blob.IBinaryModel value;");
            builder.AppendLine("            switch (tag)");
            builder.AppendLine("            {");

            foreach (var spec in cases.OrderBy(s => s.TypeTag))
                builder.AppendLine("                case " + spec.TypeTag + ": value = new "
                                   + spec.QualifiedName + "(); break;");

            builder.AppendLine("                default:");
            builder.AppendLine("                    throw new global::BH.SDK.Serialization.Blob.BlobFormatException(");
            builder.AppendLine("                        $\"{tag} is not a known " + name + "\");");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            value.Read(ref reader);");
            builder.AppendLine("            return (" + root + ")value;");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        // THE SAME QUESTION THE BLOB ASKS, IN THE FORMAT THAT ALREADY SHIPPED. A polymorphic value
        // is a two-element ARRAY - the tag then the payload - which is what every level file on
        // disk carries (`"clr":[0,{...}]`), and the tag is the same GetModelType() the blob uses.
        // The reader accepts the tag as a NAME as well as a number, because the converter this
        // replaces went through Deserialize and would have taken either.
        private static void EmitJsonDispatcher(StringBuilder builder, string root, string name,
            List<ModelSpec> derived, ModelSpec rootSpec)
        {
            var cases = derived.ToList();
            if (rootSpec != null && !rootSpec.IsAbstract) cases.Add(rootSpec);
            if (cases.Count == 0) return;

            builder.AppendLine("    /// <summary> The [tag, payload] encoding of a " + name + ". </summary>");
            builder.AppendLine("    internal static class " + name + "Json");
            builder.AppendLine("    {");
            builder.AppendLine("        public static void Write(global::Newtonsoft.Json.JsonWriter writer, "
                               + root + " value)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (value is null) { writer.WriteNull(); return; }");
            builder.AppendLine();
            builder.AppendLine("            writer.WriteStartArray();");
            builder.AppendLine("            switch (value)");
            builder.AppendLine("            {");

            foreach (var spec in cases.Where(s => s.QualifiedName != root)
                         .Concat(cases.Where(s => s.QualifiedName == root)))
            {
                builder.AppendLine("                case " + spec.QualifiedName + " typed:");
                builder.AppendLine("                    writer.WriteValue(" + spec.TypeTag + ");");
                builder.AppendLine("                    typed.WriteJson(writer);");
                builder.AppendLine("                    break;");
            }

            builder.AppendLine("                default:");
            builder.AppendLine("                    throw new global::Newtonsoft.Json.JsonSerializationException(");
            builder.AppendLine("                        $\"{value.GetType().Name} is not a known " + name + "\");");
            builder.AppendLine("            }");
            builder.AppendLine("            writer.WriteEndArray();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public static " + root + " Read(global::Newtonsoft.Json.JsonReader reader)");
            builder.AppendLine("        {");
            builder.AppendLine(
                "            if (reader.TokenType == global::Newtonsoft.Json.JsonToken.Null) return null;");
            builder.AppendLine("            if (reader.TokenType != global::Newtonsoft.Json.JsonToken.StartArray)");
            builder.AppendLine("                throw new global::Newtonsoft.Json.JsonSerializationException(");
            builder.AppendLine("                    $\"Expected a tagged " + name + ", found {reader.TokenType}\");");
            builder.AppendLine();
            builder.AppendLine("            reader.Read();");
            builder.AppendLine("            var tag = ReadTag(reader);");
            builder.AppendLine();
            builder.AppendLine("            global::BH.SDK.Serialization.Json.IJsonModel value;");
            builder.AppendLine("            switch (tag)");
            builder.AppendLine("            {");

            foreach (var spec in cases.OrderBy(s => s.TypeTag))
                builder.AppendLine("                case " + spec.TypeTag + ": value = new "
                                   + spec.QualifiedName + "(); break;");

            builder.AppendLine("                default:");
            builder.AppendLine("                    throw new global::Newtonsoft.Json.JsonSerializationException(");
            builder.AppendLine("                        $\"{tag} is not a known " + name + "\");");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            reader.Read();");
            builder.AppendLine("            value.ReadJson(reader);");
            builder.AppendLine("            reader.Read();");
            builder.AppendLine("            return (" + root + ")value;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        /// <summary> The tag, as a number or as the enum member's name. </summary>");
            builder.AppendLine("        private static int ReadTag(global::Newtonsoft.Json.JsonReader reader)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (reader.Value is string text)");
            builder.AppendLine("            {");

            foreach (var spec in cases.OrderBy(s => s.TypeTag))
                builder.AppendLine("                if (string.Equals(text, \"" + spec.Name
                    + "\", global::System.StringComparison.OrdinalIgnoreCase)) return "
                    + spec.TypeTag + ";");

            builder.AppendLine("                throw new global::Newtonsoft.Json.JsonSerializationException(");
            builder.AppendLine("                    $\"'{text}' is not a known " + name + "\");");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return global::System.Convert.ToInt32(reader.Value);");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        private static string SimpleName(string qualified)
        {
            var index = qualified.LastIndexOf('.');
            return index < 0 ? qualified : qualified.Substring(index + 1);
        }

        #endregion

        /// <summary> One transform's answer. Diagnostics travel with the spec so a refusal survives
        /// the incremental cache instead of appearing only on a cold build. </summary>
        private sealed class ModelResult : System.IEquatable<ModelResult>
        {
            public ModelResult(ModelSpec spec, ImmutableArray<Diagnostic> diagnostics)
            {
                Spec = spec;
                Diagnostics = diagnostics;
            }

            public ModelSpec Spec { get; }
            public ImmutableArray<Diagnostic> Diagnostics { get; }

            public bool Equals(ModelResult other)
            {
                if (other is null) return false;
                if (!(Spec is null ? other.Spec is null : Spec.Equals(other.Spec))) return false;
                if (Diagnostics.Length != other.Diagnostics.Length) return false;
                for (var i = 0; i < Diagnostics.Length; i++)
                    if (!Diagnostics[i].Equals(other.Diagnostics[i]))
                        return false;
                return true;
            }

            public override bool Equals(object obj) => obj is ModelResult other && Equals(other);

            public override int GetHashCode() => Spec?.GetHashCode() ?? 0;
        }
    }
}
using System.Text;

namespace BH.SDK.Roslyn.Model
{
    // THE BLOB HALF. Its conventions are the level cache's codec, which was written out by hand for
    // this same model and proved the shape works - strict write/read pairs in member order, a
    // length prefix of -1 for null (an empty keyframe list and a missing one are different states
    // and a round trip has to keep them apart), a one-byte tag for a polymorphic value with 0xFF
    // reserved for null. What is new is what a FORMAT needs and a cache did not: a per-domain
    // envelope carrying its own version, and a length that the reader checks the content against.
    //
    // THE TAG IS THE MODEL'S OWN. GetModelType() is already the discriminator the JSON side writes,
    // so the blob reuses it rather than inventing a second numbering that could drift from it. The
    // generator READS it out of the enum member the method names.

    /// <summary> Writes the IBinaryModel half of a model. </summary>
    internal static class ModelBlobEmitter
    {
        private const string Blob = "global::BH.SDK.Serialization.Blob";
        private const string Primitives = Blob + ".BlobPrimitives";

        public static void Emit(StringBuilder builder, string indent, ModelSpec spec)
        {
            builder.Append(indent).AppendLine("#region Generated blob codec");
            builder.AppendLine();
            builder.Append(indent).Append("partial void OnWriteBlob(ref ").Append(Blob).AppendLine(".BlobWriter writer);");
            builder.Append(indent).Append("partial void OnReadBlob(ref ").Append(Blob).AppendLine(".BlobReader reader);");
            builder.AppendLine();

            EmitWrite(builder, indent, spec);
            EmitRead(builder, indent, spec);

            builder.Append(indent).AppendLine("#endregion");
        }

        #region Write

        private static void EmitWrite(StringBuilder builder, string indent, ModelSpec spec)
        {
            var modifier = spec.IsAbstract ? "abstract " : Modifier(spec);

            if (spec.IsAbstract)
            {
                builder.Append(indent).Append("public abstract void Write(ref ").Append(Blob)
                    .AppendLine(".BlobWriter writer);");
                builder.AppendLine();
                EmitWriteHelper(builder, indent, spec);
                return;
            }

            builder.Append(indent).AppendLine("/// <summary> Writes this model, envelope included. </summary>");
            builder.Append(indent).Append("public ").Append(modifier).Append("void Write(ref ")
                .Append(Blob).AppendLine(".BlobWriter writer)");
            builder.Append(indent).AppendLine("{");

            if (spec.Domain != null)
            {
                builder.Append(indent).AppendLine("    // An aggregate root carries its own version, exactly as it does in");
                builder.Append(indent).AppendLine("    // JSON, and a length so a reader can tell a short payload from a");
                builder.Append(indent).AppendLine("    // wrong one. The domain is written as text rather than as a number:");
                builder.Append(indent).AppendLine("    // a numbering would be a second registry to keep in step with");
                builder.Append(indent).AppendLine("    // DataDomains, and the bytes it saves are a rounding error.");
                builder.Append(indent).Append("    writer.WriteString(\"").Append(spec.Domain).AppendLine("\");");
                builder.Append(indent).Append("    writer.WriteUShort(").Append(spec.Major).AppendLine(");");
                builder.Append(indent).Append("    writer.WriteUShort(").Append(spec.Minor).AppendLine(");");
                builder.Append(indent).AppendLine("    var lengthSlot = writer.ReserveInt();");
                builder.Append(indent).AppendLine("    var contentStart = writer.Length;");
                builder.Append(indent).Append("    WriteBlob").Append(spec.Name).AppendLine("(ref writer);");
                builder.Append(indent).AppendLine("    writer.PatchInt(lengthSlot, writer.Length - contentStart);");
            }
            else
            {
                builder.Append(indent).Append("    WriteBlob").Append(spec.Name).AppendLine("(ref writer);");
            }

            builder.Append(indent).AppendLine("}");
            builder.AppendLine();

            EmitWriteHelper(builder, indent, spec);
        }

        private static void EmitWriteHelper(StringBuilder builder, string indent, ModelSpec spec)
        {
            builder.Append(indent).Append(HelperAccess(spec)).Append("void WriteBlob").Append(spec.Name)
                .Append("(ref ").Append(Blob).AppendLine(".BlobWriter writer)");
            builder.Append(indent).AppendLine("{");

            if (spec.BaseModel != null)
                builder.Append(indent).Append("    WriteBlob").Append(SimpleName(spec.BaseModel))
                    .AppendLine("(ref writer);");

            foreach (var member in spec.Members)
            {
                if (!member.Assignable) continue;
                WriteMember(builder, indent + "    ", member);
            }

            builder.Append(indent).AppendLine("    OnWriteBlob(ref writer);");
            builder.Append(indent).AppendLine("}");
            builder.AppendLine();
        }

        private static void WriteMember(StringBuilder builder, string indent, MemberSpec member)
        {
            switch (member.Shape)
            {
                case MemberShape.Value:
                case MemberShape.Model:
                case MemberShape.PolymorphicModel:
                    builder.Append(indent).AppendLine(WriteValue(member.Value, member.Name));
                    return;

                case MemberShape.ModelList:
                case MemberShape.ValueList:
                    EmitCountedWrite(builder, indent, member, member.Name + ".Count", "item",
                        member.Name);
                    return;

                case MemberShape.ModelArray:
                case MemberShape.UnmanagedArray:
                    EmitCountedWrite(builder, indent, member, member.Name + ".Length", "item",
                        member.Name);
                    return;

                case MemberShape.ModelDictionary:
                case MemberShape.ValueDictionary:
                    builder.Append(indent).Append("if (").Append(member.Name).AppendLine(" is null)");
                    builder.Append(indent).Append("    writer.WriteInt(").Append(Blob)
                        .AppendLine(".BlobWriter.NullLength);");
                    builder.Append(indent).AppendLine("else");
                    builder.Append(indent).AppendLine("{");
                    builder.Append(indent).Append("    writer.WriteInt(").Append(member.Name).AppendLine(".Count);");
                    builder.Append(indent).Append("    foreach (var pair in ").Append(member.Name).AppendLine(")");
                    builder.Append(indent).AppendLine("    {");
                    builder.Append(indent).Append("        ").AppendLine(WriteValue(member.Key, "pair.Key"));
                    builder.Append(indent).Append("        ").AppendLine(WriteValue(member.Element, "pair.Value"));
                    builder.Append(indent).AppendLine("    }");
                    builder.Append(indent).AppendLine("}");
                    return;
            }
        }

        private static void EmitCountedWrite(StringBuilder builder, string indent, MemberSpec member,
            string count, string item, string collection)
        {
            builder.Append(indent).Append("if (").Append(collection).AppendLine(" is null)");
            builder.Append(indent).Append("    writer.WriteInt(").Append(Blob)
                .AppendLine(".BlobWriter.NullLength);");
            builder.Append(indent).AppendLine("else");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).Append("    writer.WriteInt(").Append(count).AppendLine(");");
            builder.Append(indent).Append("    foreach (var ").Append(item).Append(" in ")
                .Append(collection).AppendLine(")");
            builder.Append(indent).Append("        ").AppendLine(WriteValue(member.Element, item));
            builder.Append(indent).AppendLine("}");
        }

        /// <summary> One value, written. </summary>
        private static string WriteValue(ValueSpec value, string access)
        {
            switch (value.Kind)
            {
                case ValueKind.Bool: return "writer.WriteBool(" + access + ");";
                case ValueKind.Byte: return "writer.WriteByte(" + access + ");";
                case ValueKind.SByte: return "writer.WriteByte(unchecked((byte)" + access + "));";
                case ValueKind.Short: return "writer.WriteShort(" + access + ");";
                case ValueKind.UShort: return "writer.WriteUShort(" + access + ");";
                case ValueKind.Int: return "writer.WriteInt(" + access + ");";
                case ValueKind.UInt: return "writer.WriteUInt(" + access + ");";
                case ValueKind.Long: return "writer.WriteLong(" + access + ");";
                case ValueKind.ULong: return "writer.WriteULong(" + access + ");";
                case ValueKind.Float: return "writer.WriteFloat(" + access + ");";
                case ValueKind.Double: return "writer.WriteDouble(" + access + ");";
                case ValueKind.String: return "writer.WriteString(" + access + ");";
                case ValueKind.Guid: return "writer.WriteGuid(" + access + ");";
                case ValueKind.DateTime: return "writer.WriteDateTime(" + access + ");";
                // Immutable and rarely present, so the text it round-trips through is exact and
                // costs nothing worth optimizing.
                case ValueKind.Version: return "writer.WriteString(" + access + "?.ToString());";
                case ValueKind.Enum:
                    return WriteValue(new ValueSpec(string.Empty, value.Underlying),
                        "(" + UnderlyingName(value.Underlying) + ")" + access);
                case ValueKind.PrimitiveInt: return "writer.WriteInt(" + access + "." + value.Accessor + ");";
                case ValueKind.PrimitiveGuid: return "writer.WriteGuid(" + access + "." + value.Accessor + ");";
                case ValueKind.PrimitiveFloat: return "writer.WriteFloat(" + access + "." + value.Accessor + ");";
                case ValueKind.Struct: return Primitives + ".Write(ref writer, " + access + ");";
                case ValueKind.ModelSealed:
                    // No tag: the declared type is the only thing it can be. One presence byte,
                    // because a model member is legitimately null all over this format.
                    return "if (" + access + " is null) writer.WriteBool(false); else { writer.WriteBool(true); "
                           + access + ".Write(ref writer); }";
                case ValueKind.ModelPolymorphic:
                    return BlobDispatcher(value.Type) + ".Write(ref writer, " + access + ");";
                default:
                    return "// unencodable";
            }
        }

        #endregion

        #region Read

        private static void EmitRead(StringBuilder builder, string indent, ModelSpec spec)
        {
            if (spec.IsAbstract)
            {
                builder.Append(indent).Append("public abstract void Read(ref ").Append(Blob)
                    .AppendLine(".BlobReader reader);");
                builder.AppendLine();
                EmitReadHelper(builder, indent, spec);
                return;
            }

            builder.Append(indent).AppendLine("/// <summary> Reads this model back over itself. </summary>");
            builder.Append(indent).Append("public ").Append(Modifier(spec)).Append("void Read(ref ")
                .Append(Blob).AppendLine(".BlobReader reader)");
            builder.Append(indent).AppendLine("{");

            if (spec.Domain != null)
            {
                builder.Append(indent).AppendLine("    var domain = reader.ReadString();");
                builder.Append(indent).AppendLine("    var major = reader.ReadUShort();");
                builder.Append(indent).AppendLine("    var minor = reader.ReadUShort();");
                builder.Append(indent).AppendLine("    var length = reader.ReadInt();");
                builder.Append(indent).Append("    if (domain != \"").Append(spec.Domain).AppendLine("\")");
                builder.Append(indent).Append("        throw new ").Append(Blob)
                    .Append(".BlobFormatException($\"expected domain '").Append(spec.Domain)
                    .AppendLine("', found '{domain}'\");");
                builder.Append(indent).AppendLine("    // A version tag is written on every envelope so a future generation");
                builder.Append(indent).AppendLine("    // CAN be migrated. None can exist yet - no build has ever written a");
                builder.Append(indent).AppendLine("    // .blob - so an unknown one is refused rather than guessed at, and");
                builder.Append(indent).AppendLine("    // the .json beside it is the recovery path.");
                builder.Append(indent).Append("    if (major != ").Append(spec.Major)
                    .Append(" || minor != ").Append(spec.Minor).AppendLine(")");
                builder.Append(indent).Append("        throw new ").Append(Blob)
                    .Append(".BlobFormatException($\"").Append(spec.Domain)
                    .Append(" is version {major}.{minor}, this build reads ").Append(spec.Major)
                    .Append('.').Append(spec.Minor).AppendLine("\");");
                builder.Append(indent).AppendLine("    var contentStart = reader.Position;");
                builder.Append(indent).Append("    ReadBlob").Append(spec.Name).AppendLine("(ref reader);");
                builder.Append(indent).AppendLine("    if (reader.Position - contentStart != length)");
                builder.Append(indent).Append("        throw new ").Append(Blob)
                    .Append(".BlobFormatException(\"").Append(spec.Domain)
                    .AppendLine(" read a different number of bytes than it declared\");");
            }
            else
            {
                builder.Append(indent).Append("    ReadBlob").Append(spec.Name).AppendLine("(ref reader);");
            }

            builder.Append(indent).AppendLine("}");
            builder.AppendLine();

            EmitReadHelper(builder, indent, spec);
        }

        private static void EmitReadHelper(StringBuilder builder, string indent, ModelSpec spec)
        {
            builder.Append(indent).Append(HelperAccess(spec)).Append("void ReadBlob").Append(spec.Name)
                .Append("(ref ").Append(Blob).AppendLine(".BlobReader reader)");
            builder.Append(indent).AppendLine("{");

            if (spec.BaseModel != null)
                builder.Append(indent).Append("    ReadBlob").Append(SimpleName(spec.BaseModel))
                    .AppendLine("(ref reader);");

            foreach (var member in spec.Members)
            {
                if (!member.Assignable) continue;
                ReadMember(builder, indent + "    ", member);
            }

            builder.Append(indent).AppendLine("    OnReadBlob(ref reader);");
            builder.Append(indent).AppendLine("}");
            builder.AppendLine();
        }

        private static void ReadMember(StringBuilder builder, string indent, MemberSpec member)
        {
            switch (member.Shape)
            {
                case MemberShape.Value:
                case MemberShape.Model:
                case MemberShape.PolymorphicModel:
                    builder.Append(indent).Append(member.Name).Append(" = ")
                        .Append(ReadValue(member.Value)).AppendLine(";");
                    return;

                case MemberShape.ModelList:
                case MemberShape.ValueList:
                    EmitCountedRead(builder, indent, member,
                        "new " + member.Type + "(count)", "add", ".Add(" + ReadValue(member.Element) + ");");
                    return;

                case MemberShape.ModelArray:
                case MemberShape.UnmanagedArray:
                    EmitCountedRead(builder, indent, member,
                        "new " + ElementArray(member) + "[count]", "index",
                        "[i] = " + ReadValue(member.Element) + ";");
                    return;

                case MemberShape.ModelDictionary:
                case MemberShape.ValueDictionary:
                    builder.Append(indent).Append("{");
                    builder.AppendLine();
                    builder.Append(indent).Append("    var count = reader.ReadCount(")
                        .Append(Stride(member.Key) + Stride(member.Element)).AppendLine(");");
                    builder.Append(indent).Append("    if (count == ").Append(Blob)
                        .Append(".BlobWriter.NullLength) ").Append(member.Name).AppendLine(" = null;");
                    builder.Append(indent).AppendLine("    else");
                    builder.Append(indent).AppendLine("    {");
                    builder.Append(indent).Append("        var map = new ").Append(member.Type).AppendLine("(count);");
                    builder.Append(indent).AppendLine("        for (var i = 0; i < count; i++)");
                    builder.Append(indent).AppendLine("        {");
                    builder.Append(indent).Append("            var key = ").Append(ReadValue(member.Key)).AppendLine(";");
                    builder.Append(indent).Append("            map[key] = ").Append(ReadValue(member.Element)).AppendLine(";");
                    builder.Append(indent).AppendLine("        }");
                    builder.Append(indent).Append("        ").Append(member.Name).AppendLine(" = map;");
                    builder.Append(indent).AppendLine("    }");
                    builder.Append(indent).AppendLine("}");
                    return;
            }
        }

        private static void EmitCountedRead(StringBuilder builder, string indent, MemberSpec member,
            string construct, string mode, string tail)
        {
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).Append("    var count = reader.ReadCount(").Append(Stride(member.Element)).AppendLine(");");
            builder.Append(indent).Append("    if (count == ").Append(Blob)
                .Append(".BlobWriter.NullLength) ").Append(member.Name).AppendLine(" = null;");
            builder.Append(indent).AppendLine("    else");
            builder.Append(indent).AppendLine("    {");
            builder.Append(indent).Append("        var items = ").Append(construct).AppendLine(";");
            builder.Append(indent).AppendLine("        for (var i = 0; i < count; i++)");
            builder.Append(indent).Append("            items").AppendLine(tail);
            builder.Append(indent).Append("        ").Append(member.Name).AppendLine(" = items;");
            builder.Append(indent).AppendLine("    }");
            builder.Append(indent).AppendLine("}");
        }

        /// <summary> One value, read. </summary>
        private static string ReadValue(ValueSpec value)
        {
            switch (value.Kind)
            {
                case ValueKind.Bool: return "reader.ReadBool()";
                case ValueKind.Byte: return "reader.ReadByte()";
                case ValueKind.SByte: return "unchecked((sbyte)reader.ReadByte())";
                case ValueKind.Short: return "reader.ReadShort()";
                case ValueKind.UShort: return "reader.ReadUShort()";
                case ValueKind.Int: return "reader.ReadInt()";
                case ValueKind.UInt: return "reader.ReadUInt()";
                case ValueKind.Long: return "reader.ReadLong()";
                case ValueKind.ULong: return "reader.ReadULong()";
                case ValueKind.Float: return "reader.ReadFloat()";
                case ValueKind.Double: return "reader.ReadDouble()";
                case ValueKind.String: return "reader.ReadString()";
                case ValueKind.Guid: return "reader.ReadGuid()";
                case ValueKind.DateTime: return "reader.ReadDateTime()";
                case ValueKind.Version: return Blob + ".BlobVersions.Read(ref reader)";
                case ValueKind.Enum:
                    return "(" + value.Type + ")"
                           + ReadValue(new ValueSpec(string.Empty, value.Underlying));
                case ValueKind.PrimitiveInt: return "new " + value.Type + "(reader.ReadInt())";
                case ValueKind.PrimitiveGuid: return "new " + value.Type + "(reader.ReadGuid())";
                case ValueKind.PrimitiveFloat: return "new " + value.Type + "(reader.ReadFloat())";
                case ValueKind.Struct:
                    return Primitives + ".Read" + SimpleName(value.Type) + "(ref reader)";
                case ValueKind.ModelSealed:
                    return Blob + ".BlobModels.Read<" + value.Type + ">(ref reader)";
                case ValueKind.ModelPolymorphic:
                    return BlobDispatcher(value.Type) + ".Read(ref reader)";
                default:
                    return "default";
            }
        }

        #endregion

        #region Naming

        /// <summary> The smallest number of bytes one value of this kind can occupy - what makes a
        /// hostile count cheap to disprove before anything is allocated. </summary>
        private static int Stride(ValueSpec value)
        {
            switch (value.Kind)
            {
                case ValueKind.Bool:
                case ValueKind.Byte:
                case ValueKind.SByte:
                    return 1;
                case ValueKind.Short:
                case ValueKind.UShort:
                    return 2;
                case ValueKind.Enum:
                    return Stride(new ValueSpec(string.Empty, value.Underlying));
                case ValueKind.Long:
                case ValueKind.ULong:
                case ValueKind.Double:
                case ValueKind.DateTime:
                    return 8;
                case ValueKind.Guid:
                case ValueKind.PrimitiveGuid:
                    return 16;
                case ValueKind.None:
                    return 1;
                default:
                    // Everything else is at least a four-byte prefix or value; a model is at least
                    // its presence byte, and claiming more would refuse a legal empty one.
                    return value.Kind == ValueKind.ModelSealed || value.Kind == ValueKind.ModelPolymorphic
                        ? 1
                        : 4;
            }
        }

        /// <summary> The C# keyword an enum's underlying type is spelled with - what the cast in
        /// front of a write has to say. </summary>
        private static string UnderlyingName(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Byte: return "byte";
                case ValueKind.SByte: return "sbyte";
                case ValueKind.Short: return "short";
                case ValueKind.UShort: return "ushort";
                case ValueKind.UInt: return "uint";
                case ValueKind.Long: return "long";
                case ValueKind.ULong: return "ulong";
                default: return "int";
            }
        }

        private static string ElementArray(MemberSpec member)
        {
            var type = member.Type;
            return type.EndsWith("[]") ? type.Substring(0, type.Length - 2) : type;
        }

        public static string BlobDispatcher(string type)
            => "global::BH.SDK.Models.Generated." + SimpleName(type) + "Blob";

        private static string Modifier(ModelSpec spec)
        {
            if (spec.BaseModel != null) return "override ";
            return spec.IsSealed ? string.Empty : "virtual ";
        }

        private static string HelperAccess(ModelSpec spec) => spec.IsSealed ? "private " : "protected ";

        private static string SimpleName(string qualified)
        {
            var index = qualified.LastIndexOf('.');
            return index < 0 ? qualified : qualified.Substring(index + 1);
        }

        #endregion
    }
}

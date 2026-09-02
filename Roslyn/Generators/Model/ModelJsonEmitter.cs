using System.Text;

namespace BH.SDK.Roslyn.Model
{
    // THE JSON HALF, AND ITS ONLY ACCEPTANCE TEST IS BYTE IDENTITY. Everything here reproduces a
    // format that is already on players' disks, so "cleaner" is never an argument: the shapes below
    // are what Newtonsoft's contract plus this project's thirty-five converters already write, and
    // any difference is a level that stops loading somewhere.
    //
    // TWO THINGS ARE THE OPPOSITE OF THE BLOB'S, and both were measured off a real file rather than
    // reasoned about. Members are written DERIVED FIRST, THEN BASE - Newtonsoft's contract collects
    // the most-derived type's members ahead of its base's, which is why a ShapeObject reads
    // `shid, cid, ... , id, pid, name` and not the other way round. And a GET-ONLY property is
    // written even though nothing can read it back: the resolver sets MemberSerialization after the
    // properties are already collected, so the contract is OptOut in practice, and Resource.Type
    // rides on that. Dropping either would change the wire format silently.
    //
    // READING IS ORDER-INDEPENDENT, deliberately. A hand-edited file, a file from another tool and
    // a file this build wrote all have to work, so the reader switches on the property name and
    // skips whatever it does not know - which is also what makes an additive member free.

    /// <summary> Writes the IJsonModel half of a model. </summary>
    internal static class ModelJsonEmitter
    {
        private const string Json = "global::BH.SDK.Serialization.Json";
        private const string Primitives = Json + ".JsonPrimitives";
        private const string Writer = "global::Newtonsoft.Json.JsonWriter";
        private const string Reader = "global::Newtonsoft.Json.JsonReader";

        public static void Emit(StringBuilder builder, string indent, ModelSpec spec)
        {
            builder.Append(indent).AppendLine("#region Generated json codec");
            builder.AppendLine();

            EmitWrite(builder, indent, spec);
            EmitRead(builder, indent, spec);

            builder.Append(indent).AppendLine("#endregion");
        }

        #region Write

        private static void EmitWrite(StringBuilder builder, string indent, ModelSpec spec)
        {
            if (spec.IsAbstract)
            {
                builder.Append(indent).Append("public abstract void WriteJson(").Append(Writer)
                    .AppendLine(" writer);");
                builder.AppendLine();
                EmitWriteHelper(builder, indent, spec);
                return;
            }

            builder.Append(indent).AppendLine("/// <summary> Writes this model as the object it is on disk. </summary>");
            builder.Append(indent).Append("public ").Append(Modifier(spec)).Append("void WriteJson(")
                .Append(Writer).AppendLine(" writer)");
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).AppendLine("    writer.WriteStartObject();");
            builder.Append(indent).Append("    WriteJson").Append(spec.Name).AppendLine("(writer);");
            builder.Append(indent).AppendLine("    writer.WriteEndObject();");
            builder.Append(indent).AppendLine("}");
            builder.AppendLine();

            EmitWriteHelper(builder, indent, spec);
        }

        private static void EmitWriteHelper(StringBuilder builder, string indent, ModelSpec spec)
        {
            builder.Append(indent).Append(HelperAccess(spec)).Append("void WriteJson").Append(spec.Name)
                .Append('(').Append(Writer).AppendLine(" writer)");
            builder.Append(indent).AppendLine("{");

            foreach (var member in spec.Members)
            {
                if (member.JsonIgnored) continue;
                builder.Append(indent).Append("    writer.WritePropertyName(\"").Append(member.JsonName)
                    .AppendLine("\");");
                WriteValue(builder, indent + "    ", member);
            }

            // The base's members come AFTER this type's own - Newtonsoft's contract order, and the
            // one place the JSON layout differs from the blob's.
            if (spec.BaseModel != null)
                builder.Append(indent).Append("    WriteJson").Append(SimpleName(spec.BaseModel))
                    .AppendLine("(writer);");

            builder.Append(indent).AppendLine("}");
            builder.AppendLine();
        }

        private static void WriteValue(StringBuilder builder, string indent, MemberSpec member)
        {
            switch (member.Shape)
            {
                case MemberShape.Value:
                case MemberShape.Model:
                case MemberShape.PolymorphicModel:
                    builder.Append(indent).AppendLine(Scalar(member.Value, member.Name));
                    return;

                case MemberShape.ModelList:
                case MemberShape.ValueList:
                case MemberShape.ModelArray:
                case MemberShape.UnmanagedArray:
                    builder.Append(indent).Append("if (").Append(member.Name).AppendLine(" is null) writer.WriteNull();");
                    builder.Append(indent).AppendLine("else");
                    builder.Append(indent).AppendLine("{");
                    builder.Append(indent).AppendLine("    writer.WriteStartArray();");
                    builder.Append(indent).Append("    foreach (var item in ").Append(member.Name).AppendLine(")");
                    builder.Append(indent).Append("        ").AppendLine(Scalar(member.Element, "item"));
                    builder.Append(indent).AppendLine("    writer.WriteEndArray();");
                    builder.Append(indent).AppendLine("}");
                    return;

                case MemberShape.ModelDictionary:
                case MemberShape.ValueDictionary:
                    builder.Append(indent).Append("if (").Append(member.Name).AppendLine(" is null) writer.WriteNull();");
                    builder.Append(indent).AppendLine("else");
                    builder.Append(indent).AppendLine("{");

                    if (member.KeyProperty != null)
                    {
                        builder.Append(indent).AppendLine("    // Keyed: the value carries its own key, so it is not written twice.");
                        builder.Append(indent).AppendLine("    writer.WriteStartArray();");
                        builder.Append(indent).Append("    foreach (var item in ").Append(member.Name).AppendLine(".Values)");
                        builder.Append(indent).Append("        ").AppendLine(Scalar(member.Element, "item"));
                        builder.Append(indent).AppendLine("    writer.WriteEndArray();");
                    }
                    else if (member.Key.Kind == ValueKind.String)
                    {
                        builder.Append(indent).AppendLine("    // A string key is a JSON property name, which is Newtonsoft's own happy path.");
                        builder.Append(indent).AppendLine("    writer.WriteStartObject();");
                        builder.Append(indent).Append("    foreach (var pair in ").Append(member.Name).AppendLine(")");
                        builder.Append(indent).AppendLine("    {");
                        builder.Append(indent).AppendLine("        writer.WritePropertyName(pair.Key);");
                        builder.Append(indent).Append("        ").AppendLine(Scalar(member.Element, "pair.Value"));
                        builder.Append(indent).AppendLine("    }");
                        builder.Append(indent).AppendLine("    writer.WriteEndObject();");
                    }
                    else
                    {
                        builder.Append(indent).AppendLine("    // A key nothing can recover from the value: an array of {K,V}.");
                        builder.Append(indent).AppendLine("    writer.WriteStartArray();");
                        builder.Append(indent).Append("    foreach (var pair in ").Append(member.Name).AppendLine(")");
                        builder.Append(indent).AppendLine("    {");
                        builder.Append(indent).AppendLine("        writer.WriteStartObject();");
                        builder.Append(indent).AppendLine("        writer.WritePropertyName(\"K\");");
                        builder.Append(indent).Append("        ").AppendLine(Scalar(member.Key, "pair.Key"));
                        builder.Append(indent).AppendLine("        writer.WritePropertyName(\"V\");");
                        builder.Append(indent).Append("        ").AppendLine(Scalar(member.Element, "pair.Value"));
                        builder.Append(indent).AppendLine("        writer.WriteEndObject();");
                        builder.Append(indent).AppendLine("    }");
                        builder.Append(indent).AppendLine("    writer.WriteEndArray();");
                    }

                    builder.Append(indent).AppendLine("}");
                    return;
            }
        }

        /// <summary> One value, written. </summary>
        private static string Scalar(ValueSpec value, string access)
        {
            switch (value.Kind)
            {
                case ValueKind.Bool:
                case ValueKind.Byte:
                case ValueKind.SByte:
                case ValueKind.Short:
                case ValueKind.UShort:
                case ValueKind.Int:
                case ValueKind.UInt:
                case ValueKind.Long:
                case ValueKind.ULong:
                case ValueKind.Float:
                case ValueKind.Double:
                case ValueKind.String:
                case ValueKind.Guid:
                case ValueKind.DateTime:
                    return "writer.WriteValue(" + access + ");";
                case ValueKind.Version:
                    return Primitives + ".WriteVersion(writer, " + access + ");";
                case ValueKind.Enum:
                    // At the width the enum declares, which is what Newtonsoft writes for it.
                    return "writer.WriteValue((" + UnderlyingName(value.Underlying) + ")" + access + ");";
                case ValueKind.PrimitiveInt:
                case ValueKind.PrimitiveGuid:
                case ValueKind.PrimitiveFloat:
                    // A bare scalar, never {"Value":...} - the shape PrimitiveIntConverter writes.
                    return "writer.WriteValue(" + access + "." + value.Accessor + ");";
                case ValueKind.Struct:
                    return Primitives + ".Write(writer, " + access + ");";
                case ValueKind.ModelSealed:
                    // A versioned member is wrapped where it is HELD. Its own WriteJson writes the
                    // plain object, so the top-level wrapper stays VersionedEnvelopeConverter's -
                    // and with it the migration path an older file still needs.
                    if (value.Version.Length > 0)
                        return Json + ".JsonModels.WriteEnvelope(writer, " + access + ", \"" + value.Version + "\");";

                    // A CONCRETE member of a value family is still tagged. ConverterRouter resolves
                    // by the value's RUNTIME type and a family converter matches every implementor,
                    // so Marker.Color4 is `[0,{...}]` on disk even though it can only ever hold a
                    // Color4Value. Fifteen members are like this, and the corpus is what found them.
                    if (value.Family.Length > 0)
                        return JsonDispatcher(value.Family) + ".Write(writer, " + access + ");";

                    return "if (" + access + " is null) writer.WriteNull(); else " + access + ".WriteJson(writer);";
                case ValueKind.ModelPolymorphic:
                    return JsonDispatcher(value.Type) + ".Write(writer, " + access + ");";
                default:
                    return "writer.WriteNull();";
            }
        }

        #endregion

        #region Read

        private static void EmitRead(StringBuilder builder, string indent, ModelSpec spec)
        {
            // ReadJson is emitted on the ROOT of a hierarchy and nowhere else, unlike WriteJson.
            // A writer has to know which members to write, so every level of the hierarchy needs
            // its own; a reader does not - it loops over whatever the document carries and hands
            // each name to ReadJsonMember, which IS overridden all the way down. One method, no
            // vtable slot per level, and no chance of the two halves disagreeing about the order.
            if (spec.BaseModel == null)
            {
                builder.Append(indent).AppendLine("/// <summary> Reads one back over this instance. </summary>");
                builder.Append(indent).Append("public void ReadJson(")
                    .Append(Reader).AppendLine(" reader)");
                builder.Append(indent).AppendLine("{");

                builder.Append(indent).Append("    ").Append(Json).AppendLine(".JsonModels.ReadObject(reader, this);");

                builder.Append(indent).AppendLine("}");
                builder.AppendLine();
            }

            // Public because it is an IJsonModel member: the read LOOP lives outside the model, in
            // JsonModels, so it has to be able to call this. Virtual only where something can
            // actually override it - a sealed root has no subtype to dispatch to, and a virtual
            // member on a sealed type is not merely useless, it does not compile.
            var modifier = spec.BaseModel != null ? "public override "
                : spec.IsSealed ? "public "
                : "public virtual ";
            builder.Append(indent).Append(modifier)
                .Append("bool ReadJsonMember(").Append(Reader).AppendLine(" reader, string name)");
            builder.Append(indent).AppendLine("{");

            // Three models carry no members at all - EffectShapePoint, EffectShapeSpreadSine and
            // ScreenLimitNone are pure enum-selected behaviour - and an empty switch is a warning
            // rather than merely useless.
            var readable = 0;
            foreach (var member in spec.Members)
                if (!member.JsonIgnored && member.Assignable)
                    readable++;

            if (readable > 0)
            {
                builder.Append(indent).AppendLine("    switch (name)");
                builder.Append(indent).AppendLine("    {");

                foreach (var member in spec.Members)
                {
                    if (member.JsonIgnored || !member.Assignable) continue;
                    builder.Append(indent).Append("        case \"").Append(member.JsonName).AppendLine("\":");
                    ReadValue(builder, indent + "            ", member);
                    builder.Append(indent).AppendLine("            return true;");
                }

                builder.Append(indent).AppendLine("    }");
                builder.AppendLine();
            }
            builder.Append(indent).Append("    return ")
                .AppendLine(spec.BaseModel != null ? "base.ReadJsonMember(reader, name);" : "false;");
            builder.Append(indent).AppendLine("}");
            builder.AppendLine();
        }

        private static void ReadValue(StringBuilder builder, string indent, MemberSpec member)
        {
            switch (member.Shape)
            {
                case MemberShape.Value:
                case MemberShape.Model:
                case MemberShape.PolymorphicModel:
                    builder.Append(indent).Append(member.Name).Append(" = ")
                        .Append(ReadScalar(member.Value)).AppendLine(";");
                    return;

                case MemberShape.ModelList:
                case MemberShape.ValueList:
                    builder.Append(indent).Append("if (reader.TokenType == global::Newtonsoft.Json.JsonToken.Null) ")
                        .Append(member.Name).AppendLine(" = null;");
                    builder.Append(indent).AppendLine("else");
                    builder.Append(indent).AppendLine("{");
                    builder.Append(indent).Append("    var items = new ").Append(member.Type).AppendLine("();");
                    builder.Append(indent).AppendLine("    while (reader.Read() && reader.TokenType != global::Newtonsoft.Json.JsonToken.EndArray)");
                    builder.Append(indent).Append("        items.Add(").Append(ReadScalar(member.Element)).AppendLine(");");
                    builder.Append(indent).Append("    ").Append(member.Name).AppendLine(" = items;");
                    builder.Append(indent).AppendLine("}");
                    return;

                case MemberShape.ModelArray:
                case MemberShape.UnmanagedArray:
                    builder.Append(indent).Append("if (reader.TokenType == global::Newtonsoft.Json.JsonToken.Null) ")
                        .Append(member.Name).AppendLine(" = null;");
                    builder.Append(indent).AppendLine("else");
                    builder.Append(indent).AppendLine("{");
                    builder.Append(indent).Append("    var items = new global::System.Collections.Generic.List<")
                        .Append(member.Element.Type).AppendLine(">();");
                    builder.Append(indent).AppendLine("    while (reader.Read() && reader.TokenType != global::Newtonsoft.Json.JsonToken.EndArray)");
                    builder.Append(indent).Append("        items.Add(").Append(ReadScalar(member.Element)).AppendLine(");");
                    builder.Append(indent).Append("    ").Append(member.Name).AppendLine(" = items.ToArray();");
                    builder.Append(indent).AppendLine("}");
                    return;

                case MemberShape.ModelDictionary:
                case MemberShape.ValueDictionary:
                    builder.Append(indent).Append("if (reader.TokenType == global::Newtonsoft.Json.JsonToken.Null) ")
                        .Append(member.Name).AppendLine(" = null;");
                    builder.Append(indent).AppendLine("else");
                    builder.Append(indent).AppendLine("{");
                    builder.Append(indent).Append("    var map = new ").Append(member.Type).AppendLine("();");

                    if (member.KeyProperty != null)
                    {
                        builder.Append(indent).AppendLine("    while (reader.Read() && reader.TokenType != global::Newtonsoft.Json.JsonToken.EndArray)");
                        builder.Append(indent).AppendLine("    {");
                        builder.Append(indent).Append("        var item = ").Append(ReadScalar(member.Element)).AppendLine(";");
                        builder.Append(indent).Append("        map[item.").Append(member.KeyProperty).AppendLine("] = item;");
                        builder.Append(indent).AppendLine("    }");
                    }
                    else if (member.Key.Kind == ValueKind.String)
                    {
                        builder.Append(indent).AppendLine("    while (reader.Read() && reader.TokenType != global::Newtonsoft.Json.JsonToken.EndObject)");
                        builder.Append(indent).AppendLine("    {");
                        builder.Append(indent).AppendLine("        var key = (string)reader.Value;");
                        builder.Append(indent).AppendLine("        reader.Read();");
                        builder.Append(indent).Append("        map[key] = ").Append(ReadScalar(member.Element)).AppendLine(";");
                        builder.Append(indent).AppendLine("    }");
                    }
                    else
                    {
                        builder.Append(indent).AppendLine("    while (reader.Read() && reader.TokenType != global::Newtonsoft.Json.JsonToken.EndArray)");
                        builder.Append(indent).AppendLine("    {");
                        builder.Append(indent).Append("        ").Append(member.Key.Type).AppendLine(" pairKey = default;");
                        builder.Append(indent).Append("        ").Append(member.Element.Type).AppendLine(" pairValue = default;");
                        builder.Append(indent).AppendLine("        while (reader.Read() && reader.TokenType != global::Newtonsoft.Json.JsonToken.EndObject)");
                        builder.Append(indent).AppendLine("        {");
                        builder.Append(indent).AppendLine("            var field = (string)reader.Value;");
                        builder.Append(indent).AppendLine("            reader.Read();");
                        builder.Append(indent).Append("            if (field == \"K\") pairKey = ").Append(ReadScalar(member.Key)).AppendLine(";");
                        builder.Append(indent).Append("            else if (field == \"V\") pairValue = ").Append(ReadScalar(member.Element)).AppendLine(";");
                        builder.Append(indent).AppendLine("            else reader.Skip();");
                        builder.Append(indent).AppendLine("        }");
                        builder.Append(indent).AppendLine("        map[pairKey] = pairValue;");
                        builder.Append(indent).AppendLine("    }");
                    }

                    builder.Append(indent).Append("    ").Append(member.Name).AppendLine(" = map;");
                    builder.Append(indent).AppendLine("}");
                    return;
            }
        }

        /// <summary> One value, read. The reader sits ON the value's first token and must be left on
        /// its last one, which is what makes these safe to nest. </summary>
        private static string ReadScalar(ValueSpec value)
        {
            switch (value.Kind)
            {
                case ValueKind.Bool: return "global::System.Convert.ToBoolean(reader.Value)";
                case ValueKind.Byte: return "global::System.Convert.ToByte(reader.Value)";
                case ValueKind.SByte: return "global::System.Convert.ToSByte(reader.Value)";
                case ValueKind.Short: return "global::System.Convert.ToInt16(reader.Value)";
                case ValueKind.UShort: return "global::System.Convert.ToUInt16(reader.Value)";
                case ValueKind.Int: return "global::System.Convert.ToInt32(reader.Value)";
                case ValueKind.UInt: return "global::System.Convert.ToUInt32(reader.Value)";
                case ValueKind.Long: return "global::System.Convert.ToInt64(reader.Value)";
                case ValueKind.ULong: return "global::System.Convert.ToUInt64(reader.Value)";
                case ValueKind.Float: return "global::System.Convert.ToSingle(reader.Value)";
                case ValueKind.Double: return "global::System.Convert.ToDouble(reader.Value)";
                case ValueKind.String: return "(string)reader.Value";
                case ValueKind.Guid: return Primitives + ".ReadGuid(reader)";
                case ValueKind.DateTime: return Primitives + ".ReadDateTime(reader)";
                case ValueKind.Version: return Primitives + ".ReadVersion(reader)";
                case ValueKind.Enum:
                    return "(" + value.Type + ")" + ReadScalar(new ValueSpec(string.Empty, value.Underlying));
                case ValueKind.PrimitiveInt:
                    return "new " + value.Type + "(global::System.Convert.ToInt32(reader.Value))";
                case ValueKind.PrimitiveGuid:
                    return "new " + value.Type + "(" + Primitives + ".ReadGuid(reader))";
                case ValueKind.PrimitiveFloat:
                    return "new " + value.Type + "(global::System.Convert.ToSingle(reader.Value))";
                case ValueKind.Struct:
                    return Primitives + ".Read" + SimpleName(value.Type) + "(reader)";
                case ValueKind.ModelSealed:
                    if (value.Version.Length > 0)
                        return Json + ".JsonModels.ReadEnveloped<" + value.Type + ">(reader)";
                    if (value.Family.Length > 0)
                        return "(" + value.Type + ")" + JsonDispatcher(value.Family) + ".Read(reader)";
                    return Json + ".JsonModels.Read<" + value.Type + ">(reader)";
                case ValueKind.ModelPolymorphic:
                    return JsonDispatcher(value.Type) + ".Read(reader)";
                default:
                    return "default";
            }
        }

        #endregion

        #region Naming

        public static string JsonDispatcher(string type)
            => "global::BH.SDK.Models.Generated." + SimpleName(type) + "Json";

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

using System;
using System.Collections.Generic;
using System.IO;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Enums.Text;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;

namespace BH.SDK.Services.Cache
{
    // =========================================================================================
    // DEAD CODE, KEPT ON PURPOSE - A DELETION CANDIDATE.
    //
    // Nothing constructs, registers or calls this. The level cache is disconnected from the game:
    // `RootScope` does not register `LevelCacheService`, and `LevelLoaderService` no longer hooks
    // it on any read, write or delete. It is left in the tree for ONE reason - the session that
    // builds the `.blob` format is meant to read it before deleting it, because it is a worked
    // example of what a hand-written codec for this model looks like and which of its types are
    // polymorphic. See docs/issues/ROSLYN_PLAN.md, and LOADING_HISTORY.md section 10.
    //
    // WHY IT IS GOING RATHER THAN BEING FIXED: everything here is what a fast FORMAT does, plus
    // the cost of deciding when it is stale, an invalidation hook on every write path, and a
    // second hand-written serializer to keep in step with the real one. A format has none of
    // those problems, because it cannot disagree with the file - it IS the file.
    //
    // DO NOT WIRE IT BACK UP to make something faster. If loading is slow, that is the format's
    // job now.
    // =========================================================================================

    // THE OBJECT TREE, WRITTEN BY HAND. It is 18.1 MB of volcano's 18.5 MB and 83% of that level's
    // load, and the measured reason is not the tokenizer: it is Newtonsoft binding members by
    // reflection and dispatching a converter per value (see the consumer's
    // docs/issues/LOADING_HISTORY.md §7 - the converter layer alone is 79% of this subtree).
    // Nothing about a general serializer can be made to skip that; writing the branch out by hand
    // can, and the model already accepts exactly this kind of boilerplate - Copy, Update, Pull,
    // Equals and GetHashCode are all hand-written per type for the same reason.
    //
    // THE SIZE OF THE PRIZE IS ALREADY MEASURED ELSEWHERE. `Level.Copy()` - a hand-written deep
    // clone of this same graph, no reflection anywhere - runs in 131 ms on a 13.8 MB level, against
    // 11-15 s to deserialize one. A codec is that shape of work, so this is the right order of
    // magnitude to expect and the right thing to check first if it is not reached.
    //
    // WHAT THIS IS NOT: it is not a second file format, and no player, tool or server ever sees it.
    // It is the payload of a cache that is regenerated whenever it does not match
    // (`LevelCacheKey`), so it carries no migrations, no forward compatibility and no tolerance for
    // an unknown tag - an unreadable payload is a cache miss, and a cache miss is an ordinary load.
    // That is what buys the right to write it this densely.
    //
    // EVERY LIST AND EVERY STRING MAY BE NULL, and null is not the same as empty here: an object's
    // keyframe collections are legitimately empty (the consumer falls back to `defaults.xxx`), and
    // the round trip has to preserve which of the two it was or `Equals` stops holding. Hence the
    // length prefix of -1 rather than a separate flag.

    /// <summary> Reads and writes a level's object tree in the cache's own binary form. </summary>
    public static class LevelObjectCodec
    {
        private const int NullLength = -1;

        #region Objects

        /// <summary> Writes an object scope's whole dictionary. </summary>
        public static void WriteObjects(BinaryWriter writer, Dictionary<ObjectId, RectObject> objects)
        {
            if (objects == null)
            {
                writer.Write(NullLength);
                return;
            }

            writer.Write(objects.Count);

            // The key is recoverable from the value's own ObjectId, exactly as
            // DictionaryAsListConverter recovers it on the JSON side - so it is not written twice.
            foreach (var obj in objects.Values)
                WriteObject(writer, obj);
        }

        /// <summary> Reads one back. </summary>
        public static Dictionary<ObjectId, RectObject> ReadObjects(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count == NullLength) return null;

            var objects = new Dictionary<ObjectId, RectObject>(count);
            for (var i = 0; i < count; i++)
            {
                var obj = ReadObject(reader);
                objects[obj.ObjectId] = obj;
            }

            return objects;
        }

        private static void WriteObject(BinaryWriter writer, RectObject obj)
        {
            var type = obj.GetModelType();
            writer.Write((byte)type);

            WriteObjectId(writer, obj.ObjectId);
            WriteObjectId(writer, obj.ParentObjectId);
            WriteString(writer, obj.Name);
            writer.Write(obj.Active);
            WriteSpan(writer, obj.Span);
            writer.Write(obj.Layer);

            WriteList(writer, obj.Positions, WritePosKey);
            WriteList(writer, obj.Rotations, WriteAngleKey);
            WriteList(writer, obj.Scales, WriteScaKey);
            WriteList(writer, obj.Sizes, WriteScaKey);
            WriteList(writer, obj.AnchorsMin, WriteAlignmentKey);
            WriteList(writer, obj.AnchorsMax, WriteAlignmentKey);
            WriteList(writer, obj.Pivots, WriteAlignmentKey);

            switch (type)
            {
                case ObjectType.RectObject: break;
                case ObjectType.ShapeObject: WriteShape(writer, (ShapeObject)obj); break;
                case ObjectType.EffectObject: WriteEffect(writer, (EffectObject)obj); break;
                case ObjectType.TextObject: WriteText(writer, (TextObject)obj); break;
                case ObjectType.PrefabObject: WritePrefab(writer, (PrefabObject)obj); break;
                default: throw new NotSupportedException($"Unhandled object type '{type}'");
            }
        }

        private static RectObject ReadObject(BinaryReader reader)
        {
            var type = (ObjectType)reader.ReadByte();

            // Created by its own type BEFORE the base fields are read, so the subclass half below
            // writes into the same instance. A new RectObject subtype extends both switches here,
            // exactly as it extends ObjectConverter.GetType and LevelUtils.PullObject.
            RectObject obj = type switch
            {
                ObjectType.RectObject => new RectObject(),
                ObjectType.ShapeObject => new ShapeObject(),
                ObjectType.EffectObject => new EffectObject(),
                ObjectType.TextObject => new TextObject(),
                ObjectType.PrefabObject => new PrefabObject(),
                _ => throw new NotSupportedException($"Unhandled object type '{type}'"),
            };

            obj.ObjectId = ReadObjectId(reader);
            obj.ParentObjectId = ReadObjectId(reader);
            obj.Name = ReadString(reader);
            obj.Active = reader.ReadBoolean();
            obj.Span = ReadSpan(reader);
            obj.Layer = reader.ReadInt32();

            obj.Positions = ReadList(reader, ReadPosKey);
            obj.Rotations = ReadList(reader, ReadAngleKey);
            obj.Scales = ReadList(reader, ReadScaKey);
            obj.Sizes = ReadList(reader, ReadScaKey);
            obj.AnchorsMin = ReadList(reader, ReadAlignmentKey);
            obj.AnchorsMax = ReadList(reader, ReadAlignmentKey);
            obj.Pivots = ReadList(reader, ReadAlignmentKey);

            switch (obj)
            {
                case ShapeObject shape: ReadShape(reader, shape); break;
                case EffectObject effect: ReadEffect(reader, effect); break;
                case TextObject text: ReadText(reader, text); break;
                case PrefabObject prefab: ReadPrefab(reader, prefab); break;
            }

            return obj;
        }

        private static void WriteShape(BinaryWriter writer, ShapeObject shape)
        {
            WriteGuid(writer, shape.ShapeId.value);
            WriteGuid(writer, shape.ColliderId.value);
            writer.Write((byte)shape.ShaderType);
            writer.Write(shape.TextureResourceId.value);
            WriteList(writer, shape.Colors, WriteColor4X4Key);
            WriteList(writer, shape.UVs, WriteUVKey);
        }

        private static void ReadShape(BinaryReader reader, ShapeObject shape)
        {
            shape.ShapeId = new ShapeId(ReadGuid(reader));
            shape.ColliderId = new ShapeId(ReadGuid(reader));
            shape.ShaderType = (ShaderType)reader.ReadByte();
            shape.TextureResourceId = new TextureResourceId(reader.ReadInt32());
            shape.Colors = ReadList(reader, ReadColor4X4Key);
            shape.UVs = ReadList(reader, ReadUVKey);
        }

        private static void WriteEffect(BinaryWriter writer, EffectObject effect)
            => WriteGuid(writer, effect.EffectId.value);

        private static void ReadEffect(BinaryReader reader, EffectObject effect)
            => effect.EffectId = new EffectId(ReadGuid(reader));

        private static void WriteText(BinaryWriter writer, TextObject text)
        {
            WriteString4(writer, text.Text);
            writer.Write(text.FontResourceId.value);
            WriteList(writer, text.Colors, WriteColor4Key);
            WriteList(writer, text.FontSizes, WriteFloatKey);
            WriteList(writer, text.Fillments, WriteFillmentKey);
            WriteList(writer, text.Appearings, WriteAppearingKey);
            WriteString(writer, text.AppearingMask);
            writer.Write(text.WordWrap);
            writer.Write((byte)text.HorizontalAlignment);
            writer.Write((byte)text.VerticalAlignment);
        }

        private static void ReadText(BinaryReader reader, TextObject text)
        {
            text.Text = ReadString4(reader);
            text.FontResourceId = new FontResourceId(reader.ReadInt32());
            text.Colors = ReadList(reader, ReadColor4Key);
            text.FontSizes = ReadList(reader, ReadFloatKey);
            text.Fillments = ReadList(reader, ReadFillmentKey);
            text.Appearings = ReadList(reader, ReadAppearingKey);
            text.AppearingMask = ReadString(reader);
            text.WordWrap = reader.ReadBoolean();
            text.HorizontalAlignment = (TextObjectHorizontalAlignment)reader.ReadByte();
            text.VerticalAlignment = (TextObjectVerticalAlignment)reader.ReadByte();
        }

        private static void WritePrefab(BinaryWriter writer, PrefabObject prefab)
        {
            WriteGuid(writer, prefab.PrefabId.value);

            // The one id map in the model whose key cannot be recovered from its value, which is
            // why the JSON side needs DictionaryAsPairListConverter for it too.
            if (prefab.ObjectIds == null) writer.Write(NullLength);
            else
            {
                writer.Write(prefab.ObjectIds.Count);
                foreach (var pair in prefab.ObjectIds)
                {
                    WriteObjectId(writer, pair.Key);
                    WriteObjectId(writer, pair.Value);
                }
            }

            if (prefab.Modifications == null) writer.Write(NullLength);
            else
            {
                writer.Write(prefab.Modifications.Count);
                foreach (var modification in prefab.Modifications.Values)
                    WriteModification(writer, modification);
            }
        }

        private static void ReadPrefab(BinaryReader reader, PrefabObject prefab)
        {
            prefab.PrefabId = new PrefabId(ReadGuid(reader));

            var ids = reader.ReadInt32();
            if (ids == NullLength) prefab.ObjectIds = null;
            else
            {
                prefab.ObjectIds = new Dictionary<ObjectId, ObjectId>(ids);
                for (var i = 0; i < ids; i++)
                {
                    var key = ReadObjectId(reader);
                    prefab.ObjectIds[key] = ReadObjectId(reader);
                }
            }

            var modifications = reader.ReadInt32();
            if (modifications == NullLength) prefab.Modifications = null;
            else
            {
                prefab.Modifications = new Dictionary<ModificationKey, Modification>(modifications);
                for (var i = 0; i < modifications; i++)
                {
                    var modification = ReadModification(reader);
                    prefab.Modifications[modification.Key] = modification;
                }
            }
        }

        #endregion

        #region Keyframes

        private static void WriteKeyframe(BinaryWriter writer, Keyframe key)
        {
            writer.Write(key.Frame);
            writer.Write((byte)key.Ease);
        }

        private static void ReadKeyframe(BinaryReader reader, Keyframe key)
        {
            key.Frame = reader.ReadInt32();
            key.Ease = (EaseType)reader.ReadByte();
        }

        private static void WritePosKey(BinaryWriter writer, PosKey key)
        {
            WriteKeyframe(writer, key);
            WriteVector2(writer, key.Pos);
        }

        private static PosKey ReadPosKey(BinaryReader reader)
        {
            var key = new PosKey();
            ReadKeyframe(reader, key);
            key.Pos = ReadVector2(reader);
            return key;
        }

        private static void WriteAngleKey(BinaryWriter writer, AngleKey key)
        {
            WriteKeyframe(writer, key);
            WriteFloat(writer, key.Angle);
        }

        private static AngleKey ReadAngleKey(BinaryReader reader)
        {
            var key = new AngleKey();
            ReadKeyframe(reader, key);
            key.Angle = ReadFloat(reader);
            return key;
        }

        private static void WriteScaKey(BinaryWriter writer, ScaKey key)
        {
            WriteKeyframe(writer, key);
            WriteVector2(writer, key.Scale);
        }

        private static ScaKey ReadScaKey(BinaryReader reader)
        {
            var key = new ScaKey();
            ReadKeyframe(reader, key);
            key.Scale = ReadVector2(reader);
            return key;
        }

        private static void WriteAlignmentKey(BinaryWriter writer, AlignmentKey key)
        {
            WriteKeyframe(writer, key);
            WriteVector2(writer, key.Value);
        }

        private static AlignmentKey ReadAlignmentKey(BinaryReader reader)
        {
            var key = new AlignmentKey();
            ReadKeyframe(reader, key);
            key.Value = ReadVector2(reader);
            return key;
        }

        private static void WriteFloatKey(BinaryWriter writer, FloatKey key)
        {
            WriteKeyframe(writer, key);
            WriteFloat(writer, key.Value);
        }

        private static FloatKey ReadFloatKey(BinaryReader reader)
        {
            var key = new FloatKey();
            ReadKeyframe(reader, key);
            key.Value = ReadFloat(reader);
            return key;
        }

        private static void WriteColor4Key(BinaryWriter writer, Color4Key key)
        {
            WriteKeyframe(writer, key);
            WriteColor4(writer, key.Value);
        }

        private static Color4Key ReadColor4Key(BinaryReader reader)
        {
            var key = new Color4Key();
            ReadKeyframe(reader, key);
            key.Value = ReadColor4(reader);
            return key;
        }

        private static void WriteFillmentKey(BinaryWriter writer, FillmentKey key)
        {
            WriteKeyframe(writer, key);
            writer.Write(key.Value);
            writer.Write((byte)key.Direction);
        }

        private static FillmentKey ReadFillmentKey(BinaryReader reader)
        {
            var key = new FillmentKey();
            ReadKeyframe(reader, key);
            key.Value = reader.ReadSingle();
            key.Direction = (TextFillDirection)reader.ReadByte();
            return key;
        }

        private static void WriteAppearingKey(BinaryWriter writer, AppearingKey key)
        {
            WriteKeyframe(writer, key);
            writer.Write(key.Value);
            writer.Write((byte)key.Mode);
        }

        private static AppearingKey ReadAppearingKey(BinaryReader reader)
        {
            var key = new AppearingKey();
            ReadKeyframe(reader, key);
            key.Value = reader.ReadSingle();
            key.Mode = (TextAppearingMode)reader.ReadByte();
            return key;
        }

        private static void WriteUVKey(BinaryWriter writer, UVKey key)
        {
            WriteKeyframe(writer, key);
            WriteVector2Value(writer, key.Tilling);
            WriteVector2Value(writer, key.Offset);
        }

        private static UVKey ReadUVKey(BinaryReader reader)
        {
            var key = new UVKey();
            ReadKeyframe(reader, key);
            key.Tilling = ReadVector2Value(reader);
            key.Offset = ReadVector2Value(reader);
            return key;
        }

        // The four-corner colour family, the one keyframe family that is polymorphic in the
        // KEYFRAME rather than in the value it holds.
        private static void WriteColor4X4Key(BinaryWriter writer, IColor4X4Key key)
        {
            var type = key.GetModelType();
            writer.Write((byte)type);
            WriteKeyframe(writer, (Keyframe)key);

            switch (key)
            {
                case Color4Key value:
                    WriteColor4(writer, value.Value);
                    break;
                case ColorHorizontalKey horizontal:
                    WriteColor4(writer, horizontal.Color4Left);
                    WriteColor4(writer, horizontal.Color4Right);
                    break;
                case ColorVerticalKey vertical:
                    WriteColor4(writer, vertical.Color4Bottom);
                    WriteColor4(writer, vertical.Color4Top);
                    break;
                case Color4X4Key corners:
                    WriteColor4(writer, corners.Color4BL);
                    WriteColor4(writer, corners.Color4BR);
                    WriteColor4(writer, corners.Color4TL);
                    WriteColor4(writer, corners.Color4TR);
                    break;
                default:
                    throw new NotSupportedException($"Unhandled colour key '{key.GetType()}'");
            }
        }

        private static IColor4X4Key ReadColor4X4Key(BinaryReader reader)
        {
            var type = (Color4X4KeyType)reader.ReadByte();

            switch (type)
            {
                case Color4X4KeyType.Value:
                {
                    var key = new Color4Key();
                    ReadKeyframe(reader, key);
                    key.Value = ReadColor4(reader);
                    return key;
                }
                case Color4X4KeyType.Horizontal:
                {
                    var key = new ColorHorizontalKey();
                    ReadKeyframe(reader, key);
                    key.Color4Left = ReadColor4(reader);
                    key.Color4Right = ReadColor4(reader);
                    return key;
                }
                case Color4X4KeyType.Vertical:
                {
                    var key = new ColorVerticalKey();
                    ReadKeyframe(reader, key);
                    key.Color4Bottom = ReadColor4(reader);
                    key.Color4Top = ReadColor4(reader);
                    return key;
                }
                case Color4X4KeyType.BariCentrical:
                {
                    var key = new Color4X4Key();
                    ReadKeyframe(reader, key);
                    key.Color4BL = ReadColor4(reader);
                    key.Color4BR = ReadColor4(reader);
                    key.Color4TL = ReadColor4(reader);
                    key.Color4TR = ReadColor4(reader);
                    return key;
                }
                default:
                    throw new NotSupportedException($"Unhandled colour key type '{type}'");
            }
        }

        #endregion

        #region Polymorphic values

        private static void WriteVector2(BinaryWriter writer, IVector2 value)
        {
            if (value == null) { writer.Write((byte)0xFF); return; }

            writer.Write((byte)value.GetModelType());
            switch (value)
            {
                case Vector2Value plain:
                    writer.Write(plain.X); writer.Write(plain.Y);
                    break;
                case Vector2Rect rect:
                    writer.Write(rect.MinX); writer.Write(rect.MinY);
                    writer.Write(rect.MaxX); writer.Write(rect.MaxY);
                    break;
                case Vector2RectStep step:
                    writer.Write(step.MinX); writer.Write(step.MinY);
                    writer.Write(step.MaxX); writer.Write(step.MaxY);
                    writer.Write(step.Step);
                    break;
                case Vector2Circle circle:
                    writer.Write(circle.X); writer.Write(circle.Y); writer.Write(circle.Radius);
                    break;
                default:
                    throw new NotSupportedException($"Unhandled vector2 '{value.GetType()}'");
            }
        }

        private static IVector2 ReadVector2(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag == 0xFF) return null;

            switch ((VectorType)tag)
            {
                case VectorType.Value:
                    return new Vector2Value { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                case VectorType.RandomRect:
                    return new Vector2Rect
                    {
                        MinX = reader.ReadSingle(), MinY = reader.ReadSingle(),
                        MaxX = reader.ReadSingle(), MaxY = reader.ReadSingle(),
                    };
                case VectorType.RandomRectStep:
                    return new Vector2RectStep
                    {
                        MinX = reader.ReadSingle(), MinY = reader.ReadSingle(),
                        MaxX = reader.ReadSingle(), MaxY = reader.ReadSingle(),
                        Step = reader.ReadSingle(),
                    };
                case VectorType.RandomCircle:
                    return new Vector2Circle
                    {
                        X = reader.ReadSingle(), Y = reader.ReadSingle(), Radius = reader.ReadSingle(),
                    };
                default:
                    throw new NotSupportedException($"Unhandled vector2 type '{(VectorType)tag}'");
            }
        }

        private static void WriteVector2Value(BinaryWriter writer, Vector2Value value)
        {
            if (value == null) { writer.Write(false); return; }

            writer.Write(true);
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        private static Vector2Value ReadVector2Value(BinaryReader reader)
            => reader.ReadBoolean()
                ? new Vector2Value { X = reader.ReadSingle(), Y = reader.ReadSingle() }
                : null;

        private static void WriteFloat(BinaryWriter writer, IFloat value)
        {
            if (value == null) { writer.Write((byte)0xFF); return; }

            writer.Write((byte)value.GetModelType());
            switch (value)
            {
                case FloatValue plain: writer.Write(plain.Value); break;
                case FloatMinMax minMax: writer.Write(minMax.Min); writer.Write(minMax.Max); break;
                case FloatMinMaxStep step:
                    writer.Write(step.Min); writer.Write(step.Max); writer.Write(step.Step);
                    break;
                default: throw new NotSupportedException($"Unhandled float '{value.GetType()}'");
            }
        }

        private static IFloat ReadFloat(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag == 0xFF) return null;

            switch ((FloatType)tag)
            {
                case FloatType.Value:
                    return new FloatValue { Value = reader.ReadSingle() };
                case FloatType.RandomMinMax:
                    return new FloatMinMax { Min = reader.ReadSingle(), Max = reader.ReadSingle() };
                case FloatType.RandomMinMaxStep:
                    return new FloatMinMaxStep
                    {
                        Min = reader.ReadSingle(), Max = reader.ReadSingle(), Step = reader.ReadSingle(),
                    };
                default:
                    throw new NotSupportedException($"Unhandled float type '{(FloatType)tag}'");
            }
        }

        private static void WriteColor4(BinaryWriter writer, IColor4 value)
        {
            if (value == null) { writer.Write((byte)0xFF); return; }

            writer.Write((byte)value.GetModelType());
            switch (value)
            {
                case Color4Value plain:
                    writer.Write(plain.R); writer.Write(plain.G);
                    writer.Write(plain.B); writer.Write(plain.A);
                    break;
                case Color4ThemeRef themeRef:
                    writer.Write(themeRef.ThemeColorIndex);
                    break;
                case Color4MinMax minMax:
                    writer.Write(minMax.MinR); writer.Write(minMax.MinG);
                    writer.Write(minMax.MinB); writer.Write(minMax.MinA);
                    writer.Write(minMax.MaxR); writer.Write(minMax.MaxG);
                    writer.Write(minMax.MaxB); writer.Write(minMax.MaxA);
                    break;
                default:
                    throw new NotSupportedException($"Unhandled color4 '{value.GetType()}'");
            }
        }

        private static IColor4 ReadColor4(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag == 0xFF) return null;

            switch ((ColorType)tag)
            {
                case ColorType.Value:
                    return new Color4Value
                    {
                        R = reader.ReadSingle(), G = reader.ReadSingle(),
                        B = reader.ReadSingle(), A = reader.ReadSingle(),
                    };
                case ColorType.ThemeRef:
                    return new Color4ThemeRef { ThemeColorIndex = reader.ReadInt32() };
                case ColorType.RandomMinMax:
                    return new Color4MinMax
                    {
                        MinR = reader.ReadSingle(), MinG = reader.ReadSingle(),
                        MinB = reader.ReadSingle(), MinA = reader.ReadSingle(),
                        MaxR = reader.ReadSingle(), MaxG = reader.ReadSingle(),
                        MaxB = reader.ReadSingle(), MaxA = reader.ReadSingle(),
                    };
                default:
                    throw new NotSupportedException($"Unhandled color4 type '{(ColorType)tag}'");
            }
        }

        // `String4` only to keep it apart from the plain `string` helper below - this one is the
        // POLYMORPHIC IString, which a text object's content is so that a level can carry its text
        // per language inline.
        private static void WriteString4(BinaryWriter writer, IString value)
        {
            if (value == null) { writer.Write((byte)0xFF); return; }

            writer.Write((byte)value.GetModelType());
            switch (value)
            {
                case StringValue plain:
                    WriteString(writer, plain.Value);
                    break;
                case StringLocalized localized:
                    if (localized.Strings == null) writer.Write(NullLength);
                    else
                    {
                        writer.Write(localized.Strings.Count);
                        foreach (var language in localized.Strings)
                        {
                            WriteString(writer, language.LanguageCode);
                            WriteString(writer, language.Value);
                        }
                    }

                    break;
                default:
                    throw new NotSupportedException($"Unhandled string '{value.GetType()}'");
            }
        }

        private static IString ReadString4(BinaryReader reader)
        {
            var tag = reader.ReadByte();
            if (tag == 0xFF) return null;

            switch ((StringType)tag)
            {
                case StringType.Value:
                    return new StringValue { Value = ReadString(reader) };
                case StringType.Localized:
                {
                    var localized = new StringLocalized();
                    var count = reader.ReadInt32();
                    if (count == NullLength) { localized.Strings = null; return localized; }

                    localized.Strings = new List<StringLanguage>(count);
                    for (var i = 0; i < count; i++)
                    {
                        localized.Strings.Add(new StringLanguage
                        {
                            LanguageCode = ReadString(reader),
                            Value = ReadString(reader),
                        });
                    }

                    return localized;
                }
                default:
                    throw new NotSupportedException($"Unhandled string type '{(StringType)tag}'");
            }
        }

        #endregion

        #region Primitives

        private static void WriteObjectId(BinaryWriter writer, ObjectId id) => writer.Write(id.value);

        private static ObjectId ReadObjectId(BinaryReader reader) => new(reader.ReadInt32());

        // Two ints and the anchors, which is one more piece than the JSON form carries - that one
        // folds the anchors into the sign of each number because it has to stay two numbers wide.
        // Nothing here has to, so it says what it means.
        private static void WriteSpan(BinaryWriter writer, FrameSpan span)
        {
            writer.Write(span.StartFrame);
            writer.Write(span.FrameDuration);
            writer.Write((byte)span.Anchors);
        }

        private static FrameSpan ReadSpan(BinaryReader reader)
        {
            var start = reader.ReadInt32();
            var duration = reader.ReadInt32();
            return new FrameSpan(start, duration, (FrameAnchor)reader.ReadByte());
        }

        private static void WriteGuid(BinaryWriter writer, Guid value)
        {
            // ToByteArray allocates; the 16 bytes are written straight instead.
            Span<byte> bytes = stackalloc byte[16];
            value.TryWriteBytes(bytes);
            for (var i = 0; i < 16; i++) writer.Write(bytes[i]);
        }

        private static Guid ReadGuid(BinaryReader reader)
        {
            Span<byte> bytes = stackalloc byte[16];
            for (var i = 0; i < 16; i++) bytes[i] = reader.ReadByte();
            return new Guid(bytes);
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            // BinaryWriter.Write(string) cannot express null, and null is a real state here - an
            // object with no name is not an object named "".
            writer.Write(value != null);
            if (value != null) writer.Write(value);
        }

        private static string ReadString(BinaryReader reader)
            => reader.ReadBoolean() ? reader.ReadString() : null;

        // A modification's Value is `object` by design, holding exactly what Newtonsoft produces
        // for a raw JSON number - long or double, normalized by the property's own setter. The tag
        // set here is that contract, and anything outside it is a modification this cache cannot
        // carry, which is a miss rather than a corruption.
        private static void WriteModification(BinaryWriter writer, Modification modification)
        {
            WriteObjectId(writer, modification.Key.ObjectId);
            WriteString(writer, modification.Key.Path);

            switch (modification.Value)
            {
                case null: writer.Write((byte)0); break;
                case long value: writer.Write((byte)1); writer.Write(value); break;
                case double value: writer.Write((byte)2); writer.Write(value); break;
                case string value: writer.Write((byte)3); WriteString(writer, value); break;
                case bool value: writer.Write((byte)4); writer.Write(value); break;
                default:
                    throw new NotSupportedException(
                        $"Unhandled modification value '{modification.Value.GetType()}'");
            }
        }

        private static Modification ReadModification(BinaryReader reader)
        {
            var key = new ModificationKey
            {
                ObjectId = ReadObjectId(reader),
                Path = ReadString(reader),
            };

            object value = reader.ReadByte() switch
            {
                0 => null,
                1 => reader.ReadInt64(),
                2 => reader.ReadDouble(),
                3 => ReadString(reader),
                4 => reader.ReadBoolean(),
                var tag => throw new NotSupportedException($"Unhandled modification tag '{tag}'"),
            };

            return new Modification { Key = key, Value = value };
        }

        #endregion

        #region Lists

        private static void WriteList<TValue>(BinaryWriter writer, List<TValue> values,
            Action<BinaryWriter, TValue> write)
        {
            if (values == null)
            {
                writer.Write(NullLength);
                return;
            }

            writer.Write(values.Count);
            for (var i = 0; i < values.Count; i++)
                write(writer, values[i]);
        }

        private static List<TValue> ReadList<TValue>(BinaryReader reader, Func<BinaryReader, TValue> read)
        {
            var count = reader.ReadInt32();
            if (count == NullLength) return null;

            var values = new List<TValue>(count);
            for (var i = 0; i < count; i++)
                values.Add(read(reader));

            return values;
        }

        #endregion
    }
}

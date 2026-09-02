using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Statistics;

namespace BH.SDK.Serialization.Blob
{
    // THE FOUR STRUCTS THE GENERATOR CANNOT WRITE FOR ITSELF, and each is here for its own reason
    // rather than as an oversight:
    //
    //   FrameSpan packs two ints with the anchor flags in their sign bits, so writing "its fields"
    //   would write the packing rather than the meaning. It is written out honestly - start,
    //   duration, anchors - exactly as the level cache's codec wrote it, and for the same stated
    //   reason: nothing here has to be clever, so it says what it means. (The JSON side DOES pack,
    //   because a JSON array of two numbers is the format it already shipped.)
    //
    //   ModificationKey and RunProfile are readonly structs with get-only properties: real state,
    //   no setters, so a member-driven generator has nothing to assign on the way back in.
    //
    //   Pixel is four bytes seen as one int by design, and writing it any other way would cost four
    //   times the calls on an image-sized array.
    //
    // An id wrapper (ObjectId, ShapeId, every TypedResourceId) is NOT here: it is one int or one
    // Guid behind a public single-argument constructor, which the generator emits directly.

    /// <summary> Blob encodings for the model structs that write themselves by hand. </summary>
    public static class BlobPrimitives
    {
        #region FrameSpan

        public static void Write(ref BlobWriter writer, FrameSpan value)
        {
            writer.WriteInt(value.StartFrame);
            writer.WriteInt(value.FrameDuration);
            writer.WriteByte((byte)value.Anchors);
        }

        public static FrameSpan ReadFrameSpan(ref BlobReader reader)
        {
            var start = reader.ReadInt();
            var duration = reader.ReadInt();
            var anchors = (FrameAnchor)reader.ReadByte();
            // The constructor clamps, so no illegal span is representable however the bytes read.
            return new FrameSpan(start, duration, anchors);
        }

        #endregion

        #region ModificationKey

        public static void Write(ref BlobWriter writer, ModificationKey value)
        {
            writer.WriteInt(value.ObjectId.value);
            writer.WriteString(value.Path);
        }

        public static ModificationKey ReadModificationKey(ref BlobReader reader)
        {
            var objectId = new ObjectId(reader.ReadInt());
            return new ModificationKey(objectId, reader.ReadString());
        }

        #endregion

        #region RunProfile

        public static void Write(ref BlobWriter writer, RunProfile value)
        {
            writer.WriteInt(value.LifeCount);
            writer.WriteInt(value.SpeedCenti);
            writer.WriteBool(value.UseCheckpoints);
            writer.WriteByte((byte)value.Bot);
        }

        public static RunProfile ReadRunProfile(ref BlobReader reader)
        {
            var lives = reader.ReadInt();
            var speed = reader.ReadInt();
            var checkpoints = reader.ReadBool();
            var bot = (BotKind)reader.ReadByte();
            return new RunProfile(lives, speed, checkpoints, bot);
        }

        #endregion

        #region Pixel

        public static void Write(ref BlobWriter writer, Pixel value) => writer.WriteInt(value.rgba);

        public static Pixel ReadPixel(ref BlobReader reader) => new Pixel { rgba = reader.ReadInt() };

        #endregion
    }
}

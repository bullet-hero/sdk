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

        /// <summary> Start, duration and both anchors, written out honestly rather than as the packed pair. </summary>
        public static void Write(ref BlobWriter writer, FrameSpan value)
        {
            writer.WriteInt(value.StartFrame);
            writer.WriteInt(value.FrameDuration);
            writer.WriteByte((byte)value.Anchors);
        }

        /// <summary> Rebuilds a span from the four numbers, back through the constructor that clamps them. </summary>
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

        // THE INDEX IS WRITTEN LAST AND NEVER MOVES, per NAMING.md's positional rule: the blob
        // writes no names, so inserting a member anywhere but the end makes every trailing byte
        // mean something else. Field took the place the path held rather than being appended,
        // which is legal only because nothing on disk predates it.

        /// <summary> The template object it addresses, the field, and which element of it. </summary>
        public static void Write(ref BlobWriter writer, ModificationKey value)
        {
            writer.WriteInt(value.ObjectId.value);
            writer.WriteInt(value.Field);
            writer.WriteInt(value.Index);
        }

        /// <summary> Rebuilds the key through its constructor, in the order Write laid it down. </summary>
        public static ModificationKey ReadModificationKey(ref BlobReader reader)
        {
            var objectId = new ObjectId(reader.ReadInt());
            var field = reader.ReadInt();
            return new ModificationKey(objectId, field, reader.ReadInt());
        }

        #endregion

        #region RunProfile

        /// <summary> The four numbers a run is filed under: lives, speed, checkpoints, bot. </summary>
        public static void Write(ref BlobWriter writer, RunProfile value)
        {
            writer.WriteInt(value.LifeCount);
            writer.WriteInt(value.SpeedCenti);
            writer.WriteBool(value.UseCheckpoints);
            writer.WriteByte((byte)value.Bot);
        }

        /// <summary> Rebuilds the profile through its constructor, since its properties are get-only. </summary>
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

        /// <summary> One int, not four bytes - an image-sized array cannot afford four calls per pixel. </summary>
        public static void Write(ref BlobWriter writer, Pixel value) => writer.WriteInt(value.rgba);

        /// <summary> Reads the packed int back. </summary>
        public static Pixel ReadPixel(ref BlobReader reader) => new Pixel { rgba = reader.ReadInt() };

        #endregion
    }
}

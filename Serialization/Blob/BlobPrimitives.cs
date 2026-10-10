using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Statistics;
using BH.SDK.Versions.V1;

namespace BH.SDK.Serialization.Blob
{
    // The structs the generator cannot write for itself: FrameSpan (written as start, duration, anchors,
    // not as its packing), ModificationKey and RunProfile (get-only), Pixel (four bytes as one int).
    // An id wrapper is not here: the generator emits its public single-argument constructor.

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

        // The index is written LAST and never moves (NAMING.md's positional rule): the blob writes no
        // names, so inserting a member anywhere but the end shifts every trailing byte.

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

        /// <summary> The five things a run is filed under: lives, speed, checkpoints, bot, collision. </summary>
        public static void Write(ref BlobWriter writer, RunProfile value)
        {
            writer.WriteInt(value.LifeCount);
            writer.WriteInt(value.SpeedCenti);
            writer.WriteBool(value.UseCheckpoints);
            writer.WriteByte((byte)value.Bot);
            writer.WriteBool(value.NoCollision);
        }

        /// <summary> Rebuilds the profile through its constructor, since its properties are get-only. </summary>
        public static RunProfile ReadRunProfile(ref BlobReader reader)
        {
            var lives = reader.ReadInt();
            var speed = reader.ReadInt();
            var checkpoints = reader.ReadBool();
            var bot = (BotKind)reader.ReadByte();
            var noCollision = reader.ReadBool();
            return new RunProfile(lives, speed, checkpoints, bot, noCollision);
        }

        // THE SHAPE LevelStatistics GENERATION 1 WROTE, frozen beside the live one: four numbers, no
        // collision switch. Only the V1 snapshot reaches these.

        /// <summary> A generation-1 profile: lives, speed, checkpoints, bot. </summary>
        public static void Write(ref BlobWriter writer, RunProfileV1 value)
        {
            writer.WriteInt(value.LifeCount);
            writer.WriteInt(value.SpeedCenti);
            writer.WriteBool(value.UseCheckpoints);
            writer.WriteByte((byte)value.Bot);
        }

        /// <summary> Reads a generation-1 profile. </summary>
        public static RunProfileV1 ReadRunProfileV1(ref BlobReader reader)
        {
            var lives = reader.ReadInt();
            var speed = reader.ReadInt();
            var checkpoints = reader.ReadBool();
            var bot = (BotKind)reader.ReadByte();
            return new RunProfileV1(lives, speed, checkpoints, bot);
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

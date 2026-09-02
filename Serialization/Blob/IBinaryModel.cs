using System;

namespace BH.SDK.Serialization.Blob
{
    // A SECOND INTERFACE RATHER THAN TWO MORE MEMBERS ON IModel<T>, and the split is the point:
    // IModel is about what a model IS to the rest of the program - copy it, compare it, reset it,
    // make it become another instance - and none of that has an opinion about bytes. A model that
    // is never serialized still wants all five; a codec that changes never wants any of them.
    // Keeping them apart is also what lets a type opt out of one and not the other.

    /// <summary> A model that can write and read itself in the .blob format. Implemented by
    /// BH.SDK.Roslyn for every [GenerateModel] type; the three hand-written models
    /// (FrameSpan, ModificationKey, Modification) go through BlobPrimitives instead. </summary>
    public interface IBinaryModel
    {
        /// <summary> Appends this model to the writer. </summary>
        void Write(ref BlobWriter writer);

        /// <summary> Reads one back over this instance. </summary>
        void Read(ref BlobReader reader);
    }

    /// <summary> The two encodings a generated body needs that are not a model's own. </summary>
    public static class BlobModels
    {
        /// <summary> A model whose declared type is sealed: one presence byte and its own bytes,
        /// with no discriminator, because there is nothing else it could be. </summary>
        public static T Read<T>(ref BlobReader reader) where T : class, IBinaryModel, new()
        {
            if (!reader.ReadBool()) return null;
            var value = new T();
            value.Read(ref reader);
            return value;
        }
    }

    /// <summary> System.Version, as the text it round-trips through exactly. </summary>
    public static class BlobVersions
    {
        public static Version Read(ref BlobReader reader)
        {
            var text = reader.ReadString();
            if (text is null) return null;
            if (!Version.TryParse(text, out var version))
                throw new BlobFormatException($"'{text}' is not a version");
            return version;
        }
    }
}

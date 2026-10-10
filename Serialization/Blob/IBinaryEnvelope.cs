namespace BH.SDK.Serialization.Blob
{
    // The content's own entry point, because IBinaryModel.Read of a root reads the ENVELOPE first and a
    // migrating reader has already consumed it. JSON needs nothing like it: its envelope is written
    // by whoever holds the member.

    /// <summary> An aggregate root that can be read WITHOUT its envelope, for a caller that has
    /// already read one. Implemented by BH.SDK.Roslyn for every [ModelGeneration] type. </summary>
    public interface IBinaryEnvelope
    {
        /// <summary> Reads this model's content back over itself, envelope excluded. </summary>
        void ReadContent(ref BlobReader reader);
    }
}

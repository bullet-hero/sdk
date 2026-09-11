namespace BH.SDK.Serialization.Blob
{
    // WHY THE CONTENT NEEDS ITS OWN ENTRY POINT, AND WHY IBinaryModel.Read IS NOT IT. An aggregate
    // root's generated Read reads the ENVELOPE first - domain, generation, length - and only then
    // the content. A migrating reader has already consumed that envelope, because the generation is
    // what told it to migrate at all; handing the snapshot Read would make it look for a second one.
    //
    // The asymmetry with JSON is worth knowing before assuming this is symmetric work: there the
    // envelope is written by whoever HOLDS the member (JsonModels.WriteEnvelope) and a generated
    // ReadJson is already the plain payload reader, so nothing like this is needed on that side.

    /// <summary> An aggregate root that can be read WITHOUT its envelope, for a caller that has
    /// already read one. Implemented by BH.SDK.Roslyn for every [ModelGeneration] type. </summary>
    public interface IBinaryEnvelope
    {
        /// <summary> Reads this model's content back over itself, envelope excluded. </summary>
        void ReadContent(ref BlobReader reader);
    }
}

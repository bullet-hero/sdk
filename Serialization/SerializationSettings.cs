using Newtonsoft.Json;

namespace BH.SDK.Serialization
{
    // There is no formatting setting: every JSON document written is compact (Serialization/CLAUDE.md).

    /// <summary> What a <see cref="SerializationService"/> is built with. </summary>
    [System.Serializable]
    public class SerializationSettings
    {
        /// <summary> Whether a document may carry .NET type names. Off - the format names its own types. </summary>
        public TypeNameHandling typeNameHandling;

        /// <summary> Opt-in, so a member reaches a file only by carrying a <c>[JsonProperty]</c>. </summary>
        public MemberSerialization memberSerialization;

        // Exists for the one test that needs both answers: a change to how a level is READ is locked by
        // comparing the same bytes through both paths. Nothing in the game turns it off.

        /// <summary> Whether models read and write through their generated codecs. Off is how the one parity test reaches the reflective path; nothing in the game ever turns it off. </summary>
        public bool useGeneratedCodecs;

        /// <summary> What the game itself runs with. </summary>
        public SerializationSettings()
        {
            typeNameHandling = TypeNameHandling.None;
            memberSerialization = MemberSerialization.OptIn;
            useGeneratedCodecs = true;
        }
        /// <summary> Each rule spelled out, which only the parity test needs. </summary>
        public SerializationSettings(TypeNameHandling typeNameHandling = TypeNameHandling.None,
            MemberSerialization memberSerialization = MemberSerialization.OptIn,
            bool useGeneratedCodecs = true)
        {
            this.typeNameHandling = typeNameHandling;
            this.memberSerialization = memberSerialization;
            this.useGeneratedCodecs = useGeneratedCodecs;
        }
    }
}
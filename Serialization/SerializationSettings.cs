using Newtonsoft.Json;

namespace BH.SDK.Serialization
{
    // Formatting is not here, and is not anywhere any more: every JSON document this project
    // writes is compact. The mode that wrote an indented one is gone - nothing could recover the
    // choice from a file, so it described the person saving rather than the level, and reading one
    // by eye is what an editor's formatter is for.

    /// <summary> What a <see cref="SerializationService"/> is built with. </summary>
    [System.Serializable]
    public class SerializationSettings
    {
        /// <summary> Whether a document may carry .NET type names. Off - the format names its own types. </summary>
        public TypeNameHandling typeNameHandling;

        /// <summary> Opt-in, so a member reaches a file only by carrying a <c>[JsonProperty]</c>. </summary>
        public MemberSerialization memberSerialization;

        // THE ONLY REASON THIS SWITCH EXISTS IS THE ONE TEST THAT NEEDS BOTH ANSWERS. Anything that
        // changes how a level is READ has to be locked by a test comparing the same bytes through
        // the old path and the new one - the rule a withdrawn reader bought this project after it
        // passed 4494 tests and shipped a game that could not open a level. Turning this off is how
        // that test gets the old path; nothing in the game ever does.

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
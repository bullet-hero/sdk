using Newtonsoft.Json;

namespace BH.SDK.Serialization
{
    // Formatting is not here, and is not anywhere any more: every JSON document this project
    // writes is compact. The mode that wrote an indented one is gone - nothing could recover the
    // choice from a file, so it described the person saving rather than the level, and reading one
    // by eye is what an editor's formatter is for.
    [System.Serializable]
    public class SerializationSettings
    {
        public TypeNameHandling typeNameHandling;
        public MemberSerialization memberSerialization;

        // THE ONLY REASON THIS SWITCH EXISTS IS THE ONE TEST THAT NEEDS BOTH ANSWERS. Anything that
        // changes how a level is READ has to be locked by a test comparing the same bytes through
        // the old path and the new one - the rule a withdrawn reader bought this project after it
        // passed 4494 tests and shipped a game that could not open a level. Turning this off is how
        // that test gets the old path; nothing in the game ever does.
        public bool useGeneratedCodecs;

        public SerializationSettings()
        {
            typeNameHandling = TypeNameHandling.None;
            memberSerialization = MemberSerialization.OptIn;
            useGeneratedCodecs = true;
        }
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
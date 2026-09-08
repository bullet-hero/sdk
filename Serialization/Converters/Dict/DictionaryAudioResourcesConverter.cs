using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's audio resources, keyed by the id each one already carries. </summary>
    public class DictionaryAudioResourcesConverter : DictionaryAsListConverter<AudioResourceId, AudioResource>
    {
        /// <summary> The id every value already carries - its <c>AudioResourceId</c>. </summary>
        protected override AudioResourceId GetKey(AudioResource value) => value.AudioResourceId;
    }
}
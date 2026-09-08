using BH.SDK.Models.Audio;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's audio tracks, keyed by the id each one already carries. </summary>
    public class DictionaryAudiosConverter : DictionaryAsListConverter<AudioId, LevelTrack>
    {
        /// <summary> The id every value already carries - its <c>AudioId</c>. </summary>
        protected override AudioId GetKey(LevelTrack value) => value.AudioId;
    }
}
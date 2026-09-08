using BH.SDK.Models.Statistics;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> Per-checkpoint death tallies, keyed by the frame each one already carries. </summary>
    public class DictionaryCheckpointDeathsConverter : DictionaryAsListConverter<int, CheckpointDeaths>
    {
        /// <summary> The id every value already carries - its <c>Frame</c>. </summary>
        protected override int GetKey(CheckpointDeaths value) => value.Frame;
    }
}

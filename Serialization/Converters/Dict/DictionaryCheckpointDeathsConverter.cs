using BH.SDK.Models.Statistics;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    public class DictionaryCheckpointDeathsConverter : DictionaryAsListConverter<int, CheckpointDeaths>
    {
        protected override int GetKey(CheckpointDeaths value) => value.Frame;
    }
}

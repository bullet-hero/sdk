using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's effects, keyed by the id each one already carries. </summary>
    public class DictionaryEffectsConverter : DictionaryAsListConverter<EffectId, EffectData>
    {
        /// <summary> The id every value already carries - its <c>EffectId</c>. </summary>
        protected override EffectId GetKey(EffectData value) => value.EffectId;
    }
}
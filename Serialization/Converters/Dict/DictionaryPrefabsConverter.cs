using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's prefab templates, keyed by the id each one already carries. </summary>
    public class DictionaryPrefabsConverter : DictionaryAsListConverter<PrefabId, Prefab>
    {
        /// <summary> The id every value already carries - its <c>PrefabId</c>. </summary>
        protected override PrefabId GetKey(Prefab value) => value.PrefabId;
    }
}
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A prefab placement's per-instance overrides, keyed by the key each one already carries. </summary>
    public class DictionaryModificationsConverter : DictionaryAsListConverter<ModificationKey, Modification>
    {
        /// <summary> The id every value already carries - its <c>Key</c>. </summary>
        protected override ModificationKey GetKey(Modification value) => value.Key;
    }
}

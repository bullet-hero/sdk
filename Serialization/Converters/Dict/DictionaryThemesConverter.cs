using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's themes, keyed by the id each one already carries. </summary>
    public class DictionaryThemesConverter : DictionaryAsListConverter<ThemeId, ThemeData>
    {
        /// <summary> The id every value already carries - its <c>ThemeId</c>. </summary>
        protected override ThemeId GetKey(ThemeData value) => value.ThemeId;
    }
}
using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> Per-font glyph caches, keyed by the font id each one already carries. </summary>
    public class DictionaryCachedFontTextsConverter : DictionaryAsListConverter<FontResourceId, CachedFontText>
    {
        /// <summary> The id every value already carries - its <c>FontResourceId</c>. </summary>
        protected override FontResourceId GetKey(CachedFontText value) => value.FontResourceId;
    }
}

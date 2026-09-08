using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's font resources, keyed by the id each one already carries. </summary>
    public class DictionaryFontResourcesConverter : DictionaryAsListConverter<FontResourceId, FontResource>
    {
        /// <summary> The id every value already carries - its <c>FontResourceId</c>. </summary>
        protected override FontResourceId GetKey(FontResource value) => value.FontResourceId;
    }
}
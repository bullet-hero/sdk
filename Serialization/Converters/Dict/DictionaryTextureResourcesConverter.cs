using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's image resources, keyed by the id each one already carries. </summary>
    public class DictionaryTextureResourcesConverter : DictionaryAsListConverter<TextureResourceId, TextureResource>
    {
        /// <summary> The id every value already carries - its <c>TextureResourceId</c>. </summary>
        protected override TextureResourceId GetKey(TextureResource value) => value.TextureResourceId;
    }
}
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A scope's objects, keyed by the id each one already carries. The bulk of a level file. </summary>
    public class DictionaryObjectsConverter : DictionaryAsListConverter<ObjectId, RectObject>
    {
        /// <summary> The id every value already carries - its <c>ObjectId</c>. </summary>
        protected override ObjectId GetKey(RectObject value) => value.ObjectId;
    }
}
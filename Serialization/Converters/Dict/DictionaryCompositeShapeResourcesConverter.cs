using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.Dict
{
    /// <summary> A level's own authored shapes, keyed by the id each one already carries. </summary>
    public class DictionaryCompositeShapeResourcesConverter : DictionaryAsListConverter<ShapeId, CompositeShape>
    {
        /// <summary> The id every value already carries - its <c>ShapeId</c>. </summary>
        protected override ShapeId GetKey(CompositeShape value) => value.ShapeId;
    }
}

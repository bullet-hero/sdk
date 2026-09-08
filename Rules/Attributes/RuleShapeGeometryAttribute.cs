using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    // A class rule rather than a property one, because the invariant spans the two halves of the
    // geometry: an index means nothing without the vertex list it points into, and a property rule
    // only ever receives one value.
    //
    // Every check here has a silent failure mode, which is why they are enforced rather than
    // documented: a point outside the box samples past its own atlas cell, a flipped triangle is
    // culled and simply missing, a degenerate one collides with nothing, an unreferenced point is
    // invisible to both paths, and an index past the end would throw deep inside a bake.
    //
    // Fix delegates to ShapeGeometryUtils.Sanitize - the same call the in-game shape editor makes on
    // Save. That sharing is the point: a shape the editor accepts must be a shape validation
    // accepts, and two implementations would eventually disagree about which.

    /// <summary>
    /// Indexed shape geometry must be well formed: whole triples, in-range indices, points inside
    /// the authored box, no degenerate or back-facing triangles, no unreferenced points.
    /// </summary>
    [AttributeUsage(ClassTarget)]
    public class RuleShapeGeometryAttribute : BaseObjectRuleAttribute
    {
        /// <summary> <c>"rule_shape_geometry"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_shape_geometry";

        /// <summary> Sits on a type carrying indexed geometry. </summary>
        protected override bool IsValidTypeInternal(Type type)
            => typeof(IShapeGeometry).IsAssignableFrom(type);

        /// <summary> Passes when the vertices and the index triples describe real triangles inside the authored box. </summary>
        protected override bool IsValidInternal(object target, RuleContext context)
        {
            if (target is not IShapeGeometry geometry) return false;
            return ShapeGeometryUtils.Analyze(geometry.Vertices, geometry.Indices).IsClean;
        }

        // Sanitize repairs everything it can and leaves alone the one thing it must not invent: a
        // shape with no triangles at all stays empty, so this reports again next run rather than
        // fabricating geometry nobody authored.

        /// <summary> Sanitizes the pair - drops degenerate triangles and indices pointing at nothing. </summary>
        protected override void FixInternal(object target, RuleContext context)
        {
            if (target is not IShapeGeometry geometry) return;
            ShapeGeometryUtils.Sanitize(geometry.Vertices, geometry.Indices);
        }
    }
}

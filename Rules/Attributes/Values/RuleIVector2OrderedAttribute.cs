using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;

namespace BH.SDK.Rules.Attributes
{
    // Several IVector2 properties are not points at all - they are ranges packed into one value,
    // where X is the low end and Y the high one: EffectObjectCore.LifetimeBounds, every
    // *CurvesBySpeed.SpeedRange, ShadowsMidtonesHighlightsKey's two limit pairs. Nothing checked
    // that the low end was actually below the high one, so an inverted pair authored by hand (or
    // produced by dragging a slider past its partner) describes an empty range that every consumer
    // interprets differently - some clamp, some produce nothing, some divide by its zero width.
    //
    // The random variants are handled strictly: the rule holds only if EVERY value the range can
    // roll is ordered, which means comparing the highest possible X against the lowest possible Y.
    // A rect whose X and Y spans overlap can roll an inverted pair, so it fails - being lenient
    // there would mean the rule guarantees nothing for exactly the authored data that is hardest to
    // eyeball.

    /// <summary> An IVector2 used as a [X..Y] range must have X &lt;= Y, for every value it can
    /// produce. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIVector2OrderedAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ivector2_ordered"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ivector2_ordered";

        /// <summary> Applies to authored 2D vector properties used as a low/high pair. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IVector2).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes while X is at or below Y. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IVector2 vec) return false;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector2Value)value;
                    return valueVec.X <= valueVec.Y;
                }
                case VectorType.RandomRect:
                {
                    var rect = (Vector2Rect)value;
                    return rect.MaxX <= rect.MinY;
                }
                case VectorType.RandomRectStep:
                {
                    var rect = (Vector2RectStep)value;
                    return rect.MaxX <= rect.MinY;
                }
                // A circle rolls X and Y from the same disc, so the only way every sample can be
                // ordered is for the whole X extent to sit below the whole Y extent.
                case VectorType.RandomCircle:
                {
                    var circle = (Vector2Circle)value;
                    return circle.X + circle.Radius <= circle.Y - circle.Radius;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        // Swap rather than clamp: an inverted pair is almost always the same two numbers in the
        // wrong order, and swapping keeps both authored values instead of collapsing the range.

        /// <summary> Swaps the two, which keeps both numbers the author wrote. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IVector2 vec) return;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector2Value)value;
                    if (valueVec.X <= valueVec.Y) return;

                    (valueVec.X, valueVec.Y) = (valueVec.Y, valueVec.X);
                    break;
                }
                case VectorType.RandomRect:
                {
                    var rect = (Vector2Rect)value;
                    if (rect.MaxX <= rect.MinY) return;

                    (rect.MinX, rect.MinY) = (rect.MinY, rect.MinX);
                    (rect.MaxX, rect.MaxY) = (rect.MaxY, rect.MaxX);
                    break;
                }
                case VectorType.RandomRectStep:
                {
                    var rect = (Vector2RectStep)value;
                    if (rect.MaxX <= rect.MinY) return;

                    (rect.MinX, rect.MinY) = (rect.MinY, rect.MinX);
                    (rect.MaxX, rect.MaxY) = (rect.MaxY, rect.MaxX);
                    break;
                }
                // Swapping the centre does not help a circle: both components are drawn from one
                // radius, so any overlap survives it. Collapsing the radius is the only repair that
                // makes every sample ordered, and it keeps the authored centre.
                case VectorType.RandomCircle:
                {
                    var circle = (Vector2Circle)value;
                    if (circle.X + circle.Radius <= circle.Y - circle.Radius) return;

                    if (circle.X > circle.Y) (circle.X, circle.Y) = (circle.Y, circle.X);
                    circle.Radius = 0f;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

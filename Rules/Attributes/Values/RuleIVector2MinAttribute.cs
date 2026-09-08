using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored 2D vector with a per-axis floor, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIVector2MinAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ivector2_min"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ivector2_min";

        // always include

        /// <summary> Lower bound on the X axis. </summary>
        public float MinX { get; set; }

        /// <summary> Lower bound on the Y axis. </summary>
        public float MinY { get; set; }
        
        /// <summary> One bound, applied to every axis. </summary>
        public RuleIVector2MinAttribute(float min)
        {
            MinX = min;
            MinY = min;
        }
        /// <summary> Takes the lower bound on X and the lower bound on Y. </summary>
        public RuleIVector2MinAttribute(float minX, float minY)
        {
            MinX = minX;
            MinY = minY;
        }

        /// <summary> Applies to authored 2D vector properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IVector2).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when both axes are at or above their bounds. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IVector2 vec) return false;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector2Value)value;
                    if (valueVec.X < MinX) return false;
                    if (valueVec.Y < MinY) return false;
                    return true;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector2Rect)value;
                    if (randomRect.MinX < MinX) return false;
                    if (randomRect.MinY < MinY) return false;
                    return true;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector2RectStep)value;
                    if (randomRectStep.MinX < MinX) return false;
                    if (randomRectStep.MinY < MinY) return false;
                    return true;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector2Circle)value;
                    if (randomCircle.X - randomCircle.Radius < MinX) return false;
                    if (randomCircle.Y - randomCircle.Radius < MinY) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it up to them. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IVector2 vec) return;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector2Value)value;
                    if (valueVec.X < MinX) valueVec.X = BHSDKMath.Max(valueVec.X, MinX);
                    if (valueVec.Y < MinY) valueVec.Y = BHSDKMath.Max(valueVec.Y, MinY);
                    break;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector2Rect)value;
                    if (randomRect.MinX < MinX) randomRect.MinX = MinX;
                    if (randomRect.MinY < MinY) randomRect.MinY = MinY;
                    break;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector2RectStep)value;
                    if (randomRectStep.MinX < MinX) randomRectStep.MinX = MinX;
                    if (randomRectStep.MinY < MinY) randomRectStep.MinY = MinY;
                    break;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector2Circle)value;
                    if (randomCircle.X - randomCircle.Radius < MinX) randomCircle.X = MinX + randomCircle.Radius;
                    if (randomCircle.Y - randomCircle.Radius < MinY) randomCircle.Y = MinY + randomCircle.Radius;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

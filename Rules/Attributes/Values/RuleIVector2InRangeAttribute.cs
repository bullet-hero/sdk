using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored 2D vector bounded on both axes, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIVector2InRangeAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ivector2_in_range"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ivector2_in_range";

        // always include

        /// <summary> Lower bound on the X axis. </summary>
        public float MinX { get; set; }

        /// <summary> Upper bound on the X axis. </summary>
        public float MaxX { get; set; }

        /// <summary> Lower bound on the Y axis. </summary>
        public float MinY { get; set; }

        /// <summary> Upper bound on the Y axis. </summary>
        public float MaxY { get; set; }
        
        /// <summary> Width of the range on the X axis. </summary>
        public float DiffX => MaxX - MinX;
        /// <summary> Width of the range on the Y axis. </summary>
        public float DiffY => MaxY - MinY;
        /// <summary> Half the width of the range on the X axis - the offset from its midpoint to either end. </summary>
        public float HalfDiffX => (MaxX - MinX) / 2f;
        /// <summary> Half the width of the range on the Y axis - the offset from its midpoint to either end. </summary>
        public float HalfDiffY => (MaxY - MinY) / 2f;

        /// <summary> Both bounds, as <c>float</c>. </summary>
        public RuleIVector2InRangeAttribute(float min, float max)
        {
            MinX = min;
            MaxX = max;
            MinY = min;
            MaxY = max;
        }
        /// <summary> Takes the lower bound on X, the upper bound on X, the lower bound on Y and the upper bound on Y. </summary>
        public RuleIVector2InRangeAttribute(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        /// <summary> Applies to authored 2D vector properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IVector2).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every point the value can produce lies inside the bounds on both axes. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IVector2 vec) return false;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector2Value)value;
                    if (valueVec.X < MinX || valueVec.X > MaxX) return false;
                    if (valueVec.Y < MinY || valueVec.Y > MaxY) return false;
                    return true;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector2Rect)value;
                    if (randomRect.MinX < MinX || randomRect.MaxX > MaxX) return false;
                    if (randomRect.MinY < MinY || randomRect.MaxY > MaxY) return false;
                    return true;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector2RectStep)value;
                    if (randomRectStep.MinX < MinX || randomRectStep.MaxX > MaxX) return false;
                    if (randomRectStep.MinY < MinY || randomRectStep.MaxY > MaxY) return false;
                    return true;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector2Circle)value;
                    if (randomCircle.Radius > HalfDiffX) return false;
                    if (randomCircle.Radius > HalfDiffY) return false;
                    if (randomCircle.X - randomCircle.Radius < MinX) return false;
                    if (randomCircle.X + randomCircle.Radius > MaxX) return false;
                    if (randomCircle.Y - randomCircle.Radius < MinY) return false;
                    if (randomCircle.Y + randomCircle.Radius > MaxY) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it into the box. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IVector2 vec) return;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector2Value)value;
                    if (valueVec.X < MinX || valueVec.X > MaxX) valueVec.X = BHSDKMath.Clamp(valueVec.X, MinX, MaxX);
                    if (valueVec.Y < MinY || valueVec.Y > MaxY) valueVec.Y = BHSDKMath.Clamp(valueVec.Y, MinY, MaxY);
                    break;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector2Rect)value;
                    if (randomRect.MinX < MinX) randomRect.MinX = MinX;
                    if (randomRect.MaxX > MaxX) randomRect.MaxX = MaxX;
                    if (randomRect.MinY < MinY) randomRect.MinY = MinY;
                    if (randomRect.MaxY > MaxY) randomRect.MaxY = MaxY;
                    break;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector2RectStep)value;
                    if (randomRectStep.MinX < MinX) randomRectStep.MinX = MinX;
                    if (randomRectStep.MaxX > MaxX) randomRectStep.MaxX = MaxX;
                    if (randomRectStep.MinY < MinY) randomRectStep.MinY = MinY;
                    if (randomRectStep.MaxY > MaxY) randomRectStep.MaxY = MaxY;
                    break;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector2Circle)value;
                    
                    if (randomCircle.Radius > HalfDiffX) randomCircle.Radius = HalfDiffX;
                    else if (randomCircle.Radius > HalfDiffY) randomCircle.Radius = HalfDiffY;
                    
                    if (randomCircle.X - randomCircle.Radius < MinX)      randomCircle.X = MinX + randomCircle.Radius;
                    else if (randomCircle.X + randomCircle.Radius > MaxX) randomCircle.X = MaxX - randomCircle.Radius;
                    if (randomCircle.Y - randomCircle.Radius < MinY)      randomCircle.Y = MinY + randomCircle.Radius;
                    else if (randomCircle.Y + randomCircle.Radius > MaxY) randomCircle.Y = MaxY - randomCircle.Radius;
                    
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

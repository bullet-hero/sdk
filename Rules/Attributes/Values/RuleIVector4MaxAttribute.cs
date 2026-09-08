using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored 4D vector with a per-axis ceiling, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIVector4MaxAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ivector4_max"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ivector4_max";

        // always include

        /// <summary> Upper bound on the X axis. </summary>
        public float MaxX { get; set; }

        /// <summary> Upper bound on the Y axis. </summary>
        public float MaxY { get; set; }

        /// <summary> Upper bound on the Z axis. </summary>
        public float MaxZ { get; set; }

        /// <summary> Upper bound on the W component. </summary>
        public float MaxW { get; set; }
        
        /// <summary> One bound, applied to every axis. </summary>
        public RuleIVector4MaxAttribute(float max)
        {
            MaxX = max;
            MaxY = max;
            MaxZ = max;
            MaxW = max;
        }
        /// <summary> Takes the upper bound on X, the upper bound on Y, the upper bound on Z and the upper bound on W. </summary>
        public RuleIVector4MaxAttribute(float maxX, float maxY, float maxZ, float maxW)
        {
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
            MaxW = maxW;
        }

        /// <summary> Applies to authored 4D vector properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IVector4).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every component is at or below its bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IVector4 vec) return false;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector4Value)value;
                    if (valueVec.X > MaxX) return false;
                    if (valueVec.Y > MaxY) return false;
                    if (valueVec.Z > MaxZ) return false;
                    if (valueVec.W > MaxW) return false;
                    return true;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector4Rect)value;
                    if (randomRect.MaxX > MaxX) return false;
                    if (randomRect.MaxY > MaxY) return false;
                    if (randomRect.MaxZ > MaxZ) return false;
                    if (randomRect.MaxW > MaxW) return false;
                    return true;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector4RectStep)value;
                    if (randomRectStep.MaxX > MaxX) return false;
                    if (randomRectStep.MaxY > MaxY) return false;
                    if (randomRectStep.MaxZ > MaxZ) return false;
                    if (randomRectStep.MaxW > MaxW) return false;
                    return true;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector4Circle)value;
                    if (randomCircle.X + randomCircle.Radius > MaxX) return false;
                    if (randomCircle.Y + randomCircle.Radius > MaxY) return false;
                    if (randomCircle.Z + randomCircle.Radius > MaxZ) return false;
                    if (randomCircle.W + randomCircle.Radius > MaxW) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it down to them. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IVector4 vec) return;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector4Value)value;
                    if (valueVec.X > MaxX) valueVec.X = BHSDKMath.Min(valueVec.X, MaxX);
                    if (valueVec.Y > MaxY) valueVec.Y = BHSDKMath.Min(valueVec.Y, MaxY);
                    if (valueVec.Z > MaxZ) valueVec.Z = BHSDKMath.Min(valueVec.Z, MaxZ);
                    if (valueVec.W > MaxW) valueVec.W = BHSDKMath.Min(valueVec.W, MaxW);
                    break;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector4Rect)value;
                    if (randomRect.MaxX > MaxX) randomRect.MaxX = MaxX;
                    if (randomRect.MaxY > MaxY) randomRect.MaxY = MaxY;
                    if (randomRect.MaxZ > MaxZ) randomRect.MaxZ = MaxZ;
                    if (randomRect.MaxW > MaxW) randomRect.MaxW = MaxW;
                    break;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector4RectStep)value;
                    if (randomRectStep.MaxX > MaxX) randomRectStep.MaxX = MaxX;
                    if (randomRectStep.MaxY > MaxY) randomRectStep.MaxY = MaxY;
                    if (randomRectStep.MaxZ > MaxZ) randomRectStep.MaxZ = MaxZ;
                    if (randomRectStep.MaxW > MaxW) randomRectStep.MaxW = MaxW;
                    break;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector4Circle)value;
                    if (randomCircle.X + randomCircle.Radius > MaxX) randomCircle.X = MaxX - randomCircle.Radius;
                    if (randomCircle.Y + randomCircle.Radius > MaxY) randomCircle.Y = MaxY - randomCircle.Radius;
                    if (randomCircle.Z + randomCircle.Radius > MaxZ) randomCircle.Z = MaxZ - randomCircle.Radius;
                    if (randomCircle.W + randomCircle.Radius > MaxW) randomCircle.W = MaxW - randomCircle.Radius;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored 4D vector with a per-axis floor, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIVector4MinAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ivector4_min"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ivector4_min";

        // always include

        /// <summary> Lower bound on the X axis. </summary>
        public float MinX { get; set; }

        /// <summary> Lower bound on the Y axis. </summary>
        public float MinY { get; set; }

        /// <summary> Lower bound on the Z axis. </summary>
        public float MinZ { get; set; }

        /// <summary> Lower bound on the W component. </summary>
        public float MinW { get; set; }

        /// <summary> One bound, applied to every axis. </summary>
        public RuleIVector4MinAttribute(float min)
        {
            MinX = min;
            MinY = min;
            MinZ = min;
            MinW = min;
        }
        /// <summary> Takes the lower bound on X, the lower bound on Y, the lower bound on Z and the lower bound on W. </summary>
        public RuleIVector4MinAttribute(float minX, float minY, float minZ, float minW)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            MinW = minW;
        }

        /// <summary> Applies to authored 4D vector properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IVector4).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every component is at or above its bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IVector4 vec) return false;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector4Value)value;
                    if (valueVec.X < MinX) return false;
                    if (valueVec.Y < MinY) return false;
                    if (valueVec.Z < MinZ) return false;
                    if (valueVec.W < MinW) return false;
                    return true;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector4Rect)value;
                    if (randomRect.MinX < MinX) return false;
                    if (randomRect.MinY < MinY) return false;
                    if (randomRect.MinZ < MinZ) return false;
                    if (randomRect.MinW < MinW) return false;
                    return true;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector4RectStep)value;
                    if (randomRectStep.MinX < MinX) return false;
                    if (randomRectStep.MinY < MinY) return false;
                    if (randomRectStep.MinZ < MinZ) return false;
                    if (randomRectStep.MinW < MinW) return false;
                    return true;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector4Circle)value;
                    if (randomCircle.X - randomCircle.Radius < MinX) return false;
                    if (randomCircle.Y - randomCircle.Radius < MinY) return false;
                    if (randomCircle.Z - randomCircle.Radius < MinZ) return false;
                    if (randomCircle.W - randomCircle.Radius < MinW) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it up to them. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IVector4 vec) return;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector4Value)value;
                    if (valueVec.X < MinX) valueVec.X = BHSDKMath.Max(valueVec.X, MinX);
                    if (valueVec.Y < MinY) valueVec.Y = BHSDKMath.Max(valueVec.Y, MinY);
                    if (valueVec.Z < MinZ) valueVec.Z = BHSDKMath.Max(valueVec.Z, MinZ);
                    if (valueVec.W < MinW) valueVec.W = BHSDKMath.Max(valueVec.W, MinW);
                    break;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector4Rect)value;
                    if (randomRect.MinX < MinX) randomRect.MinX = MinX;
                    if (randomRect.MinY < MinY) randomRect.MinY = MinY;
                    if (randomRect.MinZ < MinZ) randomRect.MinZ = MinZ;
                    if (randomRect.MinW < MinW) randomRect.MinW = MinW;
                    break;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector4RectStep)value;
                    if (randomRectStep.MinX < MinX) randomRectStep.MinX = MinX;
                    if (randomRectStep.MinY < MinY) randomRectStep.MinY = MinY;
                    if (randomRectStep.MinZ < MinZ) randomRectStep.MinZ = MinZ;
                    if (randomRectStep.MinW < MinW) randomRectStep.MinW = MinW;
                    break;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector4Circle)value;
                    if (randomCircle.X - randomCircle.Radius < MinX) randomCircle.X = MinX + randomCircle.Radius;
                    if (randomCircle.Y - randomCircle.Radius < MinY) randomCircle.Y = MinY + randomCircle.Radius;
                    if (randomCircle.Z - randomCircle.Radius < MinZ) randomCircle.Z = MinZ + randomCircle.Radius;
                    if (randomCircle.W - randomCircle.Radius < MinW) randomCircle.W = MinW + randomCircle.Radius;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

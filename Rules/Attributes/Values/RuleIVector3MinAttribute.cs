using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored 3D vector with a per-axis floor, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIVector3MinAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ivector3_min"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ivector3_min";

        // always include

        /// <summary> Lower bound on the X axis. </summary>
        public float MinX { get; set; }

        /// <summary> Lower bound on the Y axis. </summary>
        public float MinY { get; set; }

        /// <summary> Lower bound on the Z axis. </summary>
        public float MinZ { get; set; }

        /// <summary> One bound, applied to every axis. </summary>
        public RuleIVector3MinAttribute(float min)
        {
            MinX = min;
            MinY = min;
            MinZ = min;
        }
        /// <summary> Takes the lower bound on X, the lower bound on Y and the lower bound on Z. </summary>
        public RuleIVector3MinAttribute(float minX, float minY, float minZ)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
        }

        /// <summary> Applies to authored 3D vector properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IVector3).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every axis is at or above its bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IVector3 vec) return false;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector3Value)value;
                    if (valueVec.X < MinX) return false;
                    if (valueVec.Y < MinY) return false;
                    if (valueVec.Z < MinZ) return false;
                    return true;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector3Rect)value;
                    if (randomRect.MinX < MinX) return false;
                    if (randomRect.MinY < MinY) return false;
                    if (randomRect.MinZ < MinZ) return false;
                    return true;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector3RectStep)value;
                    if (randomRectStep.MinX < MinX) return false;
                    if (randomRectStep.MinY < MinY) return false;
                    if (randomRectStep.MinZ < MinZ) return false;
                    return true;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector3Circle)value;
                    if (randomCircle.X - randomCircle.Radius < MinX) return false;
                    if (randomCircle.Y - randomCircle.Radius < MinY) return false;
                    if (randomCircle.Z - randomCircle.Radius < MinZ) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it up to them. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IVector3 vec) return;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector3Value)value;
                    if (valueVec.X < MinX) valueVec.X = BHSDKMath.Max(valueVec.X, MinX);
                    if (valueVec.Y < MinY) valueVec.Y = BHSDKMath.Max(valueVec.Y, MinY);
                    if (valueVec.Z < MinZ) valueVec.Z = BHSDKMath.Max(valueVec.Z, MinZ);
                    break;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector3Rect)value;
                    if (randomRect.MinX < MinX) randomRect.MinX = MinX;
                    if (randomRect.MinY < MinY) randomRect.MinY = MinY;
                    if (randomRect.MinZ < MinZ) randomRect.MinZ = MinZ;
                    break;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector3RectStep)value;
                    if (randomRectStep.MinX < MinX) randomRectStep.MinX = MinX;
                    if (randomRectStep.MinY < MinY) randomRectStep.MinY = MinY;
                    if (randomRectStep.MinZ < MinZ) randomRectStep.MinZ = MinZ;
                    break;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector3Circle)value;
                    if (randomCircle.X - randomCircle.Radius < MinX) randomCircle.X = MinX + randomCircle.Radius;
                    if (randomCircle.Y - randomCircle.Radius < MinY) randomCircle.Y = MinY + randomCircle.Radius;
                    if (randomCircle.Z - randomCircle.Radius < MinZ) randomCircle.Z = MinZ + randomCircle.Radius;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

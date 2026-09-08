using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored 3D vector bounded on every axis, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIVector3InRangeAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ivector3_in_range"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ivector3_in_range";

        // always include

        /// <summary> Lower bound on the X axis. </summary>
        public float MinX { get; set; }

        /// <summary> Upper bound on the X axis. </summary>
        public float MaxX { get; set; }

        /// <summary> Lower bound on the Y axis. </summary>
        public float MinY { get; set; }

        /// <summary> Upper bound on the Y axis. </summary>
        public float MaxY { get; set; }

        /// <summary> Lower bound on the Z axis. </summary>
        public float MinZ { get; set; }

        /// <summary> Upper bound on the Z axis. </summary>
        public float MaxZ { get; set; }
        
        /// <summary> Width of the range on the X axis. </summary>
        public float DiffX => MaxX - MinX;
        /// <summary> Width of the range on the Y axis. </summary>
        public float DiffY => MaxY - MinY;
        /// <summary> Width of the range on the Z axis. </summary>
        public float DiffZ => MaxZ - MinZ;
        /// <summary> Half the width of the range on the X axis - the offset from its midpoint to either end. </summary>
        public float HalfDiffX => (MaxX - MinX) / 2f;
        /// <summary> Half the width of the range on the Y axis - the offset from its midpoint to either end. </summary>
        public float HalfDiffY => (MaxY - MinY) / 2f;
        /// <summary> Half the width of the range on the Z axis - the offset from its midpoint to either end. </summary>
        public float HalfDiffZ => (MaxZ - MinZ) / 2f;

        /// <summary> Both bounds, as <c>float</c>. </summary>
        public RuleIVector3InRangeAttribute(float min, float max)
        {
            MinX = min;
            MaxX = max;
            MinY = min;
            MaxY = max;
            MinZ = min;
            MaxZ = max;
        }
        /// <summary> Takes the lower bound on X, the upper bound on X, the lower bound on Y, the upper bound on Y, the lower bound on Z and the upper bound on Z. </summary>
        public RuleIVector3InRangeAttribute(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        /// <summary> Applies to authored 3D vector properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IVector3).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every point the value can produce lies inside the bounds on every axis. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IVector3 vec) return false;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector3Value)value;
                    if (valueVec.X < MinX || valueVec.X > MaxX) return false;
                    if (valueVec.Y < MinY || valueVec.Y > MaxY) return false;
                    if (valueVec.Z < MinZ || valueVec.Z > MaxZ) return false;
                    return true;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector3Rect)value;
                    if (randomRect.MinX < MinX || randomRect.MaxX > MaxX) return false;
                    if (randomRect.MinY < MinY || randomRect.MaxY > MaxY) return false;
                    if (randomRect.MinZ < MinZ || randomRect.MaxZ > MaxZ) return false;
                    return true;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector3RectStep)value;
                    if (randomRectStep.MinX < MinX || randomRectStep.MaxX > MaxX) return false;
                    if (randomRectStep.MinY < MinY || randomRectStep.MaxY > MaxY) return false;
                    if (randomRectStep.MinZ < MinZ || randomRectStep.MaxZ > MaxZ) return false;
                    return true;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector3Circle)value;
                    if (randomCircle.Radius > HalfDiffX) return false;
                    if (randomCircle.Radius > HalfDiffY) return false;
                    if (randomCircle.Radius > HalfDiffZ) return false;
                    if (randomCircle.X - randomCircle.Radius < MinX) return false;
                    if (randomCircle.X + randomCircle.Radius > MaxX) return false;
                    if (randomCircle.Y - randomCircle.Radius < MinY) return false;
                    if (randomCircle.Y + randomCircle.Radius > MaxY) return false;
                    if (randomCircle.Z - randomCircle.Radius < MinZ) return false;
                    if (randomCircle.Z + randomCircle.Radius > MaxZ) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it into the box. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IVector3 vec) return;

            switch (vec.GetModelType())
            {
                case VectorType.Value:
                {
                    var valueVec = (Vector3Value)value;
                    if (valueVec.X < MinX || valueVec.X > MaxX) valueVec.X = BHSDKMath.Clamp(valueVec.X, MinX, MaxX);
                    if (valueVec.Y < MinY || valueVec.Y > MaxY) valueVec.Y = BHSDKMath.Clamp(valueVec.Y, MinY, MaxY);
                    if (valueVec.Z < MinZ || valueVec.Z > MaxZ) valueVec.Z = BHSDKMath.Clamp(valueVec.Z, MinZ, MaxZ);
                    break;
                }
                case VectorType.RandomRect:
                {
                    var randomRect = (Vector3Rect)value;
                    if (randomRect.MinX < MinX) randomRect.MinX = MinX;
                    if (randomRect.MaxX > MaxX) randomRect.MaxX = MaxX;
                    if (randomRect.MinY < MinY) randomRect.MinY = MinY;
                    if (randomRect.MaxY > MaxY) randomRect.MaxY = MaxY;
                    if (randomRect.MinZ < MinZ) randomRect.MinZ = MinZ;
                    if (randomRect.MaxZ > MaxZ) randomRect.MaxZ = MaxZ;
                    break;
                }
                case VectorType.RandomRectStep:
                {
                    var randomRectStep = (Vector3RectStep)value;
                    if (randomRectStep.MinX < MinX) randomRectStep.MinX = MinX;
                    if (randomRectStep.MaxX > MaxX) randomRectStep.MaxX = MaxX;
                    if (randomRectStep.MinY < MinY) randomRectStep.MinY = MinY;
                    if (randomRectStep.MaxY > MaxY) randomRectStep.MaxY = MaxY;
                    if (randomRectStep.MinZ < MinZ) randomRectStep.MinZ = MinZ;
                    if (randomRectStep.MaxZ > MaxZ) randomRectStep.MaxZ = MaxZ;
                    break;
                }
                case VectorType.RandomCircle:
                {
                    var randomCircle = (Vector3Circle)value;
                    
                    if (randomCircle.Radius > HalfDiffX) randomCircle.Radius = HalfDiffX;
                    else if (randomCircle.Radius > HalfDiffY) randomCircle.Radius = HalfDiffY;
                    else if (randomCircle.Radius > HalfDiffZ) randomCircle.Radius = HalfDiffZ;
                    
                    if (randomCircle.X - randomCircle.Radius < MinX)      randomCircle.X = MinX + randomCircle.Radius;
                    else if (randomCircle.X + randomCircle.Radius > MaxX) randomCircle.X = MaxX - randomCircle.Radius;
                    if (randomCircle.Y - randomCircle.Radius < MinY)      randomCircle.Y = MinY + randomCircle.Radius;
                    else if (randomCircle.Y + randomCircle.Radius > MaxY) randomCircle.Y = MaxY - randomCircle.Radius;
                    if (randomCircle.Z - randomCircle.Radius < MinZ)      randomCircle.Z = MinZ + randomCircle.Radius;
                    else if (randomCircle.Z + randomCircle.Radius > MaxZ) randomCircle.Z = MaxZ - randomCircle.Radius;
                    
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}

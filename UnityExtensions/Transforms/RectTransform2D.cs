using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace BH.SDK.Transforms
{
    [Serializable]
    public struct RectTransform2D
    {
        public float2 position; // anchored position (offset from anchor point)
        public float layer; // position z
        public float rotation; // radians
        public float2 scale; // additional local scale
        [Space]
        public float2 size; // logical size of the rect
        public float2 anchorMin; // normalized anchor point (0..1), center is (0.5, 0.5)
        public float2 anchorMax; // normalized anchor point (0..1), center is (0.5, 0.5)
        public float2 pivot; // normalized pivot (0..1), center is (0.5, 0.5)

        public float2 Anchors
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (anchorMin + anchorMax) * 0.5f;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => anchorMin = anchorMax = value;
        }

        public float Aspect
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FullSize.x / FullSize.y;
        }
        public float2 FullSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => size * scale;
        }
        public float2 HalfSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => size * 0.5f;
        }
        public float2 HalfScale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => scale * 0.5f;
        }
        public float2 HalfFullSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FullSize * 0.5f;
        }
        public float3 Position3D
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(position.x, position.y, layer);
        }
        
        public static RectTransform2D Default => new(TransformDefaults.Position, TransformDefaults.Layer, TransformDefaults.Rotation,
            TransformDefaults.Size, TransformDefaults.Scale, TransformDefaults.AnchorMin, TransformDefaults.AnchorMax, TransformDefaults.Pivot);
        public static RectTransform2D Zero => new(TransformDefaults.Position, TransformDefaults.Layer, TransformDefaults.Rotation,
            float2.zero, TransformDefaults.Scale, TransformDefaults.AnchorMin, TransformDefaults.AnchorMax, TransformDefaults.Pivot);
        
        public RectTransform2D(float2 position)
        {
            this.position = position;
            layer = TransformDefaults.Layer;
            rotation = TransformDefaults.Rotation;
            size = TransformDefaults.Size;
            scale = TransformDefaults.Scale;
            anchorMin = TransformDefaults.AnchorMin;
            anchorMax = TransformDefaults.AnchorMax;
            pivot = TransformDefaults.Pivot;
        }
        public RectTransform2D(float2 position, float rotation)
        {
            this.position = position;
            layer = TransformDefaults.Layer;
            this.rotation = rotation;
            size = TransformDefaults.Size;
            scale = TransformDefaults.Scale;
            anchorMin = TransformDefaults.AnchorMin;
            anchorMax = TransformDefaults.AnchorMax;
            pivot = TransformDefaults.Pivot;
        }
        public RectTransform2D(float2 position, float rotation, float2 size)
        {
            this.position = position;
            layer = TransformDefaults.Layer;
            this.rotation = rotation;
            this.size = size;
            scale = TransformDefaults.Scale;
            anchorMin = TransformDefaults.AnchorMin;
            anchorMax = TransformDefaults.AnchorMax;
            pivot = TransformDefaults.Pivot;
        }
        public RectTransform2D(float2 position, float rotation, float2 size, float2 scale)
        {
            this.position = position;
            layer = TransformDefaults.Layer;
            this.rotation = rotation;
            this.size = size;
            this.scale = scale;
            anchorMin = TransformDefaults.AnchorMin;
            anchorMax = TransformDefaults.AnchorMax;
            pivot = TransformDefaults.Pivot;
        }
        public RectTransform2D(float2 position, float layer, float rotation, float2 size, float2 scale)
        {
            this.position = position;
            this.layer = layer;
            this.rotation = rotation;
            this.size = size;
            this.scale = scale;
            anchorMin = TransformDefaults.AnchorMin;
            anchorMax = TransformDefaults.AnchorMax;
            pivot = TransformDefaults.Pivot;
        }
        public RectTransform2D(float2 position, float layer, float rotation, float2 size, float2 scale,
            float2 anchorMin, float2 anchorMax, float2 pivot)
        {
            this.position = position;
            this.layer = layer;
            this.rotation = rotation;
            this.size = size;
            this.scale = scale;
            this.anchorMin = anchorMin;
            this.anchorMax = anchorMax;
            this.pivot = pivot;
        }
        public RectTransform2D(Transform transform, bool invertLayer = true)
        {
            var pos = transform.localPosition;
            var sca = transform.localScale;
            
            position = new float2(pos.x, pos.y);
            layer = invertLayer ? -pos.z : pos.z;
            rotation = math.radians(transform.localEulerAngles.z);
            size = TransformDefaults.Size;
            scale = new float2(sca.x, sca.y);
            anchorMin = TransformDefaults.AnchorMin;
            anchorMax = TransformDefaults.AnchorMax;
            pivot = TransformDefaults.Pivot;
        }
        public RectTransform2D(RectTransform rectTransform, bool invertLayer = true)
        {
            var pos = rectTransform.anchoredPosition3D;
            var sca = rectTransform.localScale;
            
            position = new float2(pos.x, pos.y);
            layer = invertLayer ? -pos.z : pos.z;
            rotation = math.radians(rectTransform.localEulerAngles.z);
            size = rectTransform.sizeDelta;
            scale = new float2(sca.x, sca.y);
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
            pivot = rectTransform.pivot;
        }

        public float4x4 GetRootMatrix()
        {
            math.sincos(rotation, out var sin, out var cos);
            float sx = scale.x * size.x, sy = scale.y * size.y;
            float px = pivot.x - 0.5f, py = pivot.y - 0.5f;
            float cosX = cos * sx, sinX = sin * sx;
            float sinY = -sin * sy, cosY = cos * sy;
            var x = position.x - cosX * px - sinY * py;
            var y = position.y - sinX * px - cosY * py;
            var z = layer;

            var trs = new float4x4(
                new float4(cosX, sinX, 0f, 0f),
                new float4(sinY, cosY, 0f, 0f),
                new float4(  0f,   0f, 1f, 0f),
                new float4(   x,    y,  z, 1f)
            );

            return trs;
        }

        public void Apply(RectTransform2D parent)
        {
            rotation += parent.rotation;
            scale *= parent.scale;

            var parentHalfFullSize = parent.HalfFullSize;
            math.sincos(parent.rotation, out var parentSin, out var parentCos);

            size += parent.size * (anchorMax - anchorMin);

            // step 1: parent pivot -> parent center
            var parentPivotPoint = math.lerp(-parentHalfFullSize, parentHalfFullSize, parent.pivot); // apply p.sca
            parentPivotPoint = Math2D.RotateVectorFast(parentPivotPoint, parentSin, parentCos); // apply p.rot
            var parentCenterPoint = parent.position - parentPivotPoint; // apply p.pos

            // step 2: parent center -> self pivot
            var anchorNormalized = math.lerp(anchorMin, anchorMax, pivot);
            var anchorLocal = math.lerp(-parentHalfFullSize, parentHalfFullSize, anchorNormalized); // apply p.sca
            anchorLocal = Math2D.RotateVectorFast(anchorLocal, parentSin, parentCos); // apply p.rot

            var selfOffset = position * parent.scale; // apply sca
            selfOffset = Math2D.RotateVectorFast(selfOffset, parentSin, parentCos); // apply rot

            position = selfOffset + anchorLocal + parentCenterPoint;
            // anchorMin/anchorMax is no changed, stays the same
            // pivot is no changed, stays the same
            layer += parent.layer;
        }

        /// <summary> Return center position for transform (local) </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 GetCenterPoint()
        {
            var pivotPoint = math.lerp(-HalfFullSize, HalfFullSize, pivot); // apply sca
            pivotPoint = Math2D.RotateVector(pivotPoint, rotation); // apply rot
            return position - pivotPoint; // apply pos
        }

        // The AABB, not the rect: a rotated rect no longer fits inside FullSize, and every caller
        // that has to CONTAIN it (framing a camera on an object, a bounds test) needs the box that
        // does. Scale is taken as an absolute value, since a mirrored object (negative scale) covers
        // exactly as much screen as an unmirrored one.

        /// <summary> Axis-aligned size the rect covers once rotated - equal to <see cref="FullSize"/>
        /// at zero rotation, larger at every other angle. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 GetBoundsSize()
        {
            var fullSize = math.abs(FullSize);
            math.sincos(rotation, out var sin, out var cos);
            var absSin = math.abs(sin);
            var absCos = math.abs(cos);

            return new float2(
                fullSize.x * absCos + fullSize.y * absSin,
                fullSize.x * absSin + fullSize.y * absCos);
        }

        /// <summary> Get global point for alignment (local) </summary>
        /// <param name="alignment">from (0, 0) to (1, 1), center is (0.5, 0.5)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 GetAlignmentPoint(float2 alignment)
        {
            var halfFullSize = HalfFullSize;
            math.sincos(rotation, out var sin, out var cos);
            
            var pivotPoint = math.lerp(-halfFullSize, halfFullSize, pivot); // apply sca
            pivotPoint = Math2D.RotateVectorFast(pivotPoint, sin, cos); // apply rot
            
            var alignmentPoint = math.lerp(-halfFullSize, halfFullSize, alignment); // apply sca
            alignmentPoint = Math2D.RotateVectorFast(alignmentPoint, sin, cos); // apply rot
            
            return position - pivotPoint + alignmentPoint; // apply both pos
        }

        public Rect GetRect()
        {
            var offset = -pivot * size;
            return new Rect(offset.x, offset.y, size.x, size.y);
        }

        public void GetCorners(NativeArray<float2> corners)
        {
            if (!corners.IsCreated || corners.Length < 4)
                throw new ArgumentOutOfRangeException(nameof(corners),
                    "Calling GetCorners() with an array that is created or has less than 4 elements");
            
            var rect = GetRect();
            float x = rect.x, y = rect.y;
            float xMax = rect.xMax, yMax = rect.yMax;
            
            corners[0] = new float2(x, y);
            corners[1] = new float2(x, yMax);
            corners[2] = new float2(xMax, yMax);
            corners[3] = new float2(xMax, y);
        }
        
        // Regular transforms is not InstanceTransform, ApplyTo functions like TRS - applied with pivot
        // because for them pivot is always in center (0.5, 0.5)

        public void ApplyTo(Transform transform)
        {
            var fullSize = FullSize;
            var halfFullSize = fullSize * 0.5f;
            var pivotPoint = math.lerp(-halfFullSize, halfFullSize, pivot); // apply sca
            pivotPoint = Math2D.RotateVector(pivotPoint, rotation); // apply rot
            
            var pos = new Vector3(position.x - pivotPoint.x, position.y - pivotPoint.y, layer);
            var rot = Math2D.RotateZ(rotation);
            var sca = new Vector3(fullSize.x, fullSize.y, 1f);

            transform.localScale = sca;
            transform.SetLocalPositionAndRotation(pos, rot);
        }
        public void ApplyTo(TransformHandle handle)
        {
            var fullSize = FullSize;
            var halfFullSize = fullSize * 0.5f;
            var pivotPoint = math.lerp(-halfFullSize, halfFullSize, pivot); // apply sca
            pivotPoint = Math2D.RotateVector(pivotPoint, rotation); // apply rot
            
            var pos = new Vector3(position.x - pivotPoint.x, position.y - pivotPoint.y, layer);
            var rot = Math2D.RotateZ(rotation);
            var sca = new Vector3(fullSize.x, fullSize.y, 1f);

            handle.localScale = sca;
            handle.SetLocalPositionAndRotation(pos, rot);
        }
        public void ApplyTo(TransformAccess access)
        {
            var fullSize = FullSize;
            var halfFullSize = fullSize * 0.5f;
            var pivotPoint = math.lerp(-halfFullSize, halfFullSize, pivot); // apply sca
            pivotPoint = Math2D.RotateVector(pivotPoint, rotation); // apply rot
            
            var pos = new Vector3(position.x - pivotPoint.x, position.y - pivotPoint.y, layer);
            var rot = Math2D.RotateZ(rotation);
            var sca = new Vector3(fullSize.x, fullSize.y, 1f);

            access.localScale = sca;
            access.SetLocalPositionAndRotation(pos, rot);
        }
        public void ApplyTo(RectTransform rectTransform)
        {
            rectTransform.localPosition = new Vector3(position.x, position.y, layer);
            rectTransform.localRotation = Math2D.RotateZ(rotation);
            rectTransform.sizeDelta = new Vector2(size.x, size.y);
            rectTransform.localScale = new Vector3(scale.x, scale.y, 1f);

            rectTransform.anchorMin = new Vector2(anchorMin.x, anchorMin.y);
            rectTransform.anchorMax = new Vector2(anchorMax.x, anchorMax.y);
            rectTransform.pivot = new Vector2(pivot.x, pivot.y);
        }
        public void ApplyTo(Camera camera, bool setAspect = false)
        {
            var fullSize = FullSize;
            var halfFullSize = fullSize * 0.5f;
            var pivotPoint = math.lerp(-halfFullSize, halfFullSize, pivot); // apply sca
            pivotPoint = Math2D.RotateVector(pivotPoint, rotation); // apply rot
            
            var pos = new Vector3(position.x - pivotPoint.x, position.y - pivotPoint.y, layer);
            var rot = Math2D.RotateZ(rotation);
            var sca = new Vector3(scale.x, scale.y, 1f);

            camera.transform.localScale = sca;
            camera.transform.SetLocalPositionAndRotation(pos, rot);
            
            if (setAspect)
            {
                camera.aspect = size.x / size.y;
                camera.orthographicSize = size.y * 0.5f;
            }
            else
            {
                // with this behaviour size can fully fit in camera
                var realAspect = camera.aspect;
                var targetAspect = size.x / size.y;

                if (targetAspect > realAspect)
                    camera.orthographicSize = size.x / realAspect * 0.5f; // width is bigger, take real height
                else camera.orthographicSize = size.y * 0.5f; // height is bigger, take target height
            }
        }
    }
}
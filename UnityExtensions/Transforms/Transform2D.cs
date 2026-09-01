using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace BH.SDK.Transforms
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = ByteSize)]
    public struct Transform2D
    {
        // size = 32 bytes, elements size = 24 bytes, padding = 8 bytes
        public const int ByteSize = 32;
        
        [FieldOffset(00)] public float2 position; // local position
        [FieldOffset(08)] public float  layer; // absolute position z
        [FieldOffset(12)] public float  rotation; // local radians
        [FieldOffset(16)] public float2 scale; // local scale

        public float2 HalfScale => scale * 0.5f;

        public Transform2D(float2 position, float rotation, float2 scale)
        {
            this.position = position;
            layer = 0f;
            this.rotation = rotation;
            this.scale = scale;
        }
        public Transform2D(float2 position, float layer, float rotation, float2 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.layer = layer;
        }

        public float4x4 TRS()
        {
            math.sincos(rotation, out var sin, out var cos);
            float cosX =  cos * scale.x, sinX = sin * scale.x;
            float sinY = -sin * scale.y, cosY = cos * scale.y;
            float x = position.x, y = position.y, z = layer;
            
            var trs = new float4x4(
                new float4(cosX, sinX, 0f, 0f),
                new float4(sinY, cosY, 0f, 0f),
                new float4(  0f,   0f, 1f, 0f),
                new float4(   x,    y,  z, 1f)
            );
            return trs;
        }

        public void Apply(Transform2D parent)
        {
            scale *= parent.scale;
            rotation += parent.rotation;
            
            var pos = position * parent.scale; // apply p.sca
            pos = Math2D.RotateVector(pos, parent.rotation); // apply p.rot
            pos += parent.position; // apply p.pos
            position = pos;
            
            // layer is absolute, no changes
        }

        /// <summary> Get global point for alignment </summary>
        /// <param name="alignment">from (0, 0) to (1, 1), center is (0.5, 0.5)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 GetAlignmentPoint(float2 alignment)
        {
            var halfScale = HalfScale;
            float sinRot = math.sin(rotation), cosRot = math.cos(rotation);
            
            var alignmentPoint = math.lerp(-halfScale, halfScale, alignment); // apply sca
            alignmentPoint = Math2D.RotateVectorFast(alignmentPoint, sinRot, cosRot); // apply rot
            
            return position + alignmentPoint; // apply both pos
        }
        
        // Regular transforms is not InstanceTransform, ApplyTo functions like TRS - applied with pivot
        // because for them pivot is always in center (0.5, 0.5)
        
        public void ApplyTo(TransformHandle handle)
        {
            var pos = new Vector3(position.x, position.y, layer);
            var rot = Math2D.RotateZ(rotation);
            var sca = new Vector3(scale.x, scale.y, 1f);

            handle.localScale = sca;
            handle.SetLocalPositionAndRotation(pos, rot);
        }
        public void ApplyTo(TransformAccess access)
        {
            var pos = new Vector3(position.x, position.y, layer);
            var rot = Math2D.RotateZ(rotation);
            var sca = new Vector3(scale.x, scale.y, 1f);

            access.localScale = sca;
            access.SetLocalPositionAndRotation(pos, rot);
        }
    }
}
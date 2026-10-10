// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BH.SDK
{
    public struct alignment
    {
        public float2 value;
        
        /// <summary> (0.0f, 0.0f) Left Bottom (LEFT center right) (BOTTOM middle top) (LB - BL) </summary>
        public static float2 left_bottom   
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.0f, 0.0f);
        }
        /// <summary> (0.0f, 0.5f) Left Middle (LEFT center right) (bottom MIDDLE top) (LM - ML) </summary>
        public static float2 left_middle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.0f, 0.5f);
        }
        /// <summary> (0.0f, 1.0f) Left Top (LEFT center right) (bottom middle TOP) (LT - TL) </summary>
        public static float2 left_top
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.0f, 1.0f);
        }
        
        /// <summary> (0.5f, 0.0f) Center Bottom (left CENTER right) (BOTTOM middle top) (CB - BC) </summary>
        public static float2 center_bottom
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 0.0f);
        }
        /// <summary> (0.5f, 0.5f) Center Middle (left CENTER right) (bottom MIDDLE top) (CM - MC) </summary>
        public static float2 center_middle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 0.5f);
        }
        /// <summary> (0.5f, 1.0f) Center Top (left CENTER right) (bottom middle TOP) (CT - TC) </summary>
        public static float2 center_top
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 1.0f);
        }
        
        /// <summary> (1.0f, 0.0f) Right Bottom (left center RIGHT) (BOTTOM middle top) (RB - BR) </summary>
        public static float2 right_bottom
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(1.0f, 0.0f);
        }
        /// <summary> (1.0f, 0.5f) Right Middle (left center RIGHT) (bottom MIDDLE top) (RM - MR) </summary>
        public static float2 right_middle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(1.0f, 0.5f);
        }
        /// <summary> (1.0f, 1.0f) Right Top (left center RIGHT) (bottom middle TOP) (RT - TR) </summary>
        public static float2 right_top
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(1.0f, 1.0f);
        }
        
        // Pivots that rotate a shape about its centre of MASS; geometry is centred on its bounding box, so
        // the default (0.5, 0.5) is the box centre. Only forms whose two centres differ
        // (Docs/SHAPES.md).

        /// <summary> Centre of mass of the right triangle, as a pivot. </summary>
        public static float2 rightTriangle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.3333333f, 0.3333333f);
        }

        /// <summary> Centre of mass of the equilateral triangle, as a pivot. </summary>
        public static float2 equilateral3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 0.3556624f);
        }
        /// <summary> Centre of mass of the regular pentagon, as a pivot. </summary>
        public static float2 equilateral5
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 0.4497972f);
        }
        /// <summary> Centre of mass of the regular heptagon, as a pivot. </summary>
        public static float2 equilateral7
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 0.4746055f);
        }
        /// <summary> Centre of mass of the regular nonagon, as a pivot. </summary>
        public static float2 equilateral9
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 0.4846906f);
        }
        /// <summary> Centre of mass of the regular hendecagon, as a pivot. </summary>
        public static float2 equilateral11
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5f, 0.4897691f);
        }
    }
}
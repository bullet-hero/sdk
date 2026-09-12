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
        
        // THESE ARE THE PIVOT THAT ROTATES A SHAPE ABOUT ITS CENTRE OF MASS, and the direction
        // matters because it reversed. Every built-in shape's geometry is centred on its BOUNDING
        // BOX now, so the default pivot (0.5, 0.5) already puts the transform on the box centre -
        // which is what an editor wants to drag, size and align against, and what every gizmo and
        // the object picker already assume. What it is NOT is where a polygon balances: for an
        // odd-sided one the two are up to 0.145 apart, and rotating about the box centre makes a
        // pentagon wobble instead of spin.
        //
        // So the numbers below are what an author picks when they want the second behaviour. They
        // used to mean the opposite - the geometry was centred on the centroid and these moved it
        // onto the box - and the values barely changed, because it is one offset seen from either
        // end. They are exact now (ShapeCatalogService.GetCentroidPivot); the previous set was
        // hand-computed and drifted in the fifth decimal.
        //
        // ONLY FORMS WHOSE TWO CENTRES DIFFER APPEAR HERE. Every even-sided polygon and the circle
        // balance exactly on their box centre, so their answer is center_middle and giving them a
        // constant of their own would only invite the question of which to use.

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
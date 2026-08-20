// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules
{
    public static class EffectRules
    {
        public const bool HasStopLocalFrame_Default = false;

        // A frame counted from the emitter's own start, NOT a frame on the level timeline - an
        // effect placed near the end of a level may legitimately stop 300 of its own frames later.
        // It used to be validated as a level frame, which tied an effect's internal duration to
        // wherever it happened to be placed.
        public const int StopLocalFrame_Min = 0;
        public const int StopLocalFrame_Max = 100_000;
        public const int StopLocalFrame_Default = 10;

        // Speed window the "by speed" variants (EffectAngleCurvesBySpeed, EffectScaleCurvesBySpeed,
        // EffectColorGradientBySpeed) remap a particle's speed through. Speed is a magnitude, so it
        // never goes below zero; the upper end is well past what the force fields can produce.
        public const float SpeedRange_Min = 0f;
        public const float SpeedRange_Max = 1000f;

        // The two below are the odd ones out in this file: they bound a DEVICE setting
        // (EffectsGraphicsSettings), not authored level data, and they live here because that is the
        // one place a consumer already looks for what an effect number may be. Both count simulation
        // steps, i.e. GPU dispatches, and they bound different things - the first is per effect and
        // decides how one replay LOOKS, the second is per frame across the whole pool and decides
        // what the worst frame COSTS.
        //
        // A replay rebuilds a graph from an empty state, so each of its steps is one particle spawn
        // cohort: too few and a continuous stream comes back as that many visible packets. The floor
        // is therefore low enough to be a real emergency setting on a weak device and not so low
        // that an effect stops resembling itself; the ceiling is where a single effect would eat a
        // whole default frame budget on its own.

        public const int ReplayStepBudget_Min = 4;
        public const int ReplayStepBudget_Max = 128;
        public const int ReplayStepBudget_Default = 32;

        public const int FrameStepBudget_Min = 32;
        public const int FrameStepBudget_Max = 2048;
        public const int FrameStepBudget_Default = 256;

        public static class Core
        {
            public const bool Render_Default = true;
            public const bool Loop_Default = true;
            public const bool IsLocal_Default = true;
            
            public const uint ParticleCount_Min = 0;
            public const uint ParticleCount_Max = 32768;
            public const uint ParticleCount_Default = 10;
            
            // Particle lifetime range, in seconds. The upper bound is what keeps ParticleCount
            // meaningful: emitter cost is roughly count x lifetime, so an unbounded lifetime makes
            // a legal particle count unboundedly expensive.
            public const float LifetimeBounds_Min = 0f;
            public const float LifetimeBounds_Max = 60f;
            public const float LifetimeBounds_X_Default = 3f;
            public const float LifetimeBounds_Y_Default = 3f;
            
            // Alignment.CenterMiddleValue.Get();
            public const float Pivot_X_Default = 0.5f;
            public const float Pivot_Y_Default = 0.5f;
            
            public const float GravityConstraint_X_Default = 0f;
            public const float GravityConstraint_Y_Default = -9.81f;
            
            /// <summary> No image - the particle draws its shape's own colour. </summary>
            public static readonly TextureResourceId TextureResourceId_Default = TextureResourceId.Null;

            /// <summary> The quad. </summary>
            public static readonly ShapeId ParticleShapeId_Default = ShapeId.Square.Fill;

            // The whole texture, i.e. no atlas cell to select. Matches what TextureRegistry
            // .TryGetTextureUV hands back for an id it cannot resolve, so a missing texture and an
            // unset one produce the same rect rather than a zero one that samples a single texel.
            public const float TextureResourceUV_X_Default = 1f; // tilling x
            public const float TextureResourceUV_Y_Default = 1f; // tilling y
            public const float TextureResourceUV_Z_Default = 0f; // offset x
            public const float TextureResourceUV_W_Default = 0f; // offset y
        }
        public static class Forces
        {
            public const float StartGravityMin_Default = 0f;
            
            public const float StartGravityMax_Default = 0f;
            
            public const float StartVelocityMin_X_Default = 0f;
            public const float StartVelocityMin_Y_Default = 0f;
            
            public const float StartVelocityMax_X_Default = 0f;
            public const float StartVelocityMax_Y_Default = 0f;
            
            public const float StartAngularVelocityMin_Default = 0f;
            public const float StartAngularVelocityMax_Default = 0f;
            
            public const float OrbitalVelocity_X_Default = 0f;
            public const float OrbitalVelocity_Y_Default = 0f;
            public const float OrbitalVelocity_Z_Default = 0f;
            
            public const float LinearVelocity_X_Default = 0f;
            public const float LinearVelocity_Y_Default = 0f;
            
            public const float OrbitalCenterOffset_X_Default = 0f;
            public const float OrbitalCenterOffset_Y_Default = 0f;
            public const float OrbitalCenterOffset_Z_Default = 0f;
            
            public const float VelocitySpeed_Default = 1f;
            
            public const float LinearForce_X_Default = 0f;
            public const float LinearForce_Y_Default = 0f;
        }
        public static class Shape
        {
            public const byte Type_Default = 0;
            
            public const float CircleRadius_Min = 0f;
            public const float CircleRadius_Default = 1f;

            // The vertical semi-axis as a multiple of the horizontal one - a RATIO rather than a
            // second radius, so an unauthored value is a CIRCLE. The default has to be the neutral
            // one, or every file written before this existed - and every consumer-side asset
            // holding the same slot - reads back as an ellipse.

            public const float CircleAspect_Min = 0f;
            public const float CircleAspect_Max = 1000f;
            public const float CircleAspect_Default = 1f;

            public const float Arc_Min = 0f;
            public const float Arc_Max = BHSDKMath.PI2;
            public const float Arc_Default = Arc_Max;
            
            public const float CircleThickness_Min = 0f;
            public const float CircleThickness_Max = 1f;
            public const float CircleThickness_Default = CircleThickness_Max;
            
            public const float LineStart_X_Default = 0f;
            public const float LineStart_Y_Default = 0f;
            
            public const float LineEnd_X_Default = 1f;
            public const float LineEnd_Y_Default = 0f;
            
            public const float BoxSize_Min = 0f;
            public const float BoxSize_X_Default = 1f;
            public const float BoxSize_Y_Default = 1f;
            
            public const float ConeBaseRadius_Min = 0f;
            public const float ConeBaseRadius_Default = 1f;
            
            public const float ConeTopRadius_Min = 0f;
            public const float ConeTopRadius_Default = 0.4f;
            
            public const float ConeHeight_Min = 0f;
            public const float ConeHeight_Default = 1f;
            
            public const float TorusRadiusMinor_Min = 0f;
            public const float TorusRadiusMinor_Default = 0.4f;
            
            public const float TorusRadiusMajor_Min = 0f;
            public const float TorusRadiusMajor_Default = 1f;
        }
        public static class ShapeSpread
        {
            public const byte Type_Default = 0;
            
            public const float Spread_Default = 0f;
            
            public const float Speed_Default = 1f;
        }
        public static class Color
        {
            public const byte Type_Default = 0;
            
            public const float A_R_Default = 1f;
            public const float A_G_Default = 0f;
            public const float A_B_Default = 0f;
            public const float A_A_Default = 1f;
            
            public const float B_R_Default = 1f;
            public const float B_G_Default = 1f;
            public const float B_B_Default = 1f;
            public const float B_A_Default = 1f;
            
            public const float BySpeedRange_X_Default = 1.3f;
            public const float BySpeedRange_Y_Default = 2f;
        }
        public static class Scale
        {
            public const byte Type_Default = 0;
            
            public const float A_X_Default = 1f;
            public const float A_Y_Default = 1f;
            
            public const float B_X_Default = 1f;
            public const float B_Y_Default = 1f;
            
            public const float BySpeedRange_X_Default = 0f;
            public const float BySpeedRange_Y_Default = 1f;
        }
        public static class Angle
        {
            public const byte Type_Default = 0;
            
            public const float A_Default = 0f;
            
            public const float B_Default = 0f;
            
            public const float BySpeedRange_X_Default = 0f;
            public const float BySpeedRange_Y_Default = 1f;
        }
        
        public static CurveValue GetCurve_Default()
        {
            var key0 = new CurveKeyframeValue(0f, 0f);
            var key1 = new CurveKeyframeValue(1f, 1f);
            var keys = new List<CurveKeyframeValue> { key0, key1 };
            var curve = new CurveValue(keys, CurveWrapMode.Default, CurveWrapMode.Default);
            return curve;
        }
        public static GradientValue GetGradient_Default()
        {
            var colorKeys = new List<GradientColorKeyValue>
            {
                new(Color4Value.white, 0f),
                new(Color4Value.white, 1f),
            };
            var alphaKeys = new List<GradientAlphaKeyValue>
            {
                new(1f, 0f),
                new(1f, 1f),
            };
            
            return new GradientValue(colorKeys, alphaKeys,
                GradientInterpolationMode.PerceptualBlend, GradientColorSpace.Linear);
        }
    }
}
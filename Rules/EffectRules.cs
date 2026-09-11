// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules
{
    /// <summary> Every bound a VFX effect is authored within, plus the two device budgets that decide what an
    /// effect may cost - the one place a consumer already looks for what an effect number may be. </summary>
    public static class EffectRules
    {
        /// <summary> The default for has stop local frame, read by EffectData. </summary>
        public const bool HasStopLocalFrame_Default = false;

        // A frame counted from the emitter's own start, NOT a frame on the level timeline - an
        // effect placed near the end of a level may legitimately stop 300 of its own frames later.
        // It used to be validated as a level frame, which tied an effect's internal duration to
        // wherever it happened to be placed.

        /// <summary> Lower bound of EffectData.StopLocalFrame - it is a LOCAL FRAME, so it counts from
        /// the emitter's own first frame like every other frame in the format. </summary>
        public const int StopLocalFrame_Min = FrameRules.MinFrame;

        /// <summary> Upper bound of EffectData.StopLocalFrame. </summary>
        public const int StopLocalFrame_Max = 100_000;

        /// <summary> The stop local frame used when nothing says otherwise, read by EffectData. </summary>
        public const int StopLocalFrame_Default = 10;

        // Speed window the "by speed" variants (EffectAngleCurvesBySpeed, EffectScaleCurvesBySpeed,
        // EffectColorGradientBySpeed) remap a particle's speed through. Speed is a magnitude, so it
        // never goes below zero; the upper end is well past what the force fields can produce.

        /// <summary> Lower bound of EffectAngleCurvesBySpeed.SpeedRange, EffectColorGradientBySpeed.SpeedRange, EffectScaleCurvesBySpeed.SpeedRange. </summary>
        public const float SpeedRange_Min = 0f;

        /// <summary> Upper bound of EffectAngleCurvesBySpeed.SpeedRange, EffectColorGradientBySpeed.SpeedRange, EffectScaleCurvesBySpeed.SpeedRange. </summary>
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

        /// <summary> Lower bound of EffectsGraphicsSettings.ReplayStepBudget. </summary>
        public const int ReplayStepBudget_Min = 4;

        /// <summary> Upper bound of EffectsGraphicsSettings.ReplayStepBudget. </summary>
        public const int ReplayStepBudget_Max = 128;

        /// <summary> The replay step budget used when nothing says otherwise, read by EffectsGraphicsSettings, EffectsGraphicsSettingsTests. </summary>
        public const int ReplayStepBudget_Default = 32;

        /// <summary> Lower bound of EffectsGraphicsSettings.FrameStepBudget. </summary>
        public const int FrameStepBudget_Min = 32;

        /// <summary> Upper bound of EffectsGraphicsSettings.FrameStepBudget. </summary>
        public const int FrameStepBudget_Max = 2048;

        /// <summary> The frame step budget used when nothing says otherwise, read by EffectsGraphicsSettings, EffectsGraphicsSettingsTests. </summary>
        public const int FrameStepBudget_Default = 256;

        /// <summary> Bounds of an effect's own body: how it renders, loops and how long it lives. </summary>
        public static class Core
        {
            /// <summary> The default for render, read by EffectObjectCore. </summary>
            public const bool Render_Default = true;

            /// <summary> The default for loop, read by EffectObjectCore. </summary>
            public const bool Loop_Default = true;
            // Not authored, and there is no field for it in EffectObjectCore: a level's effect is
            // always local. This is the value the host starts its runtime state at, and the reason
            // it is a rule constant rather than a host-side literal is that it names the format's
            // guarantee - see EffectObjectCore's header.

            /// <summary> The default for is local. </summary>
            public const bool IsLocal_Default = true;

            // THE MAXIMUM IS THE GRAPH'S CAPACITY, and it is one number living in two files. Every
            // effect plays through UniversalVFX_Local/UniversalVFX_World, whose `capacity` is the
            // most particles a system can hold; a count above it is clamped by the graph with
            // nothing reported. Raising this without raising `capacity` in BOTH .vfx assets
            // therefore promises what the runtime silently refuses to deliver.
            //
            // It was 32 768 against a capacity of 32 768, and that pairing is where the cost came
            // from rather than the promise: a VFX graph's per-frame passes dispatch over CAPACITY,
            // not over the live particle count, so every instance was a 32 768-particle system
            // whatever it actually held. Authored content is nowhere near - the heaviest effect in
            // the corpus asks 500, medians are in the tens, and the default below is 10. Both
            // numbers moved to 1024 together; GamePlayer's CLAUDE.md carries the measurement.
            //
            // Lowering a bound cannot corrupt a level that exceeded it: RuleInRange repairs itself
            // by clamping, so ValidateAndFix brings an older level down to the new ceiling.

            //
            // THE TWO NUMBERS ARE NOT IN THE SAME UNIT, which is why `capacity` has to be checked
            // against this one rather than derived from it. ParticleCount is a spawn RATE (see
            // EffectObjectCore.ParticleCount); `capacity` is a population. They are equal here by
            // decision, not by arithmetic - the product a rate reaches over its lifetime exceeds
            // the cap long before the rate does, and nothing bounds that product at all.

            /// <summary> Lower bound of EffectObjectCore.ParticleCount, in particles per second. </summary>
            public const uint ParticleCount_Min = 0;

            /// <summary> Upper bound of EffectObjectCore.ParticleCount, in particles per second. </summary>
            public const uint ParticleCount_Max = 1024;

            /// <summary> The spawn rate used when nothing says otherwise, read by EffectObjectCore. </summary>
            public const uint ParticleCount_Default = 10;

            // Particle lifetime range, in seconds. The upper bound is what keeps ParticleCount
            // meaningful: emitter cost is roughly rate x lifetime, so an unbounded lifetime makes
            // a legal spawn rate unboundedly expensive. It is also what a one-shot's burst is
            // multiplied by, so it decides the size of that single batch outright.

            /// <summary> Lower bound of EffectObjectCore.LifetimeBounds. </summary>
            public const float LifetimeBounds_Min = 0f;

            /// <summary> Upper bound of EffectObjectCore.LifetimeBounds. </summary>
            public const float LifetimeBounds_Max = 60f;

            /// <summary> The lifetime bounds X used when nothing says otherwise, read by EffectObjectCore. </summary>
            public const float LifetimeBounds_X_Default = 3f;

            /// <summary> The lifetime bounds Y used when nothing says otherwise, read by EffectObjectCore. </summary>
            public const float LifetimeBounds_Y_Default = 3f;

            // Alignment.CenterMiddleValue.Get();

            /// <summary> The pivot X used when nothing says otherwise, read by EffectObjectCore. </summary>
            public const float Pivot_X_Default = 0.5f;

            /// <summary> The pivot Y used when nothing says otherwise, read by EffectObjectCore. </summary>
            public const float Pivot_Y_Default = 0.5f;

            /// <summary> The gravity constraint X used when nothing says otherwise. </summary>
            public const float GravityConstraint_X_Default = 0f;

            /// <summary> The gravity constraint Y used when nothing says otherwise. </summary>
            public const float GravityConstraint_Y_Default = -9.81f;

            /// <summary> No image - the particle draws its shape's own colour. </summary>
            public static readonly TextureResourceId TextureResourceId_Default = TextureResourceId.Null;

            /// <summary> The quad. </summary>
            public static readonly ShapeId ParticleShapeId_Default = ShapeId.Square.Fill;

            // The whole texture, i.e. no atlas cell to select. Matches what TextureRegistry
            // .TryGetTextureUV hands back for an id it cannot resolve, so a missing texture and an
            // unset one produce the same rect rather than a zero one that samples a single texel.

            /// <summary> The texture resource UV X used when nothing says otherwise. </summary>
            public const float TextureResourceUV_X_Default = 1f; // tilling x

            /// <summary> The texture resource UV Y used when nothing says otherwise. </summary>
            public const float TextureResourceUV_Y_Default = 1f; // tilling y

            /// <summary> The texture resource UV Z used when nothing says otherwise. </summary>
            public const float TextureResourceUV_Z_Default = 0f; // offset x

            /// <summary> The texture resource UV W used when nothing says otherwise. </summary>
            public const float TextureResourceUV_W_Default = 0f; // offset y
        }

        /// <summary> Bounds of the force fields a particle is pushed by. </summary>
        public static class Forces
        {
            /// <summary> The start gravity min used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartGravityMin_Default = 0f;

            /// <summary> The start gravity max used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartGravityMax_Default = 0f;

            /// <summary> The start velocity min X used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartVelocityMin_X_Default = 0f;

            /// <summary> The start velocity min Y used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartVelocityMin_Y_Default = 0f;

            /// <summary> The start velocity max X used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartVelocityMax_X_Default = 0f;

            /// <summary> The start velocity max Y used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartVelocityMax_Y_Default = 0f;

            /// <summary> The start angular velocity min used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartAngularVelocityMin_Default = 0f;

            /// <summary> The start angular velocity max used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float StartAngularVelocityMax_Default = 0f;

            /// <summary> The orbital velocity X used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float OrbitalVelocity_X_Default = 0f;

            /// <summary> The orbital velocity Y used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float OrbitalVelocity_Y_Default = 0f;

            /// <summary> The orbital velocity Z used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float OrbitalVelocity_Z_Default = 0f;

            /// <summary> The linear velocity X used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float LinearVelocity_X_Default = 0f;

            /// <summary> The linear velocity Y used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float LinearVelocity_Y_Default = 0f;

            /// <summary> The orbital center offset X used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float OrbitalCenterOffset_X_Default = 0f;

            /// <summary> The orbital center offset Y used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float OrbitalCenterOffset_Y_Default = 0f;

            /// <summary> The orbital center offset Z used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float OrbitalCenterOffset_Z_Default = 0f;

            /// <summary> The velocity speed used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float VelocitySpeed_Default = 1f;

            /// <summary> The linear force X used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float LinearForce_X_Default = 0f;

            /// <summary> The linear force Y used when nothing says otherwise, read by EffectObjectForces. </summary>
            public const float LinearForce_Y_Default = 0f;
        }

        /// <summary> Bounds of what an effect emits. </summary>
        public static class Shape
        {
            /// <summary> The type used when nothing says otherwise. </summary>
            public const byte Type_Default = 0;

            /// <summary> Lower bound of EffectShapeCircle.Radius. </summary>
            public const float CircleRadius_Min = 0f;

            /// <summary> The circle radius used when nothing says otherwise, read by ABObjectExporter, EffectShapeCircle. </summary>
            public const float CircleRadius_Default = 1f;

            // The vertical semi-axis as a multiple of the horizontal one - a RATIO rather than a
            // second radius, so an unauthored value is a CIRCLE. The default has to be the neutral
            // one, or every file written before this existed - and every consumer-side asset
            // holding the same slot - reads back as an ellipse.

            /// <summary> Lower bound of EffectShapeCircle.Aspect. </summary>
            public const float CircleAspect_Min = 0f;

            /// <summary> Upper bound of EffectShapeCircle.Aspect. </summary>
            public const float CircleAspect_Max = 1000f;

            /// <summary> The circle aspect used when nothing says otherwise, read by ABObjectExporter, ABObjectImporter, ABParticleImportTests and 1 more. </summary>
            public const float CircleAspect_Default = 1f;

            /// <summary> Lower bound of EffectShapeCircle.Arc, EffectShapeCone.Arc, EffectShapeTorus.Arc. </summary>
            public const float Arc_Min = 0f;

            /// <summary> Upper bound of EffectShapeCircle.Arc, EffectShapeCone.Arc, EffectShapeTorus.Arc. </summary>
            public const float Arc_Max = BHSDKMath.PI2;

            /// <summary> The arc used when nothing says otherwise, read by EffectShapeCircle, EffectShapeCone, EffectShapeTorus. </summary>
            public const float Arc_Default = Arc_Max;

            /// <summary> Lower bound of EffectShapeCircle.Thickness. </summary>
            public const float CircleThickness_Min = 0f;

            /// <summary> Upper bound of EffectShapeCircle.Thickness. </summary>
            public const float CircleThickness_Max = 1f;

            /// <summary> The circle thickness used when nothing says otherwise, read by EffectShapeCircle. </summary>
            public const float CircleThickness_Default = CircleThickness_Max;

            /// <summary> The line start X used when nothing says otherwise, read by EffectShapeLine. </summary>
            public const float LineStart_X_Default = 0f;

            /// <summary> The line start Y used when nothing says otherwise, read by EffectShapeLine. </summary>
            public const float LineStart_Y_Default = 0f;

            /// <summary> The line end X used when nothing says otherwise, read by EffectShapeLine. </summary>
            public const float LineEnd_X_Default = 1f;

            /// <summary> The line end Y used when nothing says otherwise, read by EffectShapeLine. </summary>
            public const float LineEnd_Y_Default = 0f;

            /// <summary> Lower bound of EffectShapeRectangle.Size. </summary>
            public const float BoxSize_Min = 0f;

            /// <summary> The box size X used when nothing says otherwise, read by EffectShapeRectangle. </summary>
            public const float BoxSize_X_Default = 1f;

            /// <summary> The box size Y used when nothing says otherwise, read by EffectShapeRectangle. </summary>
            public const float BoxSize_Y_Default = 1f;

            /// <summary> Lower bound of EffectShapeCone.BaseRadius. </summary>
            public const float ConeBaseRadius_Min = 0f;

            /// <summary> The cone base radius used when nothing says otherwise, read by EffectShapeCone. </summary>
            public const float ConeBaseRadius_Default = 1f;

            /// <summary> Lower bound of EffectShapeCone.TopRadius. </summary>
            public const float ConeTopRadius_Min = 0f;

            /// <summary> The cone top radius used when nothing says otherwise, read by EffectShapeCone. </summary>
            public const float ConeTopRadius_Default = 0.4f;

            /// <summary> Lower bound of EffectShapeCone.Height. </summary>
            public const float ConeHeight_Min = 0f;

            /// <summary> The cone height used when nothing says otherwise, read by EffectShapeCone. </summary>
            public const float ConeHeight_Default = 1f;

            /// <summary> Lower bound of EffectShapeTorus.MinorRadius. </summary>
            public const float TorusRadiusMinor_Min = 0f;

            /// <summary> The torus radius minor used when nothing says otherwise, read by EffectShapeTorus. </summary>
            public const float TorusRadiusMinor_Default = 0.4f;

            /// <summary> Lower bound of EffectShapeTorus.MajorRadius. </summary>
            public const float TorusRadiusMajor_Min = 0f;

            /// <summary> The torus radius major used when nothing says otherwise, read by EffectShapeTorus. </summary>
            public const float TorusRadiusMajor_Default = 1f;
        }

        /// <summary> Bounds of how those shapes are spread over the area they cover. </summary>
        public static class ShapeSpread
        {
            /// <summary> The type used when nothing says otherwise. </summary>
            public const byte Type_Default = 0;

            /// <summary> The spread used when nothing says otherwise, read by EffectShapeSpreadLoop, EffectShapeSpreadPingPong, EffectShapeSpreadRandom. </summary>
            public const float Spread_Default = 0f;

            /// <summary> The speed used when nothing says otherwise, read by EffectShapeSpreadLoop, EffectShapeSpreadPingPong. </summary>
            public const float Speed_Default = 1f;
        }

        /// <summary> Bounds of an effect's colour over a particle's life. </summary>
        public static class Color
        {
            /// <summary> The type used when nothing says otherwise. </summary>
            public const byte Type_Default = 0;

            /// <summary> The A R used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform, EffectColorValue. </summary>
            public const float A_R_Default = 1f;

            /// <summary> The A G used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform, EffectColorValue. </summary>
            public const float A_G_Default = 0f;

            /// <summary> The A B used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform, EffectColorValue. </summary>
            public const float A_B_Default = 0f;

            /// <summary> The A A used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform, EffectColorValue. </summary>
            public const float A_A_Default = 1f;

            /// <summary> The B R used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform. </summary>
            public const float B_R_Default = 1f;

            /// <summary> The B G used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform. </summary>
            public const float B_G_Default = 1f;

            /// <summary> The B B used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform. </summary>
            public const float B_B_Default = 1f;

            /// <summary> The B A used when nothing says otherwise, read by EffectColorRandomPerComponent, EffectColorRandomUniform. </summary>
            public const float B_A_Default = 1f;

            /// <summary> The by speed range X used when nothing says otherwise, read by EffectColorGradientBySpeed. </summary>
            public const float BySpeedRange_X_Default = 1.3f;

            /// <summary> The by speed range Y used when nothing says otherwise, read by EffectColorGradientBySpeed. </summary>
            public const float BySpeedRange_Y_Default = 2f;
        }

        /// <summary> Bounds of an effect's size over a particle's life. </summary>
        public static class Scale
        {
            /// <summary> The type used when nothing says otherwise. </summary>
            public const byte Type_Default = 0;

            /// <summary> The A X used when nothing says otherwise, read by EffectScaleRandomPerComponent, EffectScaleRandomUniform, EffectScaleValue. </summary>
            public const float A_X_Default = 1f;

            /// <summary> The A Y used when nothing says otherwise, read by EffectScaleRandomPerComponent, EffectScaleRandomUniform, EffectScaleValue. </summary>
            public const float A_Y_Default = 1f;

            /// <summary> The B X used when nothing says otherwise, read by EffectScaleRandomPerComponent, EffectScaleRandomUniform. </summary>
            public const float B_X_Default = 1f;

            /// <summary> The B Y used when nothing says otherwise, read by EffectScaleRandomPerComponent, EffectScaleRandomUniform. </summary>
            public const float B_Y_Default = 1f;

            /// <summary> The by speed range X used when nothing says otherwise, read by EffectScaleCurvesBySpeed. </summary>
            public const float BySpeedRange_X_Default = 0f;

            /// <summary> The by speed range Y used when nothing says otherwise, read by EffectScaleCurvesBySpeed. </summary>
            public const float BySpeedRange_Y_Default = 1f;
        }

        /// <summary> Bounds of an effect's rotation over a particle's life. </summary>
        public static class Angle
        {
            /// <summary> The type used when nothing says otherwise. </summary>
            public const byte Type_Default = 0;

            /// <summary> The A used when nothing says otherwise, read by EffectAngleRandomPerComponent, EffectAngleRandomUniform, EffectAngleValue. </summary>
            public const float A_Default = 0f;

            /// <summary> The B used when nothing says otherwise, read by EffectAngleRandomPerComponent, EffectAngleRandomUniform. </summary>
            public const float B_Default = 0f;

            /// <summary> The by speed range X used when nothing says otherwise, read by EffectAngleCurvesBySpeed. </summary>
            public const float BySpeedRange_X_Default = 0f;

            /// <summary> The by speed range Y used when nothing says otherwise, read by EffectAngleCurvesBySpeed. </summary>
            public const float BySpeedRange_Y_Default = 1f;
        }

        /// <summary> A fresh straight 0-to-1 ramp. Built rather than shared, since a curve is mutable and every
        /// effect authoring one must get its own. </summary>
        public static CurveValue GetCurve_Default()
        {
            var key0 = new CurveKeyframeValue(0f, 0f);
            var key1 = new CurveKeyframeValue(1f, 1f);
            var keys = new List<CurveKeyframeValue> { key0, key1 };
            var curve = new CurveValue(keys, CurveWrapMode.Default, CurveWrapMode.Default);
            return curve;
        }

        /// <summary> A fresh opaque white gradient, built for the same reason. </summary>
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
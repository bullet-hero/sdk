using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Services.Shapes;
using BH.SDK.Utils.Performance;

namespace BH.SDK.Generators.Performance
{
    /// <summary> What every stress level of one sweep shares, so that only the swept dimension moves.
    /// </summary>
    public struct StressBaseline
    {
        /// <summary> Slots every level of the sweep carries; at least the sweep's largest unit count.
        /// </summary>
        public int Pool;

        /// <summary> The level's frame rate. </summary>
        public int Fps;

        /// <summary> A baseline of <paramref name="pool"/> slots at 60 fps. </summary>
        public static StressBaseline Of(int pool) => new() { Pool = Math.Max(0, pool), Fps = DefaultFps };

        private const int DefaultFps = 60;
    }

    // Every slot is alive the whole level with one Size key; a unit only changes what a slot IS, so no other
    // dimension moves. Layout and the reasons: Docs/PERFORMANCE_RATING.md, "The stress levels".

    /// <summary> Synthetic levels for the device stress test: one per (dimension, unit count), every other
    /// dimension held at the baseline. Shared by the benchmark and the tests that prove it isolates.
    /// </summary>
    public static class StressLevelGenerator
    {
        /// <summary> Share of the screen one area unit covers. </summary>
        public const float UnitArea = 0.25f;

        /// <summary> World size of a slot that is not an area unit: drawn, yet covering no pixel. </summary>
        public const float TinySize = 0.001f;

        /// <summary> The screen aspect every stress level pins, so area is a known share of it. </summary>
        public const int AspectWidth = 16;

        /// <summary> Height part of <see cref="AspectWidth"/>. </summary>
        public const int AspectHeight = 9;

        /// <summary> Ancestors the deepest slot can have under the shared parent chain. </summary>
        public const int SpineLength = LevelRules.MaxObjectDepth - 1;

        /// <summary> The largest pool a level can hold beside the spine. </summary>
        public const int MaxPool = LevelRules.MaxObjects - SpineLength;

        /// <summary> The one effect resource every stress level carries. </summary>
        public static readonly EffectId StressEffectId = new(new Guid("6c0b5d0e-3f1e-4d3a-9a2e-5e7f40b1c001"));

        private static readonly ShapeId Square = ShapeId.Square.Fill;
        private const int KeyTracksPerSlot = 6;

        /// <summary> The most units of <paramref name="dimension"/> one level with this baseline can hold.
        /// </summary>
        public static int MaxUnits(DemandDimension dimension, StressBaseline baseline)
        {
            var frames = FrameCount(baseline);
            return dimension switch
            {
                DemandDimension.Instances => LevelRules.MaxObjects - baseline.Pool - SpineLength,
                DemandDimension.Keyframes => baseline.Pool * KeyTracksPerSlot * Math.Min(frames, LevelRules.MaxObjectKeys),
                DemandDimension.ParentLinks => baseline.Pool * SpineLength,
                DemandDimension.Glyphs => baseline.Pool * ValueRules.MaxGameString,
                DemandDimension.OpaqueBatches => Math.Min(baseline.Pool, CatalogShapes.Value.Count),
                DemandDimension.PostPasses => PostTrackCount,
                _ => baseline.Pool,
            };
        }

        /// <summary> The smallest power-of-two pool that holds <paramref name="units"/>, or the one holding the
        /// most when none does (the opaque batches stop at the catalog). Instances take no pool. </summary>
        public static StressBaseline PoolFor(DemandDimension dimension, int units)
        {
            if (dimension == DemandDimension.Instances) return StressBaseline.Of(0);

            var pool = 1;
            while (MaxUnits(dimension, StressBaseline.Of(pool)) < units && pool <= MaxPool / 2
                   && MaxUnits(dimension, StressBaseline.Of(pool * 2)) > MaxUnits(dimension, StressBaseline.Of(pool)))
                pool *= 2;
            return StressBaseline.Of(pool);
        }

        /// <summary> A level of <c>2 * WindowSeconds</c> holding <paramref name="units"/> units of
        /// <paramref name="dimension"/> over <paramref name="baseline"/>. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> More units than <see cref="MaxUnits"/>, or a pool
        /// above <see cref="MaxPool"/>. </exception>
        public static Level Create(DemandDimension dimension, int units, StressBaseline baseline)
        {
            if (baseline.Fps <= 0) baseline.Fps = StressBaseline.Of(0).Fps;
            if (baseline.Pool > MaxPool)
                throw new ArgumentOutOfRangeException(nameof(baseline), baseline.Pool,
                    $"A pool holds at most {MaxPool}.");
            if (units < 0 || units > MaxUnits(dimension, baseline))
                throw new ArgumentOutOfRangeException(nameof(units), units,
                    $"{dimension} holds at most {MaxUnits(dimension, baseline)} units over a pool of {baseline.Pool}.");

            var frames = FrameCount(baseline);
            var level = new Level();
            level.Settings.Fps = baseline.Fps;
            level.Settings.FrameDuration = frames;
            level.Game.Events.ScreenLimits.Add(new ScreenLimitKey(
                new ScreenLimitFixed(new ScreenAspect(AspectWidth, AspectHeight)), FrameRules.MinFrame));
            level.Resources.Effects[StressEffectId] = new EffectData { EffectId = StressEffectId };

            var span = new FrameSpan(FrameRules.MinFrame, frames);
            var nextId = 1;

            var spine = new ObjectId[SpineLength];
            for (var j = 0; j < SpineLength; j++)
            {
                var node = new RectObject { ObjectId = new ObjectId(nextId++), Span = span, Name = "spine" };
                if (j > 0) node.ParentObjectId = spine[j - 1];
                spine[j] = node.ObjectId;
                level.Game.Objects[node.ObjectId] = node;
            }

            var slots = new List<RectObject>(baseline.Pool);
            for (var i = 0; i < baseline.Pool; i++)
                slots.Add(Slot(dimension, i, units, baseline.Pool, frames, span, spine));

            foreach (var slot in slots)
            {
                slot.ObjectId = new ObjectId(nextId++);
                level.Game.Objects[slot.ObjectId] = slot;
            }

            if (dimension == DemandDimension.Instances)
            {
                for (var i = 0; i < units; i++)
                {
                    var extra = new RectObject { ObjectId = new ObjectId(nextId++), Span = span, Name = "extra" };
                    level.Game.Objects[extra.ObjectId] = extra;
                }
            }

            if (dimension == DemandDimension.PostPasses) AddPostPasses(level, units);

            level.Settings.ObjectIdCounter = nextId;
            return level;
        }

        /// <summary> The size a square needs to cover <paramref name="share"/> of the pinned screen at the
        /// default zoom. </summary>
        public static float SquareSideFor(float share)
        {
            var height = ValueRules.DefaultZoom;
            var width = height * AspectWidth / AspectHeight;
            return (float)Math.Sqrt(Math.Max(0f, share) * width * height);
        }

        private static int FrameCount(StressBaseline baseline)
        {
            var fps = baseline.Fps > 0 ? baseline.Fps : StressBaseline.Of(0).Fps;
            return Math.Max(FrameRules.MinFrameDuration,
                (int)Math.Round(2 * PerformanceRules.WindowSeconds * fps));
        }

        private static RectObject Slot(DemandDimension dimension, int index, int units, int pool, int frames,
            FrameSpan span, ObjectId[] spine)
        {
            var isUnit = index < units;
            RectObject slot;

            switch (dimension)
            {
                case DemandDimension.Colliders when isUnit:
                    slot = new ShapeObject { ShapeId = ShapeId.Null, ColliderId = Square };
                    break;
                case DemandDimension.Texts when isUnit:
                    slot = new TextObject { Text = new StringValue() };
                    break;
                case DemandDimension.Glyphs:
                    slot = new TextObject { Text = new StringValue(new string('W', Share(units, index, pool))) };
                    break;
                case DemandDimension.Effects when isUnit:
                    slot = new EffectObject { EffectId = StressEffectId };
                    break;
                case DemandDimension.TransparentShapes when isUnit:
                    slot = Shape(Square, ShaderType.Transparent);
                    break;
                case DemandDimension.OpaqueBatches when isUnit:
                    slot = Shape(CatalogShapes.Value[index], ShaderType.Opaque);
                    break;
                case DemandDimension.TransparentArea:
                    slot = Shape(Square, ShaderType.Transparent);
                    break;
                case DemandDimension.OpaqueArea:
                    slot = Shape(Square, ShaderType.Opaque);
                    break;
                default:
                    slot = new RectObject();
                    break;
            }

            slot.Span = span;
            slot.Name = "slot";

            var areaUnit = isUnit && DemandDimensions.IsArea(dimension);
            var side = areaUnit ? SquareSideFor(UnitArea) : TinySize;
            slot.Sizes.Add(new ScaKey(new Vector2Value(side, side), FrameRules.MinFrame));

            if (dimension == DemandDimension.Keyframes) AddKeys(slot, Share(units, index, pool), frames);
            if (dimension == DemandDimension.ParentLinks)
            {
                var depth = Math.Min(SpineLength, Share(units, index, pool));
                if (depth > 0) slot.ParentObjectId = spine[depth - 1];
            }

            return slot;
        }

        // Units spread evenly over the pool, so no slot exceeds a per-object cap the pool as a whole allows.
        private static int Share(int units, int index, int pool)
            => pool <= 0 ? 0 : units / pool + (index < units % pool ? 1 : 0);

        private static ShapeObject Shape(ShapeId shapeId, ShaderType shader)
            => new() { ShapeId = shapeId, ShaderType = shader };

        private static void AddKeys(RectObject slot, int count, int frames)
        {
            var perTrack = Math.Min(frames, LevelRules.MaxObjectKeys);
            for (var i = 0; i < count; i++)
            {
                var frame = FrameRules.MinFrame + i % perTrack;
                switch (i / perTrack)
                {
                    case 0: slot.Positions.Add(new PosKey(new Vector2Value(), frame)); break;
                    case 1: slot.Rotations.Add(new AngleKey(new FloatValue(), frame)); break;
                    case 2: slot.Scales.Add(new ScaKey(new Vector2Value(1f, 1f), frame)); break;
                    case 3: slot.AnchorsMin.Add(new AlignmentKey(new Vector2Value(), frame)); break;
                    case 4: slot.AnchorsMax.Add(new AlignmentKey(new Vector2Value(), frame)); break;
                    default: slot.Pivots.Add(new AlignmentKey(new Vector2Value(), frame)); break;
                }
            }
        }

        private const int PostTrackCount = 12;

        private static void AddPostPasses(Level level, int count)
        {
            var events = level.Game.PostProcessingEvents;
            events.Active = true;
            var on = FrameRules.MinFrame;

            for (var i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0: events.Blooms.Add(new BloomKey { Active = true, Frame = on }); break;
                    case 1: events.Chromatics.Add(new ChromaticAberrationKey { Active = true, Frame = on }); break;
                    case 2: events.Vignettes.Add(new VignetteKey { Active = true, Frame = on }); break;
                    case 3: events.Lenses.Add(new LensDistortionKey { Active = true, Frame = on }); break;
                    case 4: events.Grains.Add(new FilmGrainKey { Active = true, Frame = on }); break;
                    case 5: events.MotionBlurs.Add(new MotionBlurKey { Active = true, Frame = on }); break;
                    case 6: events.ColorCurves.Add(new ColorCurvesKey { Active = true, Frame = on }); break;
                    case 7: events.LiftGammaGains.Add(new LiftGammaGainKey { Active = true, Frame = on }); break;
                    case 8:
                        events.ShadowsMidtonesHighlights.Add(new ShadowsMidtonesHighlightsKey { Active = true, Frame = on });
                        break;
                    case 9: events.WhiteBalances.Add(new WhiteBalanceKey { Active = true, Frame = on }); break;
                    case 10: events.AnalogGlitches.Add(new AnalogGlitchKey { Active = true, Frame = on }); break;
                    default: events.DigitalGlitches.Add(new DigitalGlitchKey { Active = true, Frame = on }); break;
                }
            }
        }

        private static readonly Lazy<List<ShapeId>> CatalogShapes = new(() =>
            ShapeCatalogService.EnumerateCatalog()
                .Where(parameters => !ShapeCatalogService.IsDegenerate(parameters))
                .Select(ShapeCatalogService.ToShapeId)
                .ToList());
    }
}

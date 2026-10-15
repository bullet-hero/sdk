using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Game;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Utils.Performance
{
    // Authored spans, not resolved ones (FrameSpanFitMath lives in the client): the count can only be an
    // upper bound of what plays. Dimension meanings: Docs/PERFORMANCE_RATING.md.

    /// <summary> The count dimensions of a level's demand, exact per window, from the model alone. The area
    /// dimensions need transforms and the camera and are left zero for the client to fill. </summary>
    public static class LevelDemandCounter
    {
        /// <summary> One vector per <see cref="DemandWindows"/> window, <see cref="DemandDimensions.Count"/>
        /// long, indexed by <see cref="DemandDimension"/>. Placed prefabs must already be materialized.
        /// </summary>
        /// <param name="level"> Level to measure; null or empty measures no windows. </param>
        /// <param name="isOpaque"> Resolves <see cref="ShaderType.Auto"/> per shape; null counts Auto as
        /// transparent, as <see cref="LevelCapacityUtils.GetPeakUsage"/> does. </param>
        public static List<float[]> Count(Level level, Func<ShapeObject, bool> isOpaque = null)
        {
            var windows = new List<float[]>();
            var settings = level?.Settings;
            if (settings == null || settings.Fps <= 0) return windows;

            var fps = settings.Fps;
            var windowCount = DemandWindows.Count((float)settings.FrameDuration / fps);
            if (windowCount == 0) return windows;

            var perDimension = new float[DemandDimensions.Count][];
            for (var k = 0; k < perDimension.Length; k++) perDimension[k] = new float[windowCount];

            var levelEnd = FrameRules.EndBoundaryOf(settings.FrameDuration);
            if (level.Game?.Objects != null)
                CountObjects(level.Game, isOpaque, fps, levelEnd, perDimension);
            CountPostPasses(level.Game?.PostProcessingEvents, fps, levelEnd,
                perDimension[(int)DemandDimension.PostPasses]);

            for (var w = 0; w < windowCount; w++)
            {
                var vector = new float[DemandDimensions.Count];
                for (var k = 0; k < vector.Length; k++) vector[k] = perDimension[k][w];
                windows.Add(vector);
            }

            return windows;
        }

        /// <summary> How many glyphs a text shows: every non-whitespace code point, the longest language
        /// of a localized text. </summary>
        public static int CountGlyphs(IString text) => text switch
        {
            StringValue plain => CountGlyphs(plain.Value),
            StringLocalized localized => CountGlyphs(localized),
            _ => 0,
        };

        /// <summary> Non-whitespace code points of a plain string; a surrogate pair is one glyph. </summary>
        public static int CountGlyphs(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var count = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsLowSurrogate(c) || char.IsWhiteSpace(c) || char.IsControl(c)) continue;
                count++;
            }

            return count;
        }

        /// <summary> The frames on which player collision is on, as half-open intervals ending at
        /// <paramref name="levelEnd"/> at the latest. An empty track is on everywhere. </summary>
        public static List<(int Start, int End)> CollisionIntervals(PlayerEvents player, int levelEnd)
        {
            var keys = new List<(int Frame, bool On)>();
            if (player?.Collisions != null)
                foreach (var key in player.Collisions)
                    if (key != null) keys.Add((key.Frame, key.Value));

            return StepIntervals(keys, CollisionDefault, levelEnd);
        }

        // Lockstep: PlayerFrameData.DefaultCollision, what an empty track plays as.
        private const bool CollisionDefault = true;

        private static void CountObjects(GameLevel game, Func<ShapeObject, bool> isOpaque, int fps, int levelEnd,
            float[][] perDimension)
        {
            var collision = CollisionIntervals(game.PlayerEvents, levelEnd);
            var depths = new Dictionary<ObjectId, int>();
            var batches = new Dictionary<(TextureResourceId, ShapeId), List<FrameSpan>>();

            foreach (var pair in game.Objects)
            {
                var levelObject = pair.Value;
                if (levelObject == null) continue;

                var span = levelObject.Span;
                Add(perDimension, DemandDimension.Instances, span, fps, 1f);
                Add(perDimension, DemandDimension.Keyframes, span, fps, LevelStatsUtils.CountKeyframes(levelObject));
                Add(perDimension, DemandDimension.ParentLinks, span, fps,
                    AncestorCount(game, levelObject.ParentObjectId, depths));

                switch (levelObject)
                {
                    case ShapeObject shape:
                        CountShape(shape, isOpaque, fps, collision, batches, perDimension);
                        break;
                    case TextObject text:
                        Add(perDimension, DemandDimension.Texts, span, fps, 1f);
                        Add(perDimension, DemandDimension.Glyphs, span, fps, CountGlyphs(text.Text));
                        break;
                    case EffectObject _:
                        Add(perDimension, DemandDimension.Effects, span, fps, 1f);
                        break;
                }
            }

            // A batch is alive while ANY of its shapes is, so its spans are merged before integrating.
            var batchWindows = perDimension[(int)DemandDimension.OpaqueBatches];
            foreach (var spans in batches.Values)
                foreach (var (start, end) in Union(spans))
                    DemandWindows.Accumulate(FrameSpan.FromBounds(start, end), fps, 1f, batchWindows);
        }

        private static void CountShape(ShapeObject shape, Func<ShapeObject, bool> isOpaque, int fps,
            List<(int Start, int End)> collision, Dictionary<(TextureResourceId, ShapeId), List<FrameSpan>> batches,
            float[][] perDimension)
        {
            var span = shape.Span;

            if (shape.ColliderId.IsEnabled())
            {
                foreach (var (start, end) in collision)
                {
                    var from = Math.Max(start, span.StartFrame);
                    var to = Math.Min(end, span.EndFrame);
                    if (to > from) Add(perDimension, DemandDimension.Colliders, FrameSpan.FromBounds(from, to), fps, 1f);
                }
            }

            // ShapeId Null draws nothing: the slot is paid under Instances, no draw call is.
            if (!shape.ShapeId.IsEnabled()) return;

            if (!ResolveOpaque(shape, isOpaque))
            {
                Add(perDimension, DemandDimension.TransparentShapes, span, fps, 1f);
                return;
            }

            var key = (shape.TextureResourceId, shape.ShapeId);
            if (!batches.TryGetValue(key, out var spans)) batches[key] = spans = new List<FrameSpan>();
            spans.Add(span);
        }

        private static bool ResolveOpaque(ShapeObject shape, Func<ShapeObject, bool> isOpaque)
            => shape.ShaderType switch
            {
                ShaderType.Opaque => true,
                ShaderType.Transparent => false,
                _ => isOpaque != null && isOpaque(shape),
            };

        // Ancestors, not nodes: a root-level object walks no chain. A cycle or an over-deep chain counts at
        // the depth cap, the most a chain can cost.
        private static int AncestorCount(GameLevel game, ObjectId parentId, Dictionary<ObjectId, int> depths)
        {
            if (!parentId.IsNotNull()) return 0;
            if (depths.TryGetValue(parentId, out var known)) return known;

            var depth = ObjectDepthUtils.GetDepth(game, parentId);
            if (depth < 0) depth = LevelRules.MaxObjectDepth;
            depths[parentId] = depth;
            return depth;
        }

        private static void CountPostPasses(PostProcessingEvents events, int fps, int levelEnd, float[] perWindow)
        {
            if (events == null || !events.Active) return;

            AddTrack(events.Blooms);
            AddTrack(events.Chromatics);
            AddTrack(events.Vignettes);
            AddTrack(events.Lenses);
            AddTrack(events.Grains);
            AddTrack(events.MotionBlurs);
            AddTrack(events.ColorCurves);
            AddTrack(events.LiftGammaGains);
            AddTrack(events.ShadowsMidtonesHighlights);
            AddTrack(events.WhiteBalances);
            AddTrack(events.AnalogGlitches);
            AddTrack(events.DigitalGlitches);

            // Active follows the bounding START key, as BuildPostProcessingJob plays it; an empty track is
            // off.
            void AddTrack(IReadOnlyList<PostProcessingKeyframe> track)
            {
                if (track == null || track.Count == 0) return;

                var keys = new List<(int Frame, bool On)>(track.Count);
                foreach (var key in track)
                    if (key != null) keys.Add((key.Frame, key.Active));

                foreach (var (start, end) in StepIntervals(keys, false, levelEnd))
                    DemandWindows.Accumulate(FrameSpan.FromBounds(start, end), fps, 1f, perWindow);
            }
        }

        // A step track plays its lowest bounding key: the first key before it starts, the last one after.
        private static List<(int Start, int End)> StepIntervals(List<(int Frame, bool On)> keys, bool empty,
            int levelEnd)
        {
            var intervals = new List<(int Start, int End)>();
            if (levelEnd <= FrameRules.MinFrame) return intervals;

            if (keys.Count == 0)
            {
                if (empty) intervals.Add((FrameRules.MinFrame, levelEnd));
                return intervals;
            }

            keys.Sort((a, b) => a.Frame.CompareTo(b.Frame));

            for (var i = 0; i < keys.Count; i++)
            {
                if (!keys[i].On) continue;

                var start = i == 0 ? FrameRules.MinFrame : Math.Max(FrameRules.MinFrame, keys[i].Frame);
                var end = i + 1 < keys.Count ? Math.Min(levelEnd, keys[i + 1].Frame) : levelEnd;
                if (end <= start) continue;

                if (intervals.Count > 0 && intervals[intervals.Count - 1].End == start)
                    intervals[intervals.Count - 1] = (intervals[intervals.Count - 1].Start, end);
                else
                    intervals.Add((start, end));
            }

            return intervals;
        }

        private static List<(int Start, int End)> Union(List<FrameSpan> spans)
        {
            spans.Sort((a, b) => a.StartFrame.CompareTo(b.StartFrame));

            var union = new List<(int Start, int End)>();
            foreach (var span in spans)
            {
                if (union.Count > 0 && span.StartFrame <= union[union.Count - 1].End)
                {
                    var last = union[union.Count - 1];
                    union[union.Count - 1] = (last.Start, Math.Max(last.End, span.EndFrame));
                    continue;
                }

                union.Add((span.StartFrame, span.EndFrame));
            }

            return union;
        }

        private static void Add(float[][] perDimension, DemandDimension dimension, FrameSpan span, int fps,
            float weight)
            => DemandWindows.Accumulate(span, fps, weight, perDimension[(int)dimension]);
    }
}

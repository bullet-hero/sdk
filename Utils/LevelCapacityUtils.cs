using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Hints;

namespace BH.SDK.Utils
{
    /// <summary>
    /// Measures how many objects a level needs alive at the same time, at its heaviest frame. A
    /// player/editor uses this to size its per-frame buffers before playback starts, and the editor
    /// stores the result into the level itself as an advisory hint (see LevelHints.Limits).
    /// <br/><br/>
    /// Every family is measured independently: an object occupies an instance slot always, and a
    /// texture/effect/text slot only if it is of that type. That is deliberate - one level can hold
    /// thousands of plain transform anchors next to a handful of effects, and sizing the (far more
    /// expensive) effect buffers off the object count would be wasteful.
    /// </summary>
    public static class LevelCapacityUtils
    {
        // isOpaque exists because ShaderType.Auto is not decidable here. Resolving it needs the
        // alpha of the object's TEXTURE, and this library has no idea what a texture is - only the
        // consumer that loaded the atlas does. So a consumer that cares passes its own resolver;
        // everyone else gets Auto counted as transparent, which is the same fallback the consumer
        // applies and the harmless direction to be wrong in (an object sized into the wrong pool
        // still draws, it just misses out on depth rejection).

        /// <summary>
        /// Peak simultaneous usage across the whole level. O(n log n) - a sweep over each family's
        /// half-open FrameSpan, so two objects meeting end to start are never counted as
        /// simultaneous. Placed prefabs need no special handling: their contents are materialized
        /// into the same Objects dictionary as everything else.
        /// </summary>
        /// <param name="level"> Level to measure. </param>
        /// <param name="isOpaque"> Resolves <see cref="ShaderType.Auto"/> for one shape. Null means
        /// every Auto object counts as transparent. </param>
        public static LimitHints GetPeakUsage(Level level, Func<ShapeObject, bool> isOpaque = null)
        {
            if (level?.Game?.Objects == null) return new LimitHints();

            var instances = new IntervalSweep();
            var shapesOpaque = new IntervalSweep();
            var shapesTransparent = new IntervalSweep();
            var effects = new IntervalSweep();
            var texts = new IntervalSweep();
            var tracks = new IntervalSweep();

            foreach (var pair in level.Game.Objects)
            {
                var levelObject = pair.Value;
                if (levelObject == null) continue;

                instances.Add(levelObject);
                switch (levelObject.GetModelType())
                {
                    case ObjectType.ShapeObject:
                    {
                        if (levelObject is ShapeObject shapeObject && ResolveOpaque(shapeObject, isOpaque))
                            shapesOpaque.Add(levelObject);
                        else
                            shapesTransparent.Add(levelObject);
                        break;
                    }
                    case ObjectType.EffectObject: effects.Add(levelObject); break;
                    case ObjectType.TextObject: texts.Add(levelObject); break;
                }
            }

            if (level.Audio?.Tracks != null)
            {
                foreach (var pair in level.Audio.Tracks)
                {
                    if (pair.Value == null) continue;
                    tracks.Add(pair.Value);
                }
            }

            return new LimitHints(instances.GetPeak(),
                shapesOpaque.GetPeak(), shapesTransparent.GetPeak(),
                effects.GetPeak(), texts.GetPeak(), tracks.GetPeak());
        }

        private static bool ResolveOpaque(ShapeObject shapeObject, Func<ShapeObject, bool> isOpaque)
            => shapeObject.ShaderType switch
            {
                ShaderType.Opaque => true,
                ShaderType.Transparent => false,
                _ => isOpaque != null && isOpaque(shapeObject),
            };

        /// <summary>
        /// Peak simultaneous usage of a single family, given raw spans. Exposed for callers
        /// measuring something the level model doesn't describe.
        /// </summary>
        public static int GetPeak(IReadOnlyList<FrameSpan> spans)
        {
            var sweep = new IntervalSweep();
            for (var i = 0; i < spans.Count; i++)
                sweep.Add(spans[i]);
            return sweep.GetPeak();
        }

        /// <summary>
        /// Classic sweep line. A FrameSpan's end is already the first frame the object is gone, so
        /// the ordering between an end and a start landing on the same frame needs no correction:
        /// an object ending there and one starting there are never alive at the same time.
        /// </summary>
        private struct IntervalSweep
        {
            private List<int> _starts;
            private List<int> _ends;

            /// <summary> Counts one lifetime. </summary>
            public void Add(IFrameBounds bounds) => Add(bounds.Span);

            /// <summary> Counts one lifetime, as a bare span. </summary>
            public void Add(in FrameSpan span)
            {
                _starts ??= new List<int>();
                _ends ??= new List<int>();

                _starts.Add(span.StartFrame);
                _ends.Add(span.EndFrame);
            }

            /// <summary> The largest number of those lifetimes overlapping on any one frame. </summary>
            public int GetPeak()
            {
                if (_starts == null || _starts.Count == 0) return 0;

                _starts.Sort();
                _ends.Sort();

                int peak = 0, current = 0, endIndex = 0;
                foreach (var start in _starts)
                {
                    while (endIndex < _ends.Count && _ends[endIndex] <= start)
                    {
                        current--;
                        endIndex++;
                    }
                    current++;
                    if (current > peak) peak = current;
                }
                return peak;
            }
        }
    }
}

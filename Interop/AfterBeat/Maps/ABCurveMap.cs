using System;
using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Interop.AfterBeat
{
    // TWO CURVE MODELS THAT DISAGREE ABOUT WHERE SHAPE COMES FROM. An Afterbeat keyframe carries an
    // EASING NAME, and the source game bakes it by sampling the eased segment at 16 points before
    // handing the result to Unity. A CurveKeyframeValue carries TANGENTS and no easing at all - so
    // an ease cannot be copied across, it can only be sampled, exactly as the source game samples
    // it. That is what this file does.
    //
    // The one thing the source game has and this format does not is room: it samples every segment
    // at 16 points, while ValueRules.MaxCurveKeys bounds the WHOLE curve at 16. So the budget is
    // shared out instead - every authored keyframe is kept, since those are the points the author
    // actually placed, and whatever is left over buys interior samples for the segments whose
    // easing needs them. A Linear segment needs none, an Instant one is a step and needs none
    // either, so an ordinary emitter spends nothing and the budget goes where the curve bends.
    //
    // Read together with ABEaseMap.Evaluate, which is the sampling half.

    /// <summary> Afterbeat's eased keyframe tracks, baked into this format's tangent-based
    /// curves. </summary>
    public static class ABCurveMap
    {
        /// <summary> Interior samples one eased segment may ever get, whatever the budget allows -
        /// the source game's own count, and past it the extra points buy nothing visible. </summary>
        public const int MaxEaseSamples = 16;

        /// <summary> How close to its own end an Instant step stands, as a fraction of the segment
        /// it ends - the source game's number, and what makes the step read as vertical without two
        /// keys landing on one time. </summary>
        public const float InstantStepFraction = 0.001f;

        /// <summary> Two curve times closer than this are one time; the sorted-and-unique rule a
        /// CurveValue is validated against has to hold after every nudge here. </summary>
        public const float MinTimeStep = 1e-5f;

        /// <summary>
        /// One value channel of one source track, over the particle's own life. An absent or empty
        /// track is a flat curve at <paramref name="fallback"/> rather than an empty one - a curve
        /// cannot hold fewer than two keys, and "this channel was never authored" has to read as
        /// "it never changes".
        /// </summary>
        public static CurveValue Import(VgdTrack track, int valueIndex, float fallback,
            float timelineLength, Func<float, float> transform = null,
            InteropReport report = null, string path = null)
        {
            transform ??= value => value;
            var keyframes = Collect(track);

            if (keyframes.Count == 0) return Flat(transform(fallback));
            if (timelineLength <= 0f) timelineLength = ABParticleMap.MinTimelineLength;

            var nodes = BuildNodes(keyframes, valueIndex, fallback, timelineLength, transform,
                report, path);

            return ToCurve(nodes);
        }

        // A GRADIENT IS NOT A CURVE, and the difference is what it cannot carry. Its stops hold no
        // easing of their own - interpolation is one setting for the whole ramp - so there is
        // nothing to sample here and the authored stops cross as they are. What it also cannot
        // carry is a THEME REFERENCE: GradientColorKeyValue is a literal Color4Value by design, so
        // the colour is resolved once against the import's reference theme and stops following a
        // theme change from then on.
        //
        // RGB and alpha are two independent lists here and one value over there, so one source
        // keyframe becomes one stop in each.

        /// <summary> One source colour track as a particle ramp over its own life. </summary>
        public static GradientValue ImportGradient(VgdTrack track, float timelineLength,
            ThemeData referenceTheme, InteropReport report = null, string path = null)
        {
            var keyframes = Collect(track);
            if (keyframes.Count == 0) return EffectRules.GetGradient_Default();
            if (timelineLength <= 0f) timelineLength = ABParticleMap.MinTimelineLength;

            if (keyframes.Count > ValueRules.MaxGradientKeys)
                report?.Approximated("particle_color_stops_capped",
                    $"Some particle colour tracks carry more keyframes than a gradient here can hold; those were reduced to {ValueRules.MaxGradientKeys} stops.",
                    path);

            var colors = new List<GradientColorKeyValue>();
            var alphas = new List<GradientAlphaKeyValue>();
            var last = float.NegativeInfinity;

            foreach (var keyframe in Cap(keyframes))
            {
                var time = Normalize(keyframe.Time, timelineLength);
                if (time - last < MinTimeStep) continue;
                last = time;

                var themeIndex = ABColorMap.ToThemeIndex(
                    ABColorMap.ToIndex(ReadValue(keyframe, ColorSlotIndex, 0f)), ABPalette.Objects);
                var slot = ABColorMap.ResolveSlot(referenceTheme, new Color4ThemeRef(themeIndex));
                var alpha = Math.Clamp(ReadValue(keyframe, ColorOpacityIndex, ColorOpacityScale)
                                       / ColorOpacityScale,
                    ValueRules.MinColor, ValueRules.MaxColor);

                colors.Add(new GradientColorKeyValue(
                    new Color4Value(slot.R, slot.G, slot.B, ValueRules.MaxColor), time));
                alphas.Add(new GradientAlphaKeyValue(alpha, time));
            }

            // TWO, not ValueRules.MinGradientKeys, and the difference is the point: one stop is a
            // legal gradient in this format, so the bound no longer says anything about what an
            // IMPORT should produce. Afterbeat's own single colour keyframe means one colour for
            // the whole life, and holding it at both ends is what says so in a ramp the author can
            // then edit at either end. Collapsing it to one stop would look the same and read as
            // an import that lost a keyframe.
            if (colors.Count == 0) return EffectRules.GetGradient_Default();
            if (colors.Count < HeldRampKeys)
            {
                colors.Add(new GradientColorKeyValue(colors[0].Color4, ValueRules.MaxGradientTime));
                alphas.Add(new GradientAlphaKeyValue(alphas[0].Alpha, ValueRules.MaxGradientTime));

                if (colors[0].Time >= ValueRules.MaxGradientTime)
                {
                    colors[0] = new GradientColorKeyValue(colors[0].Color4, ValueRules.MinGradientTime);
                    alphas[0] = new GradientAlphaKeyValue(alphas[0].Alpha, ValueRules.MinGradientTime);
                }
            }

            return new GradientValue(colors, alphas,
                GradientInterpolationMode.PerceptualBlend, GradientColorSpace.Linear);
        }

        /// <summary> Stops a single authored colour is held across - see the import above. </summary>
        private const int HeldRampKeys = 2;

        /// <summary> Where a colour keyframe keeps its theme slot. </summary>
        public const int ColorSlotIndex = 0;

        /// <summary> Where a colour keyframe keeps its opacity, on a 0-100 scale. </summary>
        public const int ColorOpacityIndex = 1;

        /// <summary> What that 0-100 opacity divides by to become this format's 0-1 alpha. </summary>
        public const float ColorOpacityScale = 100f;

        // Evenly, not the first eight: the tail of a colour track is what a particle dies as, and
        // dropping it would leave every particle frozen at the colour it was halfway through.
        private static List<VgdKeyframe> Cap(List<VgdKeyframe> keyframes)
        {
            if (keyframes.Count <= ValueRules.MaxGradientKeys) return keyframes;

            var result = new List<VgdKeyframe>(ValueRules.MaxGradientKeys) { keyframes[0] };
            var interior = ValueRules.MaxGradientKeys - 2;

            for (var i = 1; i <= interior; i++)
            {
                var index = (int)Math.Round(i * (keyframes.Count - 1) / (double)(interior + 1));
                index = Math.Clamp(index, 1, keyframes.Count - 2);
                result.Add(keyframes[index]);
            }

            result.Add(keyframes[^1]);
            return result;
        }

        /// <summary> A curve that never changes, which is what an unauthored channel means. </summary>
        public static CurveValue Flat(float value)
            => new(new List<CurveKeyframeValue>
            {
                new(ValueRules.MinCurveTime, value),
                new(ValueRules.MaxCurveTime, value),
            }, CurveWrapMode.Default, CurveWrapMode.Default);

        /// <summary> The track's keyframes in time order, nulls dropped. </summary>
        private static List<VgdKeyframe> Collect(VgdTrack track)
        {
            var result = new List<VgdKeyframe>();
            if (track?.Keyframes == null) return result;

            foreach (var keyframe in track.Keyframes)
                if (keyframe != null)
                    result.Add(keyframe);

            result.Sort((left, right) => left.Time.CompareTo(right.Time));
            return result;
        }

        private static List<(float Time, float Value)> BuildNodes(List<VgdKeyframe> keyframes,
            int valueIndex, float fallback, float timelineLength, Func<float, float> transform,
            InteropReport report, string path)
        {
            var nodes = new List<(float Time, float Value)>();

            var firstTime = Normalize(keyframes[0].Time, timelineLength);
            var firstValue = transform(ReadValue(keyframes[0], valueIndex, fallback));

            // A track that starts late holds the default until it does, exactly as the source game
            // prepends it.
            if (firstTime > 0f) nodes.Add((ValueRules.MinCurveTime, transform(fallback)));
            nodes.Add((firstTime, firstValue));

            var budget = ResolveSampleBudget(keyframes, timelineLength, report, path);

            for (var i = 1; i < keyframes.Count; i++)
            {
                var startTime = Normalize(keyframes[i - 1].Time, timelineLength);
                var endTime = Normalize(keyframes[i].Time, timelineLength);
                var startValue = transform(ReadValue(keyframes[i - 1], valueIndex, fallback));
                var endValue = transform(ReadValue(keyframes[i], valueIndex, fallback));

                AppendSegment(nodes, keyframes[i], startTime, endTime, startValue, endValue,
                    budget, report, path);
            }

            // And one that ends early holds its last value to the end.
            if (nodes[^1].Time < ValueRules.MaxCurveTime)
                nodes.Add((ValueRules.MaxCurveTime, nodes[^1].Value));

            return nodes;
        }

        private static void AppendSegment(List<(float Time, float Value)> nodes, VgdKeyframe end,
            float startTime, float endTime, float startValue, float endValue, int budget,
            InteropReport report, string path)
        {
            // Degenerate - the two keyframes land on one time, so only the later value survives.
            if (endTime - startTime <= MinTimeStep)
            {
                nodes.Add((endTime, endValue));
                return;
            }

            if (IsInstant(end.Ease))
            {
                var step = (endTime - startTime) * InstantStepFraction;
                nodes.Add((endTime - Math.Max(step, MinTimeStep), startValue));
                nodes.Add((endTime, endValue));
                return;
            }

            var ease = ABEaseMap.Import(end.Ease, report, path);
            if (ease == EaseType.Linear || budget <= 0)
            {
                nodes.Add((endTime, endValue));
                return;
            }

            for (var i = 1; i <= budget; i++)
            {
                var t = i / (float)(budget + 1);
                nodes.Add((
                    startTime + (endTime - startTime) * t,
                    Lerp(startValue, endValue, ABEaseMap.Evaluate(ease, t))));
            }

            nodes.Add((endTime, endValue));
        }

        // Every authored keyframe is mandatory, so what is left of MaxCurveKeys is what the eased
        // segments share. An Instant segment costs one extra key of its own, which is why it is
        // counted here rather than assumed free.

        /// <summary> Interior samples each eased segment gets. Zero means the budget is spent and
        /// those segments degrade to straight lines. </summary>
        private static int ResolveSampleBudget(List<VgdKeyframe> keyframes, float timelineLength,
            InteropReport report, string path)
        {
            var mandatory = keyframes.Count + 2; // room for a prepended default and a held tail
            var eased = 0;

            for (var i = 1; i < keyframes.Count; i++)
            {
                var startTime = Normalize(keyframes[i - 1].Time, timelineLength);
                var endTime = Normalize(keyframes[i].Time, timelineLength);
                if (endTime - startTime <= MinTimeStep) continue;

                if (IsInstant(keyframes[i].Ease))
                {
                    mandatory++;
                    continue;
                }

                if (ABEaseMap.Import(keyframes[i].Ease, report, path) != EaseType.Linear) eased++;
            }

            if (eased == 0) return 0;

            var remaining = ValueRules.MaxCurveKeys - mandatory;
            if (remaining <= 0)
            {
                report?.Approximated("particle_curve_flattened",
                    "Some particle curves carry more keyframes than a curve here can hold; those segments cross as straight lines rather than as the easing they were authored with.",
                    path);
                return 0;
            }

            return Math.Min(MaxEaseSamples, remaining / eased);
        }

        /// <summary> Nodes into a real curve: strictly increasing in time, never fewer than two
        /// keys, never more than the format allows, tangents from the neighbours exactly as the
        /// source game computes them. </summary>
        private static CurveValue ToCurve(List<(float Time, float Value)> nodes)
        {
            var unique = Deduplicate(nodes);

            if (unique.Count < ValueRules.MinCurveKeys) return Flat(unique.Count == 0 ? 0f : unique[0].Value);
            if (unique.Count > ValueRules.MaxCurveKeys) unique = Decimate(unique);

            var keys = new List<CurveKeyframeValue>(unique.Count);

            for (var i = 0; i < unique.Count; i++)
            {
                var incoming = i > 0 ? Slope(unique[i - 1], unique[i]) : 0f;
                var outgoing = i < unique.Count - 1 ? Slope(unique[i], unique[i + 1]) : 0f;

                if (i == 0) incoming = outgoing;
                if (i == unique.Count - 1) outgoing = incoming;

                // Broken exactly when the two sides genuinely differ. These slopes come from the
                // two NEIGHBOURING segments, so an imported point sitting where the line changes
                // direction is a corner and must stay one - dragging its handle in the curve editor
                // would otherwise straighten it on the first touch.
                keys.Add(new CurveKeyframeValue(unique[i].Time, unique[i].Value,
                    CurveWeightedMode.None, CurveTangentMode.Free, incoming, outgoing, 0f, 0f,
                    incoming != outgoing));
            }

            return new CurveValue(keys, CurveWrapMode.Default, CurveWrapMode.Default);
        }

        private static List<(float Time, float Value)> Deduplicate(List<(float Time, float Value)> nodes)
        {
            nodes.Sort((left, right) => left.Time.CompareTo(right.Time));

            var result = new List<(float Time, float Value)>(nodes.Count);

            foreach (var node in nodes)
            {
                var time = Math.Clamp(node.Time, ValueRules.MinCurveTime, ValueRules.MaxCurveTime);

                // The later value wins on a shared time, which is what a step and a degenerate
                // segment both mean.
                if (result.Count > 0 && time - result[^1].Time < MinTimeStep)
                {
                    result[^1] = (result[^1].Time, node.Value);
                    continue;
                }

                result.Add((time, node.Value));
            }

            return result;
        }

        // Endpoints are kept whatever else goes: they are where the curve starts and where it ends,
        // and losing either changes what the particle looks like at birth or at death.
        private static List<(float Time, float Value)> Decimate(List<(float Time, float Value)> nodes)
        {
            var result = new List<(float Time, float Value)>(ValueRules.MaxCurveKeys) { nodes[0] };
            var interior = ValueRules.MaxCurveKeys - 2;

            for (var i = 1; i <= interior; i++)
            {
                var index = (int)Math.Round(i * (nodes.Count - 1) / (double)(interior + 1));
                index = Math.Clamp(index, 1, nodes.Count - 2);

                if (nodes[index].Time - result[^1].Time >= MinTimeStep) result.Add(nodes[index]);
            }

            result.Add(nodes[^1]);
            return result;
        }

        private static float Slope((float Time, float Value) from, (float Time, float Value) to)
        {
            var run = to.Time - from.Time;
            return run <= 0f ? 0f : (to.Value - from.Value) / run;
        }

        private static float Normalize(float time, float timelineLength)
            => Math.Clamp(time / timelineLength, ValueRules.MinCurveTime, ValueRules.MaxCurveTime);

        private static bool IsInstant(string ease)
            => string.Equals(ease, ABEaseMap.InstantEaseName, StringComparison.OrdinalIgnoreCase);

        private static float ReadValue(VgdKeyframe keyframe, int index, float fallback)
        {
            var values = keyframe?.Values;
            return values != null && index >= 0 && index < values.Count ? values[index] : fallback;
        }

        private static float Lerp(float from, float to, float t) => from + (to - from) * t;
    }
}

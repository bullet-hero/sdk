using System;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;

namespace BH.SDK.Interop.AfterBeat
{
    // Afterbeat randomizes a keyframe by carrying a type plus three spare numbers ("er": Random X,
    // Random Y, Interval) beside the value; this format randomizes it by storing a different KIND
    // of value. So the conversion is not a field copy, it is a choice of variant.
    //
    // THE ONE THING TO KNOW: "er" is the OTHER END OF THE RANGE, never an offset from the value.
    // The source game reads every one of these as Random.Range(GetVal(i), GetRandVal(i))
    // (ObjectHelpers.RandomVector2Parser / RandomFloatParser), so a keyframe at 5 with an er of 20
    // rolls between 5 and 20 - not between 5 and 25. Reading it as an offset widens every random
    // range in a converted level by the value it was anchored at, which is invisible on a keyframe
    // authored at zero and wrong everywhere else.
    //
    //   None          -> the literal value.
    //   Linear        -> the range [value, er]. Exact.
    //   LinearRounded -> the same range, snapped to whole numbers - a step of 1. Exact.
    //   Toggle        -> either end and nothing between, which is a range whose step IS its own
    //                    width. Exact for a float. For a VECTOR it is one flip deciding both
    //                    components together, and this format rolls each axis on its own address,
    //                    so the pairing is the part that cannot cross and is reported.
    //   Scale         -> the value MULTIPLIED by a factor from er[0]..er[1]. Exact, because
    //                    multiplying a fixed value by a range is a range - it only has to be
    //                    ordered, since a negative value swaps the ends.
    //
    // An Interval (er[2]) of its own turns any of the above into the step variant, which is the
    // same meaning in both formats: land on multiples rather than anywhere in the range.
    //
    // Rotation is the other thing this file owns, and it is the single most dangerous conversion in
    // the whole importer. Afterbeat writes rotation in DEGREES and each keyframe is RELATIVE to the
    // one before it; this format writes RADIANS and each keyframe is absolute. Getting either half
    // wrong produces a level that loads, plays, and spins.

    /// <summary> Afterbeat's keyframe values and randomization, mapped onto this format's
    /// polymorphic value types. </summary>
    public static class ABValueMap
    {
        /// <summary> Degrees to radians. The SDK cannot reach UnityEngine.Mathf. </summary>
        public const float DegreesToRadians = (float)(Math.PI / 180.0);

        /// <summary> Radians to degrees. </summary>
        public const float RadiansToDegrees = (float)(180.0 / Math.PI);

        /// <summary> Below this an interval is no interval at all - a step of zero would make every
        /// value in the range land on the range's own start. </summary>
        public const float MinInterval = 1e-4f;

        #region Floats

        /// <summary> One authored number, with whatever randomization the keyframe carried. </summary>
        public static IFloat ImportFloat(float value, VgdKeyframe source,
            InteropReport report = null, string path = null)
        {
            if (source == null) return new FloatValue(value);

            var other = source.GetRandom(0);
            var interval = source.GetRandom(2);

            switch ((ABRandomType)source.RandomType)
            {
                case ABRandomType.None:
                    return new FloatValue(value);

                case ABRandomType.Linear:
                    return MakeRange(value, other, interval);

                case ABRandomType.LinearRounded:
                    // Mathf.Round over the same range - which is a step of exactly one, and the
                    // source's own interval is not consulted on this branch at all.
                    return MakeRange(value, other, WholeNumberInterval);

                case ABRandomType.Toggle:
                    // A step equal to the whole width leaves exactly the two ends reachable.
                    return MakeToggle(value, other);

                case ABRandomType.Scale:
                    // The interval snaps the FACTOR over there, so it scales with the value the
                    // same way both ends of the range do.
                    return MakeRange(value * source.GetRandom(0), value * source.GetRandom(1),
                        Math.Abs(value * interval));

                default:
                    report?.Dropped("random_unknown",
                        $"Randomization type {source.RandomType} is not one this converter knows; those keyframes use their authored value.",
                        path);
                    return new FloatValue(value);
            }
        }

        /// <summary> The step that turns a range into "whole numbers only", which is what the
        /// source game's own Mathf.Round over a range amounts to. </summary>
        public const float WholeNumberInterval = 1f;

        private static IFloat MakeToggle(float a, float b)
        {
            var width = Math.Abs(b - a);
            if (width < MinInterval) return new FloatValue(a);
            return new FloatMinMaxStep(Min(a, b), Max(a, b), width);
        }

        private static IFloat MakeRange(float a, float b, float interval)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            if (Math.Abs(max - min) < MinInterval) return new FloatValue(a);
            return interval > MinInterval
                ? new FloatMinMaxStep(min, max, interval)
                : new FloatMinMax(min, max);
        }

        #endregion

        #region Vectors

        /// <summary> One authored point, with whatever randomization the keyframe carried. </summary>
        public static IVector2 ImportVector(float x, float y, VgdKeyframe source,
            InteropReport report = null, string path = null)
        {
            if (source == null) return new Vector2Value(x, y);

            var otherX = source.GetRandom(0);
            var otherY = source.GetRandom(1);
            var interval = source.GetRandom(2);

            switch ((ABRandomType)source.RandomType)
            {
                case ABRandomType.None:
                    return new Vector2Value(x, y);

                case ABRandomType.Linear:
                    return MakeRect(x, y, otherX, otherY, interval);

                case ABRandomType.LinearRounded:
                    return MakeRect(x, y, otherX, otherY, WholeNumberInterval);

                case ABRandomType.Toggle:
                {
                    // Over there ONE flip picks both components, so the point lands on one corner
                    // of the rectangle or the opposite one and never on the other two. Here each
                    // axis is rolled at its own address, so the two mixed corners become reachable
                    // - the rectangle is right, the pairing is not, and that is what is reported.
                    if (Math.Abs(otherX - x) > MinInterval && Math.Abs(otherY - y) > MinInterval)
                        report?.Approximated("random_toggle_axes_independent",
                            "Afterbeat's Toggle randomization flips both axes of a point together; this format rolls each axis on its own, so those keyframes can also land on the two mixed corners.",
                            path);

                    var stepX = Math.Abs(otherX - x);
                    var stepY = Math.Abs(otherY - y);
                    var step = Math.Max(Math.Max(stepX, stepY), MinInterval);
                    return new Vector2RectStep(Min(x, otherX), Min(y, otherY),
                        Max(x, otherX), Max(y, otherY), step);
                }

                case ABRandomType.Scale:
                {
                    // One factor scales BOTH components over there, which a per-axis rectangle
                    // cannot say - the two mixed corners are reachable here and are not there.
                    var min = source.GetRandom(0);
                    var max = source.GetRandom(1);

                    if (Math.Abs(max - min) > MinInterval
                        && Math.Abs(x) > MinInterval && Math.Abs(y) > MinInterval)
                        report?.Approximated("random_scale_axes_independent",
                            "Afterbeat's Scale randomization multiplies both axes of a point by the same factor; this format rolls each axis on its own, so the two are no longer locked to each other.",
                            path);

                    return MakeRect(x * min, y * min, x * max, y * max,
                        Math.Max(Math.Abs(x * interval), Math.Abs(y * interval)));
                }

                default:
                    report?.Dropped("random_unknown",
                        $"Randomization type {source.RandomType} is not one this converter knows; those keyframes use their authored value.",
                        path);
                    return new Vector2Value(x, y);
            }
        }

        private static IVector2 MakeRect(float minX, float minY, float maxX, float maxY, float interval)
        {
            var x0 = Min(minX, maxX);
            var y0 = Min(minY, maxY);
            var x1 = Max(minX, maxX);
            var y1 = Max(minY, maxY);

            if (Math.Abs(x1 - x0) < MinInterval && Math.Abs(y1 - y0) < MinInterval)
                return new Vector2Value(x0, y0);

            return interval > MinInterval
                ? new Vector2RectStep(x0, y0, x1, y1, interval)
                : new Vector2Rect(x0, y0, x1, y1);
        }

        #endregion

        #region Export

        /// <summary> The single number a polymorphic float resolves to on the way out. A range has
        /// no representation in a plain Afterbeat keyframe, so it collapses to its midpoint. </summary>
        public static float ExportFloat(IFloat value, InteropReport report = null, string path = null)
        {
            switch (value)
            {
                case null:
                    return 0f;
                case FloatValue literal:
                    return literal.Value;
                case FloatMinMaxStep step:
                    ReportRandom(report, path);
                    return (step.Min + step.Max) * 0.5f;
                case FloatMinMax range:
                    ReportRandom(report, path);
                    return (range.Min + range.Max) * 0.5f;
                default:
                    ReportRandom(report, path);
                    return 0f;
            }
        }

        /// <summary> The same for a point. </summary>
        public static (float X, float Y) ExportVector(IVector2 value,
            InteropReport report = null, string path = null)
        {
            switch (value)
            {
                case null:
                    return (0f, 0f);
                case Vector2Value literal:
                    return (literal.X, literal.Y);
                case Vector2RectStep step:
                    ReportRandom(report, path);
                    return ((step.MinX + step.MaxX) * 0.5f, (step.MinY + step.MaxY) * 0.5f);
                case Vector2Rect rect:
                    ReportRandom(report, path);
                    return ((rect.MinX + rect.MaxX) * 0.5f, (rect.MinY + rect.MaxY) * 0.5f);
                // The uniform forms carry the same rect - they differ only in how it resolves at
                // playback - so they export to the same midpoint rather than falling to the default
                // arm, which would drop the point to the origin.
                case Vector2RectStepUniform stepUniform:
                    ReportRandom(report, path);
                    return ((stepUniform.MinX + stepUniform.MaxX) * 0.5f,
                        (stepUniform.MinY + stepUniform.MaxY) * 0.5f);
                case Vector2RectUniform rectUniform:
                    ReportRandom(report, path);
                    return ((rectUniform.MinX + rectUniform.MaxX) * 0.5f,
                        (rectUniform.MinY + rectUniform.MaxY) * 0.5f);
                case Vector2Circle circle:
                    ReportRandom(report, path);
                    return (circle.X, circle.Y);
                default:
                    ReportRandom(report, path);
                    return (0f, 0f);
            }
        }

        private static void ReportRandom(InteropReport report, string path)
            => report?.Approximated("random_resolved",
                "Afterbeat keyframes cannot carry this format's random ranges; those values export as the middle of their range.",
                path);

        #endregion

        #region Rotation

        /// <summary>
        /// Turns Afterbeat's chain of relative degree deltas into this format's absolute radians.
        /// Call once per track, in keyframe order, carrying <paramref name="accumulatedDegrees"/>
        /// between calls.
        /// </summary>
        public static float AccumulateRotation(float deltaDegrees, ref float accumulatedDegrees)
        {
            accumulatedDegrees += deltaDegrees;
            return accumulatedDegrees * DegreesToRadians;
        }

        // Rotation randomization used to be dropped here, silently and completely - the track read
        // its value with GetValue(0) and never looked at the keyframe's own r/er, so two thirds of
        // every real workshop level's rotation keyframes converted to a fixed angle and no report
        // said so. It is the same five types as every other track; only the ARITHMETIC differs.
        //
        // Afterbeat randomizes a rotation DELTA, this format randomizes an ABSOLUTE angle, so the
        // delta's range is added onto the running total. That is exact for a single random keyframe
        // and an APPROXIMATION for a run of them: over there each roll moves the base every later
        // keyframe is measured from, so the spread compounds along the track, while every key here
        // rolls at its own address against the authored total. The per-keyframe spread is right and
        // the drift between keyframes is not - which is what the caller reports once per track.

        /// <summary>
        /// One of Afterbeat's relative degree deltas as this format's absolute radians, carrying
        /// whatever randomization the keyframe held. Call once per track, in keyframe order,
        /// carrying <paramref name="accumulatedDegrees"/> between calls.
        /// </summary>
        public static IFloat AccumulateRotationValue(float deltaDegrees, VgdKeyframe source,
            ref float accumulatedDegrees, float offsetRadians = 0f,
            InteropReport report = null, string path = null)
        {
            var baseDegrees = accumulatedDegrees;
            var radians = AccumulateRotation(deltaDegrees, ref accumulatedDegrees) + offsetRadians;

            if (source == null || !TryRotationDelta(deltaDegrees, source,
                    out var a, out var b, out var stepDegrees, report, path))
                return new FloatValue(radians);

            var min = (baseDegrees + Min(a, b)) * DegreesToRadians + offsetRadians;
            var max = (baseDegrees + Max(a, b)) * DegreesToRadians + offsetRadians;
            if (max - min < MinInterval) return new FloatValue(radians);

            var step = Math.Abs(stepDegrees) * DegreesToRadians;
            return step > MinInterval
                ? new FloatMinMaxStep(min, max, step)
                : new FloatMinMax(min, max);
        }

        /// <summary> Whether this keyframe randomizes its delta, and between which two degree
        /// amounts - the same five types every other track takes, applied to a delta. </summary>
        private static bool TryRotationDelta(float deltaDegrees, VgdKeyframe source,
            out float a, out float b, out float stepDegrees, InteropReport report, string path)
        {
            a = deltaDegrees;
            b = deltaDegrees;
            stepDegrees = 0f;

            switch ((ABRandomType)source.RandomType)
            {
                case ABRandomType.None:
                    return false;

                case ABRandomType.Linear:
                    b = source.GetRandom(0);
                    stepDegrees = source.GetRandom(2);
                    return true;

                case ABRandomType.LinearRounded:
                    b = source.GetRandom(0);
                    stepDegrees = WholeNumberInterval;
                    return true;

                case ABRandomType.Toggle:
                    // A step equal to the whole width leaves exactly the two ends reachable, which
                    // is what a toggle is - and for one number the pairing problem a vector has
                    // does not arise, so this crosses exactly.
                    b = source.GetRandom(0);
                    stepDegrees = Math.Abs(b - a);
                    return true;

                case ABRandomType.Scale:
                    // The factor multiplies the DELTA, and the interval snaps that same factor.
                    a = deltaDegrees * source.GetRandom(0);
                    b = deltaDegrees * source.GetRandom(1);
                    stepDegrees = Math.Abs(deltaDegrees * source.GetRandom(2));
                    return true;

                default:
                    report?.Dropped("random_unknown",
                        $"Randomization type {source.RandomType} is not one this converter knows; those keyframes use their authored value.",
                        path);
                    return false;
            }
        }

        /// <summary>
        /// The reverse - absolute radians back to the delta in degrees Afterbeat expects. Carry
        /// <paramref name="previousRadians"/> between calls; it starts at zero, because an object's
        /// first rotation keyframe is a delta from no rotation at all.
        /// </summary>
        public static float DifferentiateRotation(float absoluteRadians, ref float previousRadians)
        {
            var delta = absoluteRadians - previousRadians;
            previousRadians = absoluteRadians;
            return delta * RadiansToDegrees;
        }

        #endregion

        private static float Min(float a, float b) => a <= b ? a : b;
        private static float Max(float a, float b) => a >= b ? a : b;
    }
}
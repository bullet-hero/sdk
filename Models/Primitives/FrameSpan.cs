using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Models.Primitives
{
    // Half-open [Start, Start + Duration): nothing here can represent an inclusive end, and LastFrame is
    // the only conversion. Both fields are stored biased by their own floor, so the all-zero pattern
    // (default, Reset, a degraded corrupt file) is the smallest LEGAL span.

    /// <summary>
    /// Half-open frame interval [StartFrame, StartFrame + FrameDuration) plus the anchor flags saying
    /// which edges follow the parent's. No illegal value is representable; default is one frame at MinFrame.
    /// </summary>
    public struct FrameSpan : IModel<FrameSpan>, IComparable<FrameSpan>
    {
        private const int ValueMask = int.MaxValue;
        private const int AnchorFlag = int.MinValue;

        private int _rawStart;
        private int _rawDuration;

        /// <summary> First frame the span covers. </summary>
        public readonly int StartFrame => (_rawStart & ValueMask) + FrameRules.MinFrame;

        /// <summary> How many frames the span covers, never below one. </summary>
        public readonly int FrameDuration => (_rawDuration & ValueMask) + FrameRules.MinFrameDuration;

        /// <summary> First frame AFTER the span - an exclusive boundary, never a covered frame. </summary>
        public readonly int EndFrame => StartFrame + FrameDuration;

        // The one inclusive number in the format, and the only reason it exists is the vendored
        // interval tree, whose Query/Add are inclusive on both ends and offer no half-open mode.
        // Keeping the conversion here means no call site anywhere writes a bare -1.

        /// <summary> Last frame the span actually covers, for inclusive-interval consumers only. </summary>
        public readonly int LastFrame => EndFrame - 1;

        /// <summary> Which edges follow the parent span's edges. </summary>
        public readonly FrameAnchor Anchors =>
            (_rawStart < 0 ? FrameAnchor.Start : FrameAnchor.None) |
            (_rawDuration < 0 ? FrameAnchor.End : FrameAnchor.None);

        /// <summary> True when the start edge follows the parent's. </summary>
        public readonly bool IsAnchoredStart => _rawStart < 0;

        /// <summary> True when the end edge follows the parent's. </summary>
        public readonly bool IsAnchoredEnd => _rawDuration < 0;

        /// <summary> Both arguments are CLAMPED into the invariants rather than checked, so no illegal span is
        /// representable. </summary>
        public FrameSpan(int startFrame, int frameDuration, FrameAnchor anchors = FrameAnchor.None)
        {
            var start = BHSDKMath.Clamp(startFrame, FrameRules.MinFrame, FrameRules.MaxFrame);

            // The ceiling is "how many frames are left from start", i.e. MaxFrame - start + 1, and
            // MaxFrame is MaxFrameDuration - so a span starting on the first frame may still cover a
            // whole maximal timeline.
            var duration = BHSDKMath.Clamp(frameDuration,
                FrameRules.MinFrameDuration, FrameRules.MaxFrame - start + 1);

            _rawStart = (start - FrameRules.MinFrame) | ((anchors & FrameAnchor.Start) != 0 ? AnchorFlag : 0);
            _rawDuration = (duration - FrameRules.MinFrameDuration) |
                           ((anchors & FrameAnchor.End) != 0 ? AnchorFlag : 0);
        }

        // The START is clamped before the length is taken: a raw difference would let an out-of-range
        // start silently LENGTHEN the span ([0, 100) becoming [1, 101)). The end is what survives.

        /// <summary> Builds from a half-open pair, where endFrame is the first frame NOT covered. </summary>
        public static FrameSpan FromBounds(int startFrame, int endFrame) =>
            FromBounds(startFrame, endFrame, FrameAnchor.None);

        /// <summary> Builds from a half-open pair, keeping the given anchors. </summary>
        public static FrameSpan FromBounds(int startFrame, int endFrame, FrameAnchor anchors)
        {
            var start = BHSDKMath.Clamp(startFrame, FrameRules.MinFrame, FrameRules.MaxFrame);
            return new FrameSpan(start, endFrame - start, anchors);
        }

        /// <summary> True when the span covers that frame. </summary>
        public readonly bool Contains(int frame) => frame >= StartFrame && frame < EndFrame;

        /// <summary> True when the other span lies entirely inside this one. </summary>
        public readonly bool Contains(in FrameSpan other) =>
            other.StartFrame >= StartFrame && other.EndFrame <= EndFrame;

        /// <summary> True when the two spans share at least one frame. </summary>
        public readonly bool Overlaps(in FrameSpan other) =>
            StartFrame < other.EndFrame && other.StartFrame < EndFrame;

        // A local frame is a FRAME, not an offset: the span's own first frame is local MinFrame, as the
        // keyframe inspector shows.

        /// <summary> Absolute frame to one local to this span's start (the form keyframes store). </summary>
        public readonly int ToLocalFrame(int globalFrame) => globalFrame - StartFrame + FrameRules.MinFrame;

        /// <summary> Local frame back to absolute. </summary>
        public readonly int ToGlobalFrame(int localFrame) => StartFrame + localFrame - FrameRules.MinFrame;

        /// <summary> The same span moved to a new start, keeping its length. </summary>
        public readonly FrameSpan WithStart(int startFrame) => new(startFrame, FrameDuration, Anchors);

        /// <summary> The same span given a new length. </summary>
        public readonly FrameSpan WithDuration(int frameDuration) => new(StartFrame, frameDuration, Anchors);

        /// <summary> The same span stretched to a new exclusive end. </summary>
        public readonly FrameSpan WithEnd(int endFrame) => FromBounds(StartFrame, endFrame, Anchors);

        /// <summary> The same span with different edges following the parent. </summary>
        public readonly FrameSpan WithAnchors(FrameAnchor anchors) => new(StartFrame, FrameDuration, anchors);

        /// <summary> The same span slid along the timeline, keeping its length. </summary>
        public readonly FrameSpan Shifted(int deltaFrames) => new(StartFrame + deltaFrames, FrameDuration, Anchors);

        // Both edges are clamped rather than the span being moved, so an object stays where the
        // author put it and only loses the part that no longer fits. FrameDuration >= 1 survives
        // because the parent is at least one frame long itself.

        /// <summary> This span cut down to fit inside parent, keeping its own anchors. </summary>
        public readonly FrameSpan ClampedInto(in FrameSpan parent)
        {
            var start = BHSDKMath.Clamp(StartFrame, parent.StartFrame, parent.LastFrame);
            var end = BHSDKMath.Clamp(EndFrame, start + FrameRules.MinFrameDuration, parent.EndFrame);
            return FromBounds(start, end, Anchors);
        }

        /// <summary> Back to the smallest legal span - one frame, on the timeline's first frame. </summary>
        public void Reset()
        {
            _rawStart = 0;
            _rawDuration = 0;
        }

        /// <summary> The untyped spelling of <c>Copy</c>. </summary>
        public readonly object Clone() => Copy();

        /// <summary> A deep copy, sharing nothing mutable with this one. </summary>
        public readonly FrameSpan Copy() => this;

        /// <summary> Becomes the source, replacing everything this instance held. </summary>
        public void Update(FrameSpan src)
        {
            this = src;
        }

        /// <summary> Takes the source's contents in place, so nothing pointing inside this instance is invalidated. </summary>
        public void Pull(FrameSpan src)
        {
            this = src;
        }

        /// <summary> Member by member. </summary>
        public readonly bool Equals(FrameSpan other) =>
            _rawStart == other._rawStart && _rawDuration == other._rawDuration;

        /// <summary> The same, boxed. </summary>
        public readonly override bool Equals(object obj) => obj is FrameSpan other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        public readonly override int GetHashCode() => HashCode.Combine(_rawStart, _rawDuration);

        /// <summary> Ordered by start, then by length - the order a timeline lists them in. </summary>
        public readonly int CompareTo(FrameSpan other)
        {
            var compareStart = StartFrame.CompareTo(other.StartFrame);
            return compareStart != 0 ? compareStart : FrameDuration.CompareTo(other.FrameDuration);
        }

        /// <summary> One line, for a log. </summary>
        public readonly override string ToString() => $"[{StartFrame}, {EndFrame})";
    }
}
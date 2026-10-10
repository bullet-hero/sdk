using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Enums;

namespace BH.SDK.Rules
{
    /// <summary> What a frame and a timeline may be: the floor of both, the ceiling a level cannot outgrow, and
    /// the conversions between frames, time and playback speed. </summary>
    public static class FrameRules
    {
        // THE TIMELINE COUNTS FROM ONE, a format decision: frame f covers [(f-1)/fps, f/fps). FRAME, COUNT
        // and DELTA are three kinds of number and only the first shifts (FrameUtils has a pair for each).
        // Full convention: "Frames, spans and the timeline" in the root CLAUDE.md.

        /// <summary> Lowest frame allowed, read by AudioFileLevelGenerator, AudioTextureGeneratorsTests, BaseLevelGenerator and 40 more. </summary>
        public const int MinFrame = 1;

        // "There is no frame here": zero, below MinFrame, so it never collides with a real one. NOT an
        // index sentinel - List.IndexOf, enumerator cursors and the blob's null-length prefix keep -1.

        /// <summary> The answer when there is no frame at all - a lookup that found nothing, a field
        /// not yet set, a level holding no content. Never a frame the timeline can reach. </summary>
        public const int NoFrame = 0;

        /// <summary> Lower bound of IFrameDuration.FrameDuration, LevelSettings.FrameDuration. </summary>
        public const int MinFrameDuration = 1;

        /// <summary> Lowest time allowed. </summary>
        public const float MinTime = 0f;

        // Upper bound of a timeline and of every Frame, chosen from what a level can plausibly be (about
        // 4.6 hours at 60 fps), so a hostile file cannot claim a two-billion-frame timeline.

        /// <summary> Upper bound of LevelSettings.FrameDuration. </summary>
        public const int MaxFrameDuration = 1_000_000;

        /// <summary> Highest frame allowed, read by FrameOriginTests, FrameSpan, FrameSpanTests and 2 more. </summary>
        public const int MaxFrame = MaxFrameDuration;

        // Only the outer cap a hostile file is held to. The real bound is per placement and cannot be a
        // property rule: PlacementOffset <= Span.StartFrame - MinFrame, or FrameSpan clamps silently.

        /// <summary> Lower bound of PrefabObject.PlacementOffset - negative, since content starting
        /// LATER than its placement is an ordinary empty head. </summary>
        public const int MinPlacementOffset = -MaxFrameDuration;

        /// <summary> Upper bound of PrefabObject.PlacementOffset. </summary>
        public const int MaxPlacementOffset = MaxFrameDuration;

        /// <summary> Lower bound of LevelSettings.Fps. </summary>
        public const int MinFramerate = 1;

        /// <summary> Upper bound of LevelSettings.Fps. </summary>
        public const int MaxFramerate = 1000;

        // Absorbs float32 round-trip error of ToTime (18.999998f must not floor to 18). Tiny against a
        // frame, so genuine continuous playback time is unaffected.

        /// <summary> The delta framerate. </summary>
        public const float DeltaFramerate = 1f / MaxFramerate;

        /// <summary> Lowest speed allowed. </summary>
        public const float MinSpeed = -2f;

        /// <summary> Highest speed allowed. </summary>
        public const float MaxSpeed = 2f;

        /// <summary> The speed used when nothing says otherwise. </summary>
        public const float DefaultSpeed = 1f;

        /// <summary> Granularity the speed step is quantized to. </summary>
        public const float SpeedStep = 0.1f;

        /// <summary> The ease used when nothing says otherwise, read by BaseSpawnGenerator, BulletLaserSweepGenerator. </summary>
        public const EaseType DefaultEase = EaseType.Linear;

        /// <summary> True when a framerate can be divided by. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidFramerate(int framerate) => framerate > 0;

        /// <summary> Refuses a framerate nothing can be converted against. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertFramerate(int framerate)
        {
            if (!IsValidFramerate(framerate))
                throw new Exception("Framerate must be greater than 0");
        }

        /// <summary> True when a time is on the timeline at all. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidTime(float time) => time >= 0f;

        /// <summary> Refuses a time before the start of the level. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertTime(float time)
        {
            if (!IsValidTime(time))
                throw new Exception("Time can't be negative");
        }

        /// <summary> True when a frame number is on the timeline at all. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidFrame(int frame) => frame >= MinFrame;

        /// <summary> Refuses a frame before the start of the level, <see cref="NoFrame"/> included. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertFrame(int frame)
        {
            if (!IsValidFrame(frame))
                throw new Exception($"Frame must be at least {MinFrame}");
        }

        // The derivations exist so nobody spells the origin inline again ("last frame of N" was N - 1 in
        // about twenty places).

        /// <summary> The last frame a timeline of <paramref name="frameCount"/> frames covers. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastFrameOf(int frameCount) => MinFrame + frameCount - 1;

        /// <summary> How many frames a timeline ending on <paramref name="lastFrame"/> holds - the inverse
        /// of <see cref="LastFrameOf"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOf(int lastFrame) => lastFrame - MinFrame + 1;

        /// <summary> The exclusive boundary just past a timeline of <paramref name="frameCount"/> frames,
        /// i.e. what a FrameSpan covering all of it would report as its EndFrame. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EndBoundaryOf(int frameCount) => MinFrame + frameCount;

        // Progress is measured between the first and the LAST reachable frame, not against the length: the
        // playhead never reaches the end boundary, so elapsed / frameCount topped out at (N-1)/N.

        /// <summary> How far through a timeline of <paramref name="frameCount"/> frames a frame is, 0 to 1 -
        /// <see cref="MinFrame"/> and below report 0, <see cref="LastFrameOf"/> and beyond report 1. </summary>
        public static float ProgressOf(int frame, int frameCount)
        {
            if (frameCount <= 0) return 0f;

            var lastFrame = LastFrameOf(frameCount);

            // A one-frame timeline has no span to divide by: its only frame is both ends at once, so
            // standing on it is standing at the end.
            if (lastFrame <= MinFrame) return frame >= MinFrame ? 1f : 0f;

            if (frame <= MinFrame) return 0f;
            if (frame >= lastFrame) return 1f;

            return (frame - MinFrame) / (float)(lastFrame - MinFrame);
        }

        /// <summary> True when a playback speed is inside what the game offers - negatives play backwards. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidSpeed(float speed) => speed is >= MinSpeed and <= MaxSpeed;

        /// <summary> Refuses a playback speed outside that range. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertSpeed(float speed)
        {
            if (!IsValidSpeed(speed))
                throw new Exception($"Speed must be in bounds ({MinSpeed}, {MaxSpeed})");
        }
    }
}
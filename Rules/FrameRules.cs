using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Enums;

namespace BH.SDK.Rules
{
    /// <summary> What a frame and a timeline may be: the floor of both, the ceiling a level cannot outgrow, and
    /// the conversions between frames, time and playback speed. </summary>
    public static class FrameRules
    {
        // THE TIMELINE COUNTS FROM ONE, and it is a format decision rather than a display one: the
        // number an author reads off the playhead, the number the model holds and the number in the
        // file are all the same number. Frame f covers [(f-1)/fps, f/fps), so frame 1 begins at t=0
        // and audio needs no compensation.
        //
        // Everything RELATIVE is unaffected by where the counting starts - a duration, a difference,
        // an overlap test, a sort. Only three kinds of number exist here and only one of them moves:
        // a FRAME is a position and starts at one, a COUNT is how many frames something lasts, a
        // DELTA is a difference between two frames. Mixing them is the whole failure mode of this
        // file, which is why FrameUtils carries ToFrame/ToTime for the first kind and
        // ToFrameCount/ToSecondsCount for the second rather than one pair for both.

        /// <summary> Lowest frame allowed, read by ABEventsImporter, ABLevelGenerator, ABLevelImporter and 19 more. </summary>
        public const int MinFrame = 1;

        // Freed by the origin move and worth spending on the one thing every frame-valued field
        // needed and had to spell as -1: "there is no frame here". Zero is below MinFrame, so it can
        // never collide with a real one, and it is what a default-initialised int already holds.
        //
        // It is NOT an index sentinel. List.IndexOf, enumerator cursors and the blob's null-length
        // prefix keep their own -1, which means something else entirely and often sits in a method
        // right next to one of these.

        /// <summary> The answer when there is no frame at all - a lookup that found nothing, a field
        /// not yet set, a level holding no content. Never a frame the timeline can reach. </summary>
        public const int NoFrame = 0;

        /// <summary> Lower bound of IFrameDuration.FrameDuration, LevelSettings.FrameDuration, Prefab.FrameDuration. </summary>
        public const int MinFrameDuration = 1;

        /// <summary> Lowest time allowed. </summary>
        public const float MinTime = 0f;

        // Upper bound of a timeline, and therefore of every Frame in the format. Chosen from what a
        // level can plausibly be rather than from int.MaxValue: at the default 60fps this is ~4.6
        // hours, at the 1000fps ceiling ~17 minutes. Without it a hostile or corrupt file can claim
        // a two-billion-frame timeline, which every editor timeline widget and every "allocate per
        // frame" consumer has to survive.

        /// <summary> Upper bound of LevelSettings.FrameDuration. </summary>
        public const int MaxFrameDuration = 1_000_000;

        /// <summary> Highest frame allowed, read by ABMapTests, ABTimeMap, FrameSpan and 2 more. </summary>
        public const int MaxFrame = MaxFrameDuration;

        /// <summary> Lower bound of LevelSettings.Fps. </summary>
        public const int MinFramerate = 1;

        /// <summary> Upper bound of LevelSettings.Fps. </summary>
        public const int MaxFramerate = 1000;

        // Absorbs float32 round-trip error from ToTime (frame -> time -> frame must always
        // recover the exact original frame). Without it, a time produced by ToTime(frame, ...)
        // can land a hair below the intended integer (e.g. 18.999998f instead of 19f) and Floor
        // below would drop it a whole frame early. Tiny relative to a frame (1/framerate), so it
        // doesn't affect genuine continuous playback time.

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

        /// <summary> The ease used when nothing says otherwise, read by ABParallaxImporter, BaseSpawnGenerator, BulletLaserSweepGenerator. </summary>
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

        // THE THREE DERIVATIONS EXIST SO NOBODY WRITES THE ORIGIN INLINE AGAIN. Before the timeline
        // counted from one, "the last frame of a timeline N frames long" was spelled `N - 1` in
        // roughly twenty places, each of which had to be found and inverted by hand. Named, the
        // relationship is greppable and the next change to it is one line rather than twenty.

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
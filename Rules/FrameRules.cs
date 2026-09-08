using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Enums;

namespace BH.SDK.Rules
{
    /// <summary> What a frame and a timeline may be: the floor of both, the ceiling a level cannot outgrow, and
    /// the conversions between frames, time and playback speed. </summary>
    public static class FrameRules
    {
        /// <summary> Lowest frame allowed, read by ABEventsImporter, ABLevelGenerator, ABLevelImporter and 19 more. </summary>
        public const int MinFrame = 0;
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
        public const int MaxFrame = MaxFrameDuration - 1;

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
        public static bool IsValidFrame(int frame) => frame >= 0;

        /// <summary> Refuses a frame before the start of the level. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertFrame(int frame)
        {
            if (!IsValidFrame(frame))
                throw new Exception("Frame can't be negative");
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
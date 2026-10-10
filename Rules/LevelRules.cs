using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules
{
    /// <summary> What a level as a whole may hold - how many markers, checkpoints, beat segments and background
    /// events, and the seed convention. The object list itself is deliberately uncapped. </summary>
    public static class LevelRules
    {
        /// <summary> Upper bound of GameEvents.Markers. </summary>
        public const int MaxMarkerEvents = 1024;

        /// <summary> Upper bound of GameEvents.Checkpoints. </summary>
        public const int MaxCheckpointEvents = 128;

        /// <summary> Upper bound of GameEvents.Beats. </summary>
        public const int MaxBeatEvents = 256;

        /// <summary> Upper bound of GameEvents.Backgrounds. </summary>
        public const int MaxBackgroundEvents = 128;

        // 512, not the 128 other one-shot lists get: the theme track is interpolated, so a colour change
        // costs a PAIR of keyframes (a real level needed 146). Raising a cap invalidates nothing authored.

        /// <summary> Upper bound of GameEvents.Themes. </summary>
        public const int MaxThemeEvents = 512;

        /// <summary> Upper bound of GameEvents.ScreenLimits. </summary>
        public const int MaxScreenLimitEvents = 128;

        /// <summary> Upper bound of PlayerEvents.Collisions, PlayerEvents.Controls, PlayerEvents.Sizes and 4 more. </summary>
        public const int MaxPlayerKeys = 512;

        /// <summary> Upper bound of CameraEvents.Positions, CameraEvents.Rotations, CameraEvents.Shakes and 1 more. </summary>
        public const int MaxCameraKeys = 512;

        /// <summary> Upper bound of PostProcessingEvents.AnalogGlitches, PostProcessingEvents.Blooms, PostProcessingEvents.Chromatics and 9 more. </summary>
        public const int MaxPostProcessingKeys = 512;
        // Matches the camera and player tracks. Only a validation clamp or import truncation: nothing
        // sizes a buffer by it. Evidence for the size: BH.SDK/Docs/RULES.md, "Level and prefab".

        /// <summary> Upper bound of CameraEvents.Pivots, RectObject.AnchorsMax, RectObject.AnchorsMin and 11 more. </summary>
        public const int MaxObjectKeys = 512;

        /// <summary> Upper bound of LevelTrackEffects.StereoPans, LevelTrackEffects.Volumes. </summary>
        public const int MaxAudioKeys = 32;

        // Capped because a total count is what a LOADER pays (deserialize, id-map, parent-link every
        // object). 2^18 sits far above any authored level and far below what exhausts a phone.

        /// <summary> Upper bound of GameLevel.Objects. </summary>
        public const int MaxObjects = 262_144;

        // NOT MaxObjects: that is how many may EXIST; this is how many ids have ever been MINTED, and ids are
        // never reused. They diverge with editing time, so one cap made long-lived levels fail validation
        // and RuleInRange's clamping Fix then reissued live ids. The ceiling is the int's, never a wrap.

        /// <summary> Upper bound of LevelSettings.ObjectIdCounter. </summary>
        public const int MaxObjectIds = int.MaxValue;

        // Three quarters of the range: a validation Warning here tells the author long before minting
        // refuses, when only the compaction generator (LEVEL_MODEL_ANALYSIS.md D2) is left.

        /// <summary> Counter value at which validation starts warning that the range is running out. </summary>
        public const int ObjectIdCounterWarning = MaxObjectIds / 4 * 3;

        // A long on purpose: the int spelling of "how many left" overflows exactly at the end of the
        // range and would report an exhausted counter as having room.

        /// <summary> How many more ids a counter sitting at this value may still mint. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RemainingObjectIds(int counter) =>
            counter < ObjectId.MinLevelValue ? 0L : (long)MaxObjectIds - counter + 1L;

        /// <summary> Can a counter at this value mint <paramref name="count"/> more ids without
        /// crossing <see cref="MaxObjectIds"/> - the question a BULK create must ask before it
        /// writes anything, so it cannot commit half a run and then refuse. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanMintObjectIds(int counter, int count) =>
            count >= 0 && RemainingObjectIds(counter) >= count;

        /// <summary> Is a counter at this value far enough into the range to tell the author. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsObjectIdCounterNearExhaustion(int counter) => counter >= ObjectIdCounterWarning;

        /// <summary> Refuses a mint that would push the counter past the end of the id space, where
        /// the next value is not an id at all. The polite refusal belongs at the CALLER, which can
        /// decline before writing anything; this is the net under it. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertObjectIdAvailable(int counter)
        {
            if (!CanMintObjectIds(counter, 1))
                throw new Exception($"Object id counter {counter} has no id left to mint - " +
                                    $"the range ends at {MaxObjectIds} and ids are never reused");
        }

        // The player's MaxChildInherit (16, a fixed stackalloc) MINUS ONE: the editor parents its overlays
        // one level under the selected object, so a level at the runtime cap would lose its selection
        // border. Past 16 the player composes against a mid-chain ancestor. Cycles are a graph check.

        /// <summary> Highest object depth allowed, read by ContentRemoverGenerator, GeneratorContext, GraphRule and 6 more. </summary>
        public const int MaxObjectDepth = 15;

        // Bounds of one BeatSegment. The offset is bounded by the timeline, not one beat: a legal phase
        // depends on Bpm and a property rule sees one property. BeatMath normalizes it.

        /// <summary> Lower bound of BeatSegment.BPM. </summary>
        public const float MinBpm = 1f;

        /// <summary> Upper bound of BeatSegment.BPM. </summary>
        public const float MaxBpm = 1000f;

        /// <summary> The bpm used when nothing says otherwise, read by BeatSegment, BeatSegmentTests. </summary>
        public const float DefaultBpm = 120f;

        /// <summary> Lower bound of BeatSegment.Offset. </summary>
        public const float MinBeatOffset = -FrameRules.MaxFrameDuration;

        /// <summary> Upper bound of BeatSegment.Offset. </summary>
        public const float MaxBeatOffset = FrameRules.MaxFrameDuration;

        /// <summary> Lower bound of BeatSegment.BeatsPerBar. </summary>
        public const int MinBeatsPerBar = 1;

        /// <summary> Upper bound of BeatSegment.BeatsPerBar. </summary>
        public const int MaxBeatsPerBar = 32;

        /// <summary> The beats per bar used when nothing says otherwise, read by BeatSegment, BeatSegmentTests, LevelGraphAnalyzerTests. </summary>
        public const int DefaultBeatsPerBar = 4;

        // Cap on grid points one BeatMath collection may produce. Not a format limit (the grid is
        // computed): a fast tempo over a long segment is millions of beats.

        /// <summary> Highest beat grid points allowed, read by BeatMath. </summary>
        public const int MaxBeatGridPoints = 65_536;

        /// <summary> Upper bound of AudioLevel.Tracks. </summary>
        public const int MaxAudioTracks = 512;

        /// <summary> Upper bound of LevelMeta.ResourcesMeta. </summary>
        public const int MaxResourcesMeta = 512;

        // Cost is proportional to template content, nothing sizes a buffer by it. 64 was crossed by a real
        // workshop level (72 templates), which lost eight of them and 63 placements.

        /// <summary> Upper bound of LevelResources.Prefabs. </summary>
        public const int MaxPrefabs = 256;

        // Bounds of LevelHints.Limits: a sanity clamp so a hostile file cannot make a device preallocate
        // gigabytes. The hint is advisory; the real ceiling is per device, at runtime.

        /// <summary> Lower bound of LimitHints.Effects, LimitHints.Instances, LimitHints.ShapesOpaque and 3 more. </summary>
        public const int MinCapacityHint = 0;

        /// <summary> Upper bound of LimitHints.Effects, LimitHints.Instances, LimitHints.ShapesOpaque and 3 more. </summary>
        public const int MaxCapacityHint = 1_048_576; // 2^20

        // Zero is the ABSENCE of a seed in all three resolution tiers, and valid authored data, so a
        // predicate rather than a rule. [MinSeed, MaxValidSeed] is what the FIELD accepts (NullSeed
        // included); [MinValidSeed, MaxValidSeed] is what generators must draw from.

        /// <summary> The null seed, read by DisplayGraphicsSettings, LevelSettings, LevelSettingsTests and 1 more. </summary>
        public const int NullSeed = 0;

        /// <summary> Lower bound of LevelSettings.Seed. </summary>
        public const int MinSeed = 0;

        /// <summary> Lowest valid seed allowed. </summary>
        public const int MinValidSeed = 1;

        /// <summary> Highest valid seed allowed. </summary>
        public const int MaxValidSeed = int.MaxValue;

        /// <summary> Is this a real, usable seed - what a generator must produce and what playback
        /// ends up running on. NullSeed is NOT one. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidSeed(int seed) => seed >= MinValidSeed && seed <= MaxValidSeed;

        /// <summary> Refuses a seed that is neither "not set" nor a usable number. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertSeed(int seed)
        {
            if (!IsValidSeed(seed))
                throw new Exception($"Seed {seed} is outside [{MinValidSeed}, {MaxValidSeed}] - " +
                                    "0 means unseeded and negative values are never valid");
        }

        /// <summary> Is this something a seed FIELD may hold - the above, plus NullSeed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSeedInput(int seed) => seed >= MinSeed && seed <= MaxValidSeed;

        /// <summary> Refuses a seed handed to the hash, where "not set" is no longer an answer. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertSeedInput(int seed)
        {
            if (!IsSeedInput(seed))
                throw new Exception($"Seed {seed} is outside [{MinSeed}, {MaxValidSeed}] - " +
                                    "a seed field takes 0 (unseeded) or a real seed, never a negative");
        }

        /// <summary> Clamp for a seed INPUT, where NullSeed is a legal "leave it unseeded". </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ClampSeed(int seed) =>
            seed < MinSeed ? MinSeed : seed > MaxValidSeed ? MaxValidSeed : seed;
    }
}
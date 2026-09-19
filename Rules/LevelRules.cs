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

        // 512 RATHER THAN THE 128 THE OTHER ONE-SHOT EVENT LISTS GET, because this one is not a
        // list of one-shots: the theme track is an animated track like the camera's, interpolated
        // between neighbours, so an author states a colour change with a PAIR of keyframes and a
        // level whose palette follows its music spends them at that rate. A real imported level
        // came in at 146 and lost the last stretch of its colour work to the old cap. Raising a cap
        // invalidates nothing already authored - it only allows what could not be said before.

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
        // 32 was never justified in writing, and five real Afterbeat workshop levels say it was too
        // low by an order of magnitude: 1 948 authored tracks exceeded it, and the worst carried 189
        // keyframes across 123 seconds - truncating it froze that object for 100 of them. Every
        // consumer of this number is a validation clamp or an import truncation; nothing sizes a
        // buffer, a native collection or a blob field by it (a blob count is always an Int32), so
        // the cap costs only what an author actually writes. It matches the camera and player tracks
        // now for the reason stated above them: one number for every track is one fewer to explain.

        /// <summary> Upper bound of CameraEvents.Pivots, RectObject.AnchorsMax, RectObject.AnchorsMin and 11 more. </summary>
        public const int MaxObjectKeys = 512;

        /// <summary> Upper bound of LevelTrackEffects.StereoPans, LevelTrackEffects.Volumes. </summary>
        public const int MaxAudioKeys = 32;

        // Was deliberately uncapped for a long time, on the grounds that peak simultaneous objects
        // (LevelHints.Limits) is what actually costs anything at runtime. It is capped now because a
        // total count is what a LOADER pays for - every object is deserialized, id-mapped and
        // parent-linked before playback ever decides it is off-screen - so an unbounded count is an
        // unbounded load, not an unbounded frame. 2^18 sits far above any authored level and far
        // below what would exhaust a phone.

        /// <summary> Upper bound of GameLevel.Objects. </summary>
        public const int MaxObjects = 262_144;

        // NOT MaxObjects, and the difference is the whole point. MaxObjects is how many objects may
        // EXIST - what a loader pays for. This is how many ids have ever been MINTED, and ids are
        // never reused (see LevelSettings.ObjectIdCounter) so nothing ever gives one back: a
        // generator run, a paste, a prefab materialize and an undone creation all consume from here
        // permanently. The two quantities diverge with editing TIME, not with level size, and
        // binding the counter to the collection's cap made a long-lived level fail validation while
        // holding almost nothing - and fail it destructively, since RuleInRange is an Error whose
        // Fix CLAMPS: the repair wrote the counter back down and the next object created took an id
        // that was already live.
        //
        // An int cannot hold more, so this ceiling is the type's rather than a policy, and reaching
        // it needs 2.1 billion create gestures. What happens there anyway is the refusal below plus
        // the compaction generator designed in Docs/Issues/LEVEL_MODEL_ANALYSIS.md section D2 -
        // never a wrap and never a negative, those being game-space objects and the three reserved
        // parents.

        /// <summary> Upper bound of LevelSettings.ObjectIdCounter. </summary>
        public const int MaxObjectIds = int.MaxValue;

        // Three quarters of the range, and the only part of this whole area a player would ever see.
        // A validation WARNING here is what turns "the editor refused and I do not know why" into
        // something the author was told about long before the wall - by the time minting actually
        // refuses there is nothing left to do but run the compaction generator, and being told then
        // is being told too late.

        /// <summary> Counter value at which validation starts warning that the range is running out. </summary>
        public const int ObjectIdCounterWarning = MaxObjectIds / 4 * 3;

        // Returns a long on purpose. The obvious int spelling of "how many are left" overflows at
        // exactly the end of the range it exists to describe (MaxObjectIds - counter + 1 with the
        // counter at its floor is int.MaxValue, one step from wrapping), and an overflow HERE would
        // report a exhausted counter as having room, which is the one wrong answer that matters.

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

        // Longest parent chain AUTHORED CONTENT may have. Depth is walked per object per frame (a
        // child's transform and layer are the sum up its chain), so the real ceiling is the
        // consumer's: the Unity player walks it into a fixed stackalloc of
        // LevelPlayerSettings.MaxChildInherit (16) and, past that, composes an object against a
        // mid-chain ancestor instead of its root.
        //
        // This is that ceiling MINUS ONE. The editor parents its own overlays (the selection
        // outline's marching-ants segments, the gizmo handles) one level under the selected object,
        // so a level authored right at the runtime cap would push its own overlay past it - the
        // object would render correctly and its selection border would not. Cycles are a graph
        // invariant and checked separately.

        /// <summary> Highest object depth allowed, read by ABExportContext, ContentRemoverGenerator, GeneratorContext and 3 more. </summary>
        public const int MaxObjectDepth = 15;

        // Bounds of one BeatSegment. The tempo range covers everything a real song sits in with room
        // to spare on both sides; the offset is bounded by the timeline itself rather than by one
        // beat's length, since what a legal phase is depends on Bpm and a property attribute only
        // ever sees one property. BeatMath normalizes it into a single beat on the way out anyway.

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

        /// <summary> The beats per bar used when nothing says otherwise, read by ABEventsImporter, BeatSegment, BeatSegmentTests. </summary>
        public const int DefaultBeatsPerBar = 4;

        // How many grid points one BeatMath collection may produce. Not a format limit - the grid is
        // computed, never stored - but a fast tempo over a long segment is millions of beats, and
        // both consumers (a viewport redraw, a generator's beat list) would rather be cut off than
        // stall. A viewport never comes near it; the whole-level form is what it actually guards.

        /// <summary> Highest beat grid points allowed, read by BeatMath. </summary>
        public const int MaxBeatGridPoints = 65_536;

        /// <summary> Upper bound of AudioLevel.Tracks. </summary>
        public const int MaxAudioTracks = 512;

        /// <summary> Upper bound of LevelMeta.ResourcesMeta. </summary>
        public const int MaxResourcesMeta = 512;

        // Raised from 64, which a real workshop level already crossed: 72 templates, so eight of
        // them and the 63 placements referencing them were dropped - 524 of that level's 3 690
        // materialized objects, one seventh of what it draws. A prefab is a template sitting in the
        // resources, so what it costs is proportional to what is IN it rather than to this number,
        // and nothing sizes a buffer by it.

        /// <summary> Upper bound of LevelResources.Prefabs. </summary>
        public const int MaxPrefabs = 256;

        // Bounds of LevelHints.Limits - purely a format-level sanity clamp, so a corrupted or
        // hostile file can't ask a player's device to preallocate gigabytes before the runtime even
        // looks at the number. The real ceiling is per-device and applied at runtime; the hint
        // itself is advisory and never trusted on its own.

        /// <summary> Lower bound of LimitHints.Effects, LimitHints.Instances, LimitHints.ShapesOpaque and 3 more. </summary>
        public const int MinCapacityHint = 0;

        /// <summary> Upper bound of LimitHints.Effects, LimitHints.Instances, LimitHints.ShapesOpaque and 3 more. </summary>
        public const int MaxCapacityHint = 1_048_576; // 2^20

        // Zero is not "seed number zero", it is the absence of a seed - the same convention every
        // tier of seed resolution follows (per-launch override, then LevelSettings.Seed, then a
        // freshly generated one), so a consumer only ever has to ask IsValidSeed instead of
        // spelling out != 0 at each of the three steps. Shaped like AudioRules.IsActiveMixLevel: a
        // constant plus the one predicate that reads it, rather than a rule attribute, because 0 is
        // perfectly VALID authored data - it is what an unpinned level stores.
        //
        // Two ranges, not one, and confusing them is the easy mistake here. [MinSeed, MaxValidSeed]
        // is what the FIELD accepts, NullSeed included - that is what RuleMinValue validates and what a
        // seed input clamps to. [MinValidSeed, MaxValidSeed] is what a REAL seed lives in, and it is
        // what every generator must draw from: hand a run seed 0 and it silently means "unseeded",
        // so a generator that could produce it would occasionally produce a run nobody can reproduce.

        /// <summary> The null seed, read by ABLevelExporter, DisplayGraphicsSettings, LevelSettings and 2 more. </summary>
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
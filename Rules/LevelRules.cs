using System;
using System.Runtime.CompilerServices;

namespace BH.SDK.Rules
{
    public static class LevelRules
    {
        public const int MaxMarkerEvents = 1024;
        public const int MaxCheckpointEvents = 128;
        public const int MaxBeatEvents = 256;
        public const int MaxBackgroundEvents = 128;

        // 512 RATHER THAN THE 128 THE OTHER ONE-SHOT EVENT LISTS GET, because this one is not a
        // list of one-shots: the theme track is an animated track like the camera's, interpolated
        // between neighbours, so an author states a colour change with a PAIR of keyframes and a
        // level whose palette follows its music spends them at that rate. A real imported level
        // came in at 146 and lost the last stretch of its colour work to the old cap. Raising a cap
        // invalidates nothing already authored - it only allows what could not be said before.
        public const int MaxThemeEvents = 512;

        public const int MaxScreenLimitEvents = 128;

        public const int MaxPlayerKeys = 512;
        public const int MaxCameraKeys = 512;
        public const int MaxPostProcessingKeys = 512;
        public const int MaxObjectKeys = 32;
        public const int MaxAudioKeys = 32;

        // Was deliberately uncapped for a long time, on the grounds that peak simultaneous objects
        // (LevelHints.Limits) is what actually costs anything at runtime. It is capped now because a
        // total count is what a LOADER pays for - every object is deserialized, id-mapped and
        // parent-linked before playback ever decides it is off-screen - so an unbounded count is an
        // unbounded load, not an unbounded frame. 2^18 sits far above any authored level and far
        // below what would exhaust a phone.
        public const int MaxObjects = 262_144;

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
        public const int MaxObjectDepth = 15;

        // Bounds of one BeatSegment. The tempo range covers everything a real song sits in with room
        // to spare on both sides; the offset is bounded by the timeline itself rather than by one
        // beat's length, since what a legal phase is depends on Bpm and a property attribute only
        // ever sees one property. BeatMath normalizes it into a single beat on the way out anyway.
        public const float MinBpm = 1f;
        public const float MaxBpm = 1000f;
        public const float DefaultBpm = 120f;

        public const float MinBeatOffset = -FrameRules.MaxFrameDuration;
        public const float MaxBeatOffset = FrameRules.MaxFrameDuration;

        public const int MinBeatsPerBar = 1;
        public const int MaxBeatsPerBar = 32;
        public const int DefaultBeatsPerBar = 4;

        // How many grid points one BeatMath collection may produce. Not a format limit - the grid is
        // computed, never stored - but a fast tempo over a long segment is millions of beats, and
        // both consumers (a viewport redraw, a generator's beat list) would rather be cut off than
        // stall. A viewport never comes near it; the whole-level form is what it actually guards.
        public const int MaxBeatGridPoints = 65_536;

        public const int MaxAudioTracks = 512;
        public const int MaxResourcesMeta = 512;

        public const int MaxPrefabs = 64;

        // Bounds of LevelHints.Limits - purely a format-level sanity clamp, so a corrupted or
        // hostile file can't ask a player's device to preallocate gigabytes before the runtime even
        // looks at the number. The real ceiling is per-device and applied at runtime; the hint
        // itself is advisory and never trusted on its own.
        public const int MinCapacityHint = 0;
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
        public const int NullSeed = 0;
        public const int MinSeed = 0;

        public const int MinValidSeed = 1;
        public const int MaxValidSeed = int.MaxValue;

        /// <summary> Is this a real, usable seed - what a generator must produce and what playback
        /// ends up running on. NullSeed is NOT one. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidSeed(int seed) => seed >= MinValidSeed && seed <= MaxValidSeed;

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

using System;
using System.Collections.Generic;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Statistics;
using BH.SDK.Rules;

namespace BH.SDK.Services.Profile
{
    // MAX, NOT SUM, AND THAT IS A CHOICE ABOUT WHAT A MERGE IS FOR. The common case is one history
    // travelling - phone to PC and back - and a profile carries no identity a merge could subtract a
    // common base with. Summing would double every counter the second time the same history comes
    // home; max makes merging idempotent (merge(a, a) == a) and never inflates. What it gives up is two
    // genuinely separate histories, which it undercounts to the larger of the two. Decided with the
    // author; Docs/Plans/PROFILE_TRANSFER_PLAN*.md is the record.
    //
    // Everything else follows from "never invent a number": a first-time is the earlier of the two
    // that are set (a default DateTime is "never", not "the year 1"), a last-time is the later, a value
    // that describes the latest state (a level's name, the current streak) comes from whichever side
    // played last, and a record is the better of two real runs - never a blend.
    //
    // Merges write INTO the local instance and keep its nested instances, which is the contract
    // StatisticsStore relies on: views hold references into these objects.

    /// <summary> Merges two copies of a player's statistics into the local one. </summary>
    public static class StatisticsMerge
    {
        /// <summary> Merges <paramref name="incoming"/> into <paramref name="local"/>. </summary>
        public static void Merge(LevelStatistics local, LevelStatistics incoming)
        {
            if (local == null) throw new ArgumentNullException(nameof(local));
            if (incoming == null) return;

            var incomingLater = incoming.LastPlayedUtc > local.LastPlayedUtc;

            if (local.LevelId == LevelId.Null) local.LevelId = incoming.LevelId;
            if (incomingLater)
            {
                local.LevelName = incoming.LevelName?.Copy() ?? local.LevelName;
                if (incoming.LevelVersion != null) local.LevelVersion = (Version)incoming.LevelVersion.Clone();
            }

            local.FirstPlayedUtc = EarliestSet(local.FirstPlayedUtc, incoming.FirstPlayedUtc);
            local.LastPlayedUtc = Latest(local.LastPlayedUtc, incoming.LastPlayedUtc);
            local.FirstClearUtc = EarliestSet(local.FirstClearUtc, incoming.FirstClearUtc);

            local.TotalRealSeconds = Math.Max(local.TotalRealSeconds, incoming.TotalRealSeconds);
            local.SessionCount = Math.Max(local.SessionCount, incoming.SessionCount);
            local.Attempts = Math.Max(local.Attempts, incoming.Attempts);
            local.Clears = Math.Max(local.Clears, incoming.Clears);
            local.Deaths = Math.Max(local.Deaths, incoming.Deaths);
            local.Hits = Math.Max(local.Hits, incoming.Hits);
            local.Dashes = Math.Max(local.Dashes, incoming.Dashes);
            local.CheckpointRestarts = Math.Max(local.CheckpointRestarts, incoming.CheckpointRestarts);
            local.Quits = Math.Max(local.Quits, incoming.Quits);

            // A pair: the progress belongs to the frame it was reached at.
            if (incoming.BestFrame > local.BestFrame)
            {
                local.BestFrame = incoming.BestFrame;
                local.BestProgress = incoming.BestProgress;
            }

            if (incoming.Records != null)
                foreach (var pair in incoming.Records)
                {
                    if (pair.Value == null) continue;

                    var current = local.GetRecord(pair.Key);
                    if (current == null || BestRunOrder.Compare(pair.Value, current) > 0)
                        local.SetRecord(pair.Key, pair.Value.Copy());
                }

            MergeDifficulty(local.Difficulty, incoming.Difficulty, incomingLater);
            MergeEditor(local.Editor, incoming.Editor);
        }

        /// <summary> Merges <paramref name="incoming"/> into <paramref name="local"/>. When
        /// <paramref name="levels"/> - the per-level files AFTER their own merge - is given, the
        /// counts and the most-played level derived from them are recomputed as well. </summary>
        public static void Merge(GameStatistics local, GameStatistics incoming,
            IEnumerable<LevelStatistics> levels = null)
        {
            if (local == null) throw new ArgumentNullException(nameof(local));
            if (incoming == null) return;

            var incomingLater = incoming.Profile.LastPlayedUtc > local.Profile.LastPlayedUtc;

            var profile = local.Profile;
            profile.FirstPlayedUtc = EarliestSet(profile.FirstPlayedUtc, incoming.Profile.FirstPlayedUtc);
            profile.LastPlayedUtc = Latest(profile.LastPlayedUtc, incoming.Profile.LastPlayedUtc);
            profile.AppLaunches = Math.Max(profile.AppLaunches, incoming.Profile.AppLaunches);
            profile.TotalAppSeconds = Math.Max(profile.TotalAppSeconds, incoming.Profile.TotalAppSeconds);

            var screens = local.Screens;
            screens.MenuSeconds = Math.Max(screens.MenuSeconds, incoming.Screens.MenuSeconds);
            screens.GameSeconds = Math.Max(screens.GameSeconds, incoming.Screens.GameSeconds);
            screens.EditorSeconds = Math.Max(screens.EditorSeconds, incoming.Screens.EditorSeconds);
            screens.LoadingSeconds = Math.Max(screens.LoadingSeconds, incoming.Screens.LoadingSeconds);

            var devices = local.Devices;
            devices.KeyboardMouseSeconds = Math.Max(devices.KeyboardMouseSeconds, incoming.Devices.KeyboardMouseSeconds);
            devices.TouchscreenSeconds = Math.Max(devices.TouchscreenSeconds, incoming.Devices.TouchscreenSeconds);
            devices.GamepadSeconds = Math.Max(devices.GamepadSeconds, incoming.Devices.GamepadSeconds);
            devices.DeviceGyroSeconds = Math.Max(devices.DeviceGyroSeconds, incoming.Devices.DeviceGyroSeconds);

            local.Avatar.TotalDashes = Math.Max(local.Avatar.TotalDashes, incoming.Avatar.TotalDashes);
            local.Avatar.TotalDistanceMoved = Math.Max(local.Avatar.TotalDistanceMoved, incoming.Avatar.TotalDistanceMoved);

            var editor = local.Editor;
            editor.LevelsCreated = Math.Max(editor.LevelsCreated, incoming.Editor.LevelsCreated);
            editor.LevelsDeleted = Math.Max(editor.LevelsDeleted, incoming.Editor.LevelsDeleted);
            editor.ObjectsCreated = Math.Max(editor.ObjectsCreated, incoming.Editor.ObjectsCreated);
            editor.OperationsExecuted = Math.Max(editor.OperationsExecuted, incoming.Editor.OperationsExecuted);
            editor.GeneratorsRun = Math.Max(editor.GeneratorsRun, incoming.Editor.GeneratorsRun);
            editor.TotalResources = Math.Max(editor.TotalResources, incoming.Editor.TotalResources);

            var totals = local.Totals;
            totals.TotalAttempts = Math.Max(totals.TotalAttempts, incoming.Totals.TotalAttempts);
            totals.TotalClears = Math.Max(totals.TotalClears, incoming.Totals.TotalClears);
            totals.TotalDeaths = Math.Max(totals.TotalDeaths, incoming.Totals.TotalDeaths);
            totals.TotalHits = Math.Max(totals.TotalHits, incoming.Totals.TotalHits);
            totals.DistinctLevelsPlayed = Math.Max(totals.DistinctLevelsPlayed, incoming.Totals.DistinctLevelsPlayed);
            totals.DistinctLevelsCleared = Math.Max(totals.DistinctLevelsCleared, incoming.Totals.DistinctLevelsCleared);
            totals.TotalFramesSimulated = Math.Max(totals.TotalFramesSimulated, incoming.Totals.TotalFramesSimulated);

            var tutorial = local.Tutorial;
            tutorial.Completions = Math.Max(tutorial.Completions, incoming.Tutorial.Completions);
            tutorial.FirstCompletedUtc = EarliestSet(tutorial.FirstCompletedUtc, incoming.Tutorial.FirstCompletedUtc);
            tutorial.LastCompletedUtc = Latest(tutorial.LastCompletedUtc, incoming.Tutorial.LastCompletedUtc);

            var streaks = local.Streaks;
            streaks.LongestClearStreak = Math.Max(streaks.LongestClearStreak, incoming.Streaks.LongestClearStreak);
            if (incomingLater)
            {
                streaks.CurrentClearStreak = incoming.Streaks.CurrentClearStreak;
                streaks.LastPlayedLevelId = incoming.Streaks.LastPlayedLevelId;
            }

            if (incoming.Streaks.MostPlayedAttempts > streaks.MostPlayedAttempts)
            {
                streaks.MostPlayedAttempts = incoming.Streaks.MostPlayedAttempts;
                streaks.MostPlayedLevelId = incoming.Streaks.MostPlayedLevelId;
            }

            if (levels == null) return;

            var played = 0;
            var cleared = 0;

            foreach (var level in levels)
            {
                if (level == null) continue;
                if (level.Attempts > 0) played++;
                if (level.Cleared) cleared++;

                if (level.Attempts > streaks.MostPlayedAttempts)
                {
                    streaks.MostPlayedAttempts = level.Attempts;
                    streaks.MostPlayedLevelId = level.LevelId;
                }
            }

            totals.DistinctLevelsPlayed = Math.Max(totals.DistinctLevelsPlayed, played);
            totals.DistinctLevelsCleared = Math.Max(totals.DistinctLevelsCleared, cleared);
        }

        // THE HISTOGRAMS ARE TIED TO A LENGTH (DifficultyStatistics' header): a bucket is a fraction of
        // the level as it was when the deaths were counted. Two copies built against the same length
        // merge bucket by bucket; built against different lengths they describe two different levels,
        // and the copy from whichever side played last is the one describing the level that exists.
        private static void MergeDifficulty(DifficultyStatistics local, DifficultyStatistics incoming,
            bool incomingLater)
        {
            if (local == null || incoming == null || !incoming.HasValue) return;

            if (!local.HasValue || local.BucketFrameDuration != incoming.BucketFrameDuration)
            {
                if (local.HasValue && !incomingLater) return;

                local.BucketFrameDuration = incoming.BucketFrameDuration;
                local.DeathsByBucket = CopyOf(incoming.DeathsByBucket);
                local.HitsByBucket = CopyOf(incoming.HitsByBucket);
                local.DeathsBeforeCheckpoint = incoming.DeathsBeforeCheckpoint;
                local.DeathsByCheckpoint.Clear();
                foreach (var pair in incoming.DeathsByCheckpoint)
                    local.DeathsByCheckpoint[pair.Key] = new CheckpointDeaths(pair.Value.Frame, pair.Value.Deaths);
                return;
            }

            MaxInto(local.DeathsByBucket, incoming.DeathsByBucket);
            MaxInto(local.HitsByBucket, incoming.HitsByBucket);
            local.DeathsBeforeCheckpoint = Math.Max(local.DeathsBeforeCheckpoint, incoming.DeathsBeforeCheckpoint);

            foreach (var pair in incoming.DeathsByCheckpoint)
            {
                if (local.DeathsByCheckpoint.TryGetValue(pair.Key, out var mine))
                {
                    mine.Deaths = Math.Max(mine.Deaths, pair.Value.Deaths);
                    continue;
                }

                if (local.DeathsByCheckpoint.Count >= StatisticsRules.MaxCheckpointDeaths) continue;
                local.DeathsByCheckpoint[pair.Key] = new CheckpointDeaths(pair.Value.Frame, pair.Value.Deaths);
            }
        }

        private static void MergeEditor(LevelEditorStatistics local, LevelEditorStatistics incoming)
        {
            if (local == null || incoming == null) return;

            local.EditorOpens = Math.Max(local.EditorOpens, incoming.EditorOpens);
            local.TotalEditSeconds = Math.Max(local.TotalEditSeconds, incoming.TotalEditSeconds);
            local.LastEditedUtc = Latest(local.LastEditedUtc, incoming.LastEditedUtc);
            local.Saves = Math.Max(local.Saves, incoming.Saves);
            local.Autosaves = Math.Max(local.Autosaves, incoming.Autosaves);
            local.Operations = Math.Max(local.Operations, incoming.Operations);
        }

        private static void MaxInto(int[] local, int[] incoming)
        {
            if (local == null || incoming == null) return;

            var count = Math.Min(local.Length, incoming.Length);
            for (var i = 0; i < count; i++)
                local[i] = Math.Max(local[i], incoming[i]);
        }

        private static int[] CopyOf(int[] source) => source == null ? new int[StatisticsRules.BucketCount] : (int[])source.Clone();

        private static DateTime EarliestSet(DateTime a, DateTime b)
        {
            if (a == default) return b;
            if (b == default) return a;
            return a <= b ? a : b;
        }

        private static DateTime Latest(DateTime a, DateTime b) => a >= b ? a : b;
    }

    // THE SAME ORDER THE GAME FILES A RECORD BY - further, then fewer hits, then more lives left, then
    // fewer dashes (Services.Shared's StatisticsMath.IsBetter, which compares a live run rather than two
    // stored ones). One more tie-break here, because two stored runs can be equal in all four and a
    // merge still has to pick: the earlier one, since it was achieved first. Change one, change both.

    /// <summary> Orders two stored records the way the game decides a new one is better. </summary>
    public static class BestRunOrder
    {
        /// <summary> Positive when <paramref name="a"/> is the better run, negative when
        /// <paramref name="b"/> is, zero when they are the same run. </summary>
        public static int Compare(BestRun a, BestRun b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (a.Frame != b.Frame) return a.Frame > b.Frame ? 1 : -1;
            if (a.Hits != b.Hits) return a.Hits < b.Hits ? 1 : -1;
            if (a.LivesLeft != b.LivesLeft) return a.LivesLeft > b.LivesLeft ? 1 : -1;
            if (a.Dashes != b.Dashes) return a.Dashes < b.Dashes ? 1 : -1;
            if (a.TimeUtc != b.TimeUtc) return a.TimeUtc < b.TimeUtc ? 1 : -1;
            return 0;
        }
    }
}

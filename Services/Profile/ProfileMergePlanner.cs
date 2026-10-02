using System;
using System.Collections.Generic;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Profile;

namespace BH.SDK.Services.Profile
{
    /// <summary> One incoming level a merge places, and the folder it lands in. </summary>
    public readonly struct ProfileLevelMove
    {
        public ProfileLevelMove(ProfileLevelEntry incoming, string targetFolder, string replacedFolder)
        {
            Incoming = incoming;
            TargetFolder = targetFolder;
            ReplacedFolder = replacedFolder;
        }

        /// <summary> The level as the archive describes it. Its <c>Folder</c> is where it sits in the archive. </summary>
        public ProfileLevelEntry Incoming { get; }

        /// <summary> The folder under <c>levels/</c> it is written to. </summary>
        public string TargetFolder { get; }

        /// <summary> The local folder it replaces, or null when it is new. </summary>
        public string ReplacedFolder { get; }
    }

    /// <summary> What a level merge will do, decided before anything is touched. </summary>
    public sealed class ProfileLevelPlan
    {
        /// <summary> Levels the profile does not have. </summary>
        public List<ProfileLevelMove> Added { get; } = new List<ProfileLevelMove>();

        /// <summary> Levels whose incoming copy is newer - the local one is replaced whole. </summary>
        public List<ProfileLevelMove> Replaced { get; } = new List<ProfileLevelMove>();

        /// <summary> Incoming levels skipped because the local copy is as new or newer. </summary>
        public List<ProfileLevelEntry> KeptLocal { get; } = new List<ProfileLevelEntry>();
    }

    // A LEVEL IS MATCHED BY ITS LevelId, NEVER BY ITS FOLDER. A folder is a name a player can change and
    // two players can share; the id is what LevelMeta declares scores and progress attach to. So the
    // same level under two folder names is one level, and two different levels under one folder name
    // are two - the second is placed under its own id instead of overwriting the first.
    //
    // THE WINNER IS WHOLE. A level's documents and media refer to each other, so mixing a newer
    // level.json with an older media folder would produce a level neither side ever had. A tie keeps
    // the local copy: the merge only replaces what it can show is newer.
    //
    // Folder names compare case-insensitively, because the file systems a profile lands on mostly do.

    /// <summary> Decides, purely, what merging an archive's levels into the local ones does. </summary>
    public static class ProfileMergePlanner
    {
        private const char Separator = '-';

        /// <summary> Plans a level merge. <paramref name="local"/> describes the profile's own levels the
        /// same way a manifest describes the archive's. </summary>
        public static ProfileLevelPlan PlanLevels(IReadOnlyList<ProfileLevelEntry> incoming,
            IReadOnlyList<ProfileLevelEntry> local)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (local == null) throw new ArgumentNullException(nameof(local));

            var plan = new ProfileLevelPlan();
            var byId = new Dictionary<LevelId, ProfileLevelEntry>();
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in local)
            {
                if (entry == null) continue;
                taken.Add(entry.Folder);
                if (entry.LevelId != LevelId.Null && !byId.ContainsKey(entry.LevelId)) byId[entry.LevelId] = entry;
            }

            foreach (var entry in incoming)
            {
                if (entry == null) continue;

                if (entry.LevelId != LevelId.Null && byId.TryGetValue(entry.LevelId, out var existing))
                {
                    if (entry.ModifiedUtc > existing.ModifiedUtc)
                        plan.Replaced.Add(new ProfileLevelMove(entry, existing.Folder, existing.Folder));
                    else
                        plan.KeptLocal.Add(entry);
                    continue;
                }

                var folder = FreeFolder(entry, taken);
                taken.Add(folder);
                if (entry.LevelId != LevelId.Null) byId[entry.LevelId] = new ProfileLevelEntry(entry.LevelId, folder, entry.ModifiedUtc);

                plan.Added.Add(new ProfileLevelMove(entry, folder, null));
            }

            return plan;
        }

        /// <summary> The folder name a level placed under its own id gets. </summary>
        public static string FolderOf(LevelId id) => id.value.ToString("N");

        private static string FreeFolder(ProfileLevelEntry entry, HashSet<string> taken)
        {
            if (!taken.Contains(entry.Folder)) return entry.Folder;

            var stem = entry.LevelId != LevelId.Null ? FolderOf(entry.LevelId) : entry.Folder;
            if (!taken.Contains(stem)) return stem;

            for (var n = 2; ; n++)
            {
                var candidate = stem + Separator + n;
                if (!taken.Contains(candidate)) return candidate;
            }
        }
    }
}

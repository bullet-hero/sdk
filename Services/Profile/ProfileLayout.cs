using System;
using BH.SDK.Models;
using BH.SDK.Models.Profile;

namespace BH.SDK.Services.Profile
{
    // THE ARCHIVE MIRRORS THE PROFILE ROOT, and this is the whole of the mapping. A category is one
    // folder (or, for settings, one file) directly under the root, named by FileNames, so an archive
    // entry's category is its first path segment - nothing has to be renamed on the way in or out.
    //
    // IT IS ALSO THE WHITELIST, read the same way on pack and on unpack: a path that maps to no
    // category is not part of a profile and is ignored, whoever put it in the zip. That is what keeps
    // profile-backups/ (the one backup and every temporary of an import) out of every archive without
    // a special case anywhere.

    /// <summary> Which profile category owns which path, and the order a profile is packed in. </summary>
    public static class ProfileLayout
    {
        private const char Separator = '/';

        /// <summary> Settings first (small, read whole), then statistics, then everything large. </summary>
        public static readonly ProfileCategory[] PackOrder =
        {
            ProfileCategory.Settings,
            ProfileCategory.Statistics,
            ProfileCategory.Levels,
            ProfileCategory.Library,
            ProfileCategory.Backups,
            ProfileCategory.Reports,
        };

        /// <summary> Whether a path is the manifest. </summary>
        public static bool IsManifest(string path) =>
            string.Equals(path, FileNames.ProfileManifestFileName, StringComparison.Ordinal);

        /// <summary> Whether a category is a single file rather than a folder. </summary>
        public static bool IsFile(ProfileCategory category) => category == ProfileCategory.Settings;

        /// <summary> The folder a category owns under the profile root, or the file name for a
        /// file category. </summary>
        public static string RootOf(ProfileCategory category)
        {
            switch (category)
            {
                case ProfileCategory.Levels: return FileNames.LevelDirectory;
                case ProfileCategory.Statistics: return FileNames.StatisticsDirectory;
                case ProfileCategory.Settings: return FileNames.SettingsFileName;
                case ProfileCategory.Library: return FileNames.ResourcesDirectory;
                case ProfileCategory.Backups: return FileNames.BackupsDirectory;
                case ProfileCategory.Reports: return FileNames.ReportsDirectory;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, "Not a single category.");
            }
        }

        /// <summary> The archive path of a file inside a folder category. </summary>
        public static string Combine(ProfileCategory category, string relative) =>
            IsFile(category) ? RootOf(category) : RootOf(category) + Separator + relative;

        /// <summary> The category an archive path belongs to, or <see cref="ProfileCategory.None"/> when
        /// it is not part of a profile. </summary>
        public static ProfileCategory CategoryOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return ProfileCategory.None;

            foreach (var category in PackOrder)
            {
                var root = RootOf(category);

                if (IsFile(category))
                {
                    if (string.Equals(path, root, StringComparison.Ordinal)) return category;
                    continue;
                }

                if (path.Length > root.Length + 1 && path[root.Length] == Separator &&
                    path.StartsWith(root, StringComparison.Ordinal))
                    return category;
            }

            return ProfileCategory.None;
        }

        /// <summary> The path relative to its category's folder; the file name for a file category. </summary>
        public static string RelativeOf(string path, ProfileCategory category) =>
            IsFile(category) ? path : path.Substring(RootOf(category).Length + 1);
    }
}

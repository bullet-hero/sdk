using System;

namespace BH.SDK.Models.Profile
{
    /// <summary> Which parts of a player's profile an archive carries - one flag per folder (or file)
    /// under the profile root. </summary>
    [Flags]
    public enum ProfileCategory
    {
        None = 0,
        /// <summary> <c>levels/</c> - the player's own levels. </summary>
        Levels = 1 << 0,
        /// <summary> <c>stats/</c> - global and per-level statistics. </summary>
        Statistics = 1 << 1,
        /// <summary> <c>settings.json</c>. </summary>
        Settings = 1 << 2,
        /// <summary> <c>resources/</c> - the device library and its collections. </summary>
        Library = 1 << 3,
        /// <summary> <c>backups/</c> - the level editor's autosaves. </summary>
        Backups = 1 << 4,
        /// <summary> <c>reports/</c> - error reports. </summary>
        Reports = 1 << 5,
        /// <summary> <c>recordings/</c> - videos the level editor exported. </summary>
        Recordings = 1 << 6,

        /// <summary> Every category. </summary>
        All = Levels | Statistics | Settings | Library | Backups | Reports | Recordings,
    }
}

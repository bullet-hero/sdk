using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Achievements
{
    // A new domain, not a bump: no existing model refers to it. Only WHEN, never whether - an achievement is
    // derived from statistics by the game, so an id added by hand unlocks nothing. Docs/STATISTICS.md

    /// <summary> When each achievement unlocked (<c>stats/achievements.json</c>). An id the reading build
    /// does not know is kept, never dropped. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.AchievementsRecord, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class AchievementsRecord : IModel<AchievementsRecord>
    {
        /// <summary> Every unlock, at most one per id. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Unlocked)]
        public List<AchievementUnlock> Unlocked { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AchievementsRecord()
        {
            Unlocked = new List<AchievementUnlock>();
        }

        /// <summary> Whether <paramref name="id"/> is unlocked. </summary>
        public bool IsUnlocked(string id) => Find(id) != null;

        /// <summary> The unlock of <paramref name="id"/>, or null. </summary>
        public AchievementUnlock Find(string id)
        {
            foreach (var unlock in Unlocked)
                if (unlock != null && string.Equals(unlock.Id, id, StringComparison.Ordinal))
                    return unlock;
            return null;
        }

        /// <summary> Adds an unlock unless the id already has one. Returns whether it was added. </summary>
        public bool Add(string id, DateTime unlockedUtc)
        {
            if (string.IsNullOrEmpty(id) || IsUnlocked(id)) return false;

            Unlocked.Add(new AchievementUnlock(id, unlockedUtc));
            return true;
        }
    }
}

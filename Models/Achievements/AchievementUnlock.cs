using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Models.Achievements
{
    /// <summary> One unlocked achievement: which, and when. </summary>
    [GenerateModel]
    public sealed partial class AchievementUnlock : IModel<AchievementUnlock>
    {
        /// <summary> The achievement's stable id, as the game's catalog spells it. </summary>
        [JsonProperty(Names.Id)]
        public string Id { get; set; }

        /// <summary> When it unlocked, UTC. </summary>
        [JsonProperty(Names.UnlockedUtc)]
        public DateTime UnlockedUtc { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AchievementUnlock()
        {
            Id = string.Empty;
            UnlockedUtc = default;
        }

        /// <summary> Built from its two values. </summary>
        public AchievementUnlock(string id, DateTime unlockedUtc)
        {
            Id = id;
            UnlockedUtc = unlockedUtc;
        }
    }
}

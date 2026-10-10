using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    /// <summary>
    /// Device-wide options that fit no other group - today, how aggressively a level's resources are
    /// fetched. Language is planned but not stored yet
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GeneralSettings : IModel<GeneralSettings>, IMoveable<GeneralSettings>
    {
        // About discovery, not one store: off by default (the narrow answer is the honest one), and a
        // source with one answer ignores it. Docs/SETTING_GROUPS.md

        /// <summary> Whether sources also list content they find but cannot vouch for. Off means a
        /// source lists only what it can confirm belongs to the player. </summary>
        [JsonProperty(Names.ShowAllFoundContent)]
        public bool ShowAllFoundContent { get; set; }

        /// <summary> How many resources download/load at once. Higher is faster on a good
        /// connection, worse on a phone with a weak one. </summary>
        [RuleInRange(1, 8)]
        [JsonProperty(Names.ResourceParallelLoadCount)]
        public int ResourceParallelLoadCount { get; set; }

        /// <summary> Seconds before a remote resource fetch is given up on and its next fallback
        /// source is tried. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.ResourceWebTimeout)]
        public float ResourceWebTimeout { get; set; }

        // Empty means "follow the device", so no RuleStringPattern: its Fix would rewrite empty to "en".

        /// <summary> Which language localized level text is read in. Empty follows the device. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxLanguageCode)]
        [JsonProperty(Names.Language)]
        public string Language { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public GeneralSettings()
        {
            ShowAllFoundContent = false;
            ResourceParallelLoadCount = 2;
            ResourceWebTimeout = 5f;
            Language = string.Empty;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public GeneralSettings(bool showAllFoundContent, int resourceParallelLoadCount,
            float resourceWebTimeout, string language)
        {
            ShowAllFoundContent = showAllFoundContent;
            ResourceParallelLoadCount = resourceParallelLoadCount;
            ResourceWebTimeout = resourceWebTimeout;
            Language = language;
        }
    }
}
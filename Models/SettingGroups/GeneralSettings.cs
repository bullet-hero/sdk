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
    /// fetched. Language is planned but not stored yet (see the TODO below).
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GeneralSettings : IModel<GeneralSettings>, IMoveable<GeneralSettings>
    {
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
        
        // Empty means "follow the device", and that is why this carries no RuleStringPattern despite
        // being a BCP-47 code: the pattern demands at least two letters, and its Fix would rewrite an
        // empty value to "en" - silently taking away the only way a player has of saying "whatever
        // this machine is set to". A save file carried to another machine should follow that machine.

        /// <summary> Which language localized level text is read in. Empty follows the device. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxLanguageCode)]
        [JsonProperty(Names.Language)]
        public string Language { get; set; }

        public GeneralSettings()
        {
            ResourceParallelLoadCount = 2;
            ResourceWebTimeout = 5f;
            Language = string.Empty;
        }
        public GeneralSettings(int resourceParallelLoadCount, float resourceWebTimeout, string language)
        {
            ResourceParallelLoadCount = resourceParallelLoadCount;
            ResourceWebTimeout = resourceWebTimeout;
            Language = language;
        }
    }
}
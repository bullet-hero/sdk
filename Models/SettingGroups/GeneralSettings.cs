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
        // A SETTING ABOUT DISCOVERY, NOT ABOUT ONE STORE. Every place levels come from can be asked
        // two different questions: "what does this source say belongs to this player" and "what is
        // actually lying there". The two disagree in ordinary ways - a workshop item unsubscribed
        // from whose files remain, content copied in by hand, a download that never finished - and
        // which answer a player wants is a preference rather than a fact about any one platform.
        //
        // IT IS OFF BY DEFAULT because the honest answer is the narrow one: a level a source does not
        // claim may be stale, half-written or something the player already chose to remove, and a
        // browser that lists it without being asked is inventing content. Turning it on is the
        // player saying they would rather see everything and judge for themselves.
        //
        // NOT EVERY SOURCE IMPLEMENTS IT, and that is expected rather than a gap to fill: a source
        // with no second way of looking - the player's own folder, where the listing IS the disk -
        // has one answer to give and ignores this entirely. Today the Steam workshop is the only
        // source for which the two questions differ.

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
            ShowAllFoundContent = false;
            ResourceParallelLoadCount = 2;
            ResourceWebTimeout = 5f;
            Language = string.Empty;
        }

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
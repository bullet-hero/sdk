using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    /// <summary>
    /// How the editor publishes a level to a storefront - which language a Workshop item's title and
    /// description are taken in first.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorPublishingSettings : IModel<EditorPublishingSettings>,
        IMoveable<EditorPublishingSettings>
    {
        /// <summary> Which language a Workshop item's title and description are taken in first. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Language)]
        public PublishLanguage Language { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorPublishingSettings()
        {
            ResetOwn();
        }
        /// <summary> Built from its language. </summary>
        public EditorPublishingSettings(PublishLanguage language)
        {
            Language = language;
        }
        private void ResetOwn()
        {
            Language = PublishLanguage.English;
        }
    }
}

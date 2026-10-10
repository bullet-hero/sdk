using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Whether effects are on NOW is session state; this is what a session STARTS with. True in the model,
    // seeded false in a settings.json born on a phone (RootEntryPoint). Docs/SETTING_GROUPS.md

    /// <summary>
    /// The editor's preview of a level's post-processing effects.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorEffectsSettings : IModel<EditorEffectsSettings>, IMoveable<EditorEffectsSettings>
    {
        /// <summary> Whether the editor's post-processing preview starts switched on. </summary>
        [JsonProperty(Names.ActiveDefault)]
        public bool ActiveDefault { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorEffectsSettings()
        {
            ResetOwn();
        }
        /// <summary> Built from its default. </summary>
        public EditorEffectsSettings(bool activeDefault)
        {
            ActiveDefault = activeDefault;
        }
        private void ResetOwn()
        {
            ActiveDefault = true;
        }
    }
}

using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // WHETHER THE EFFECTS ARE ON RIGHT NOW is not here, and that is the same split the viewport grid
    // and the preview player already make: the current view is session state (Services.GameEditor's
    // EditorPostProcessingService, which forwards to LevelPlayer.PostProcessingActive), while what a
    // SESSION STARTS with describes how the author works and is remembered.
    //
    // TRUE in the model and per-platform in a fresh settings file, which is the one place this group
    // differs from its two neighbours. The model's default is what an author who never had the
    // setting gets - the behaviour the editor already had, so the field is additive and no
    // DataVersion moved - while a settings.json BORN on a phone is seeded false
    // (Services.Root's RootEntryPoint, beside the controls seeding it already does): post-processing
    // is the single most expensive thing the editor draws, and the one screen a phone runs it on is
    // also the one where the author is scrubbing rather than watching.
    //
    // The seed and the default disagree ON PURPOSE and it is not a contradiction: a default answers
    // "what did this field mean before it existed", a seed answers "what does this device want".

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

        public EditorEffectsSettings()
        {
            ResetOwn();
        }
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

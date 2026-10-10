using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Controls;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    // Audio sits second, right after General, and the settings screen's tabs follow the same order.

    /// <summary>
    /// The player's own options, saved once per device (settings.json), the third top-level file beside a
    /// level and its metadata. Nothing here travels with a level.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.UserSettings, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class UserSettings : IModel<UserSettings>, IMoveable<UserSettings>
    {
        /// <summary> Options fitting no other group (resource loading, later language). </summary>
        [RuleNotNull]
        [JsonProperty(Names.General)]
        public GeneralSettings General { get; set; }

        /// <summary> Volume mix. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Audio)]
        public AudioSettings Audio { get; set; }

        /// <summary> Which devices drive the avatar, in what mode, with what tuning. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Controls)]
        public ControlsSettings Controls { get; set; }

        /// <summary> Rendering quality and per-subsystem switches. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Graphics)]
        public GraphicsSettings Graphics { get; set; }

        // The editor's interface group.

        /// <summary> Interface overlays the game draws over every screen. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Interface)]
        public InterfaceSettings Interface { get; set; }

        /// <summary> In-game level editor preferences. Present even for players who never open the
        /// editor - the file has a fixed shape. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GameEditor)]
        public GameEditorSettings GameEditor { get; set; }

        // Names.Keys is shared with CurveValue's keyframe list: the two can never co-occur on one model.

        /// <summary> Which key runs which command, as overrides on the shipped defaults. Empty for a
        /// player who never rebound anything, which is most of them. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Keys)]
        public KeybindingsSettings Keybindings { get; set; }

        // The first member added after release (generation Release is frozen in Versions/V1/UserSettingsV1).
        // It flips on DECLINING the tutorial offer as much as on finishing it.

        /// <summary> Whether the tutorial was already offered - finished or declined. </summary>
        [JsonProperty(Names.TutorialCompleted)]
        public bool TutorialCompleted { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public UserSettings()
        {
            General = new GeneralSettings();
            Audio = new AudioSettings();
            Controls = new ControlsSettings();
            Graphics = new GraphicsSettings();
            GameEditor = new GameEditorSettings();
            Interface = new InterfaceSettings();
            Keybindings = new KeybindingsSettings();
            TutorialCompleted = false;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public UserSettings(GeneralSettings general, AudioSettings audio,
            ControlsSettings controls, GraphicsSettings graphics, GameEditorSettings gameEditor,
            InterfaceSettings interfaceSettings, KeybindingsSettings keybindings, bool tutorialCompleted)
        {
            General = general;
            Audio = audio;
            Controls = controls;
            Graphics = graphics;
            GameEditor = gameEditor;
            Interface = interfaceSettings;
            Keybindings = keybindings;
            TutorialCompleted = tutorialCompleted;
        }
    }
}
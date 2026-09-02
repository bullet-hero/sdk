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
    // TODO Add tests for IResetable

    /// <summary>
    /// The player's own options, saved once per device (settings.json) - the third top-level file
    /// next to a level and its metadata. Nothing here travels with a level: the same level must
    /// play the same way regardless of these.
    /// </summary>
    // (2,0) because GameEditorSettings was RESTRUCTURED - sixteen flat properties became nine nested
    // groups. Every earlier change to this file was additive and rode on Newtonsoft leaving a
    // constructor's default in place for an absent key; moving keys is the one thing that cannot,
    // so this is the domain's first snapshot-and-migrator pair (Versions/V1_0/).
    [RuleContainer]
    [DataVersion(DataDomains.UserSettings, 2, 0)]
    [GenerateModel]
    public sealed partial class UserSettings : IModel<UserSettings>, IMoveable<UserSettings>
    {
        /// <summary> Options fitting no other group (resource loading, later language). </summary>
        [RuleNotNull]
        [JsonProperty(Names.General)]
        public GeneralSettings General { get; set; }

        /// <summary> Which devices drive the avatar, in what mode, with what tuning. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Controls)]
        public ControlsSettings Controls { get; set; }

        /// <summary> Volume mix. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Audio)]
        public AudioSettings Audio { get; set; }

        /// <summary> Rendering quality and per-subsystem switches. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Graphics)]
        public GraphicsSettings Graphics { get; set; }

        // Added after the domain was already at 1.0 and deliberately does NOT bump it: a settings.json
        // written before this group existed simply has no "iface" key, and Newtonsoft leaves the
        // constructor's defaults in place. That is the same call the Controls group's removed property
        // made - an additive property with a default needs no snapshot and no migrator.

        /// <summary> Interface overlays the game draws over every screen. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Interface)]
        public InterfaceSettings Interface { get; set; }

        /// <summary> In-game level editor preferences. Present even for players who never open the
        /// editor - the file has a fixed shape. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GameEditor)]
        public GameEditorSettings GameEditor { get; set; }

        // Additive like the Interface group above, and for the same reason: an older settings.json
        // has no "keys" key and Newtonsoft leaves the constructor's empty map in place, so no
        // snapshot and no migrator.
        //
        // The key is Names.Keys, which CurveValue also uses for its keyframe list. That reuse is
        // what the file's own header allows - "a key is only ever reused across models that can
        // never co-occur" - and a curve lives inside a level while this lives inside settings.json,
        // so the two can never appear on one model.

        /// <summary> Which key runs which command, as overrides on the shipped defaults. Empty for a
        /// player who never rebound anything, which is most of them. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Keys)]
        public KeybindingsSettings Keybindings { get; set; }

        public UserSettings()
        {
            General = new GeneralSettings();
            Controls = new ControlsSettings();
            Audio = new AudioSettings();
            Graphics = new GraphicsSettings();
            GameEditor = new GameEditorSettings();
            Interface = new InterfaceSettings();
            Keybindings = new KeybindingsSettings();
        }

        public UserSettings(GeneralSettings general, ControlsSettings controls,
            AudioSettings audio, GraphicsSettings graphics, GameEditorSettings gameEditor,
            InterfaceSettings interfaceSettings, KeybindingsSettings keybindings)
        {
            General = general;
            Controls = controls;
            Audio = audio;
            Graphics = graphics;
            GameEditor = gameEditor;
            Interface = interfaceSettings;
            Keybindings = keybindings;
        }
    }
}
using System;
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
    [RuleContainer]
    [DataVersion(DataDomains.UserSettings, 1, 0)]
    public class UserSettings : IModel<UserSettings>, IMoveable<UserSettings>
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

        public UserSettings()
        {
            General = new GeneralSettings();
            Controls = new ControlsSettings();
            Audio = new AudioSettings();
            Graphics = new GraphicsSettings();
            GameEditor = new GameEditorSettings();
            Interface = new InterfaceSettings();
        }
        public UserSettings(GeneralSettings general, ControlsSettings controls,
            AudioSettings audio, GraphicsSettings graphics, GameEditorSettings gameEditor,
            InterfaceSettings interfaceSettings)
        {
            General = general;
            Controls = controls;
            Audio = audio;
            Graphics = graphics;
            GameEditor = gameEditor;
            Interface = interfaceSettings;
        }
        public void Reset()
        {
            General.Reset();
            Controls.Reset();
            Audio.Reset();
            Graphics.Reset();
            GameEditor.Reset();
            Interface.Reset();
        }

        public object Clone() => Copy();
        public UserSettings Copy() => new(General.Copy(), Controls.Copy(),
            Audio.Copy(), Graphics.Copy(), GameEditor.Copy(), Interface.Copy());
        
        public void Pull(UserSettings source)
        {
            General.Pull(source.General);
            Controls.Pull(source.Controls);
            Audio.Pull(source.Audio);
            Graphics.Pull(source.Graphics);
            GameEditor.Pull(source.GameEditor);
            Interface.Pull(source.Interface);
        }

        public bool Equals(UserSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return General.Equals(other.General)
                   && Controls.Equals(other.Controls)
                   && Audio.Equals(other.Audio)
                   && Graphics.Equals(other.Graphics)
                   && GameEditor.Equals(other.GameEditor)
                   && Interface.Equals(other.Interface);
        }
        
        public override bool Equals(object obj) => obj is UserSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(General, Controls, Audio, Graphics, GameEditor, Interface);
    }
}
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // The groups are typed as TODAY's classes wherever they did not change at this bump - they are not
    // domains of their own, so a group that changes freezes its own leaf snapshot next to this one (no
    // [ModelGeneration], like AudioLevelV0) and this file is retyped to it. Controls is the one that
    // did: ControlsSettingsV1 still carries cursor_return. Keys are literal: a snapshot spells them
    // the way its own generation did.

    /// <summary> Generation 1 (Release) of the user-settings domain - the shape 1.0.0 shipped, before
    /// the tutorial flag and with the cursor-return switch. A frozen snapshot - never edit it to match today's shape. </summary>
    [ModelGeneration(ModelDomains.UserSettings, ModelGenerations.Release)]
    [GenerateModel]
    public sealed partial class UserSettingsV1 : IModel<UserSettingsV1>
    {
        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("general")]
        public GeneralSettings General { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("audio")]
        public AudioSettings Audio { get; set; }

        /// <summary> Changed at this bump: lost <c>cursor_return</c>. </summary>
        [JsonProperty("controls")]
        public ControlsSettingsV1 Controls { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("graphics")]
        public GraphicsSettings Graphics { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("iface")]
        public InterfaceSettings Interface { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("game_editor")]
        public GameEditorSettings GameEditor { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("keys")]
        public KeybindingsSettings Keybindings { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public UserSettingsV1()
        {
            General = new GeneralSettings();
            Audio = new AudioSettings();
            Controls = new ControlsSettingsV1();
            Graphics = new GraphicsSettings();
            GameEditor = new GameEditorSettings();
            Interface = new InterfaceSettings();
            Keybindings = new KeybindingsSettings();
        }
    }
}

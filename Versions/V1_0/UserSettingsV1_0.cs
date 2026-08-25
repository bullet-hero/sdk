using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Controls;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1_0
{
    // ReSharper disable once InconsistentNaming

    // The first snapshot this domain has ever needed. Only ONE of the seven groups actually changed
    // shape at this generation, so only that one has a VX_Y class of its own; the other six are typed
    // with their CURRENT classes, which is not laziness but the rule the Versions README states - a
    // frozen snapshot only freezes what moved, and re-freezing an unchanged group would mean
    // maintaining a second copy of it forever.
    //
    // The six unchanged groups carry no [DataVersion] either, so Newtonsoft reads them as plain
    // nested objects with no envelope to unwrap, and an absent key still leaves the constructor's
    // default in place - which is what keeps every additive change that shipped before this one
    // readable through this snapshot too.

    [DataVersion(DataDomains.UserSettings, 1, 0)]
    public class UserSettingsV1_0
    {
        [JsonProperty("general")]
        public GeneralSettings General { get; set; }

        [JsonProperty("controls")]
        public ControlsSettings Controls { get; set; }

        [JsonProperty("audio")]
        public AudioSettings Audio { get; set; }

        [JsonProperty("graphics")]
        public GraphicsSettings Graphics { get; set; }

        [JsonProperty("iface")]
        public InterfaceSettings Interface { get; set; }

        [JsonProperty("game_editor")]
        public GameEditorSettingsV1_0 GameEditor { get; set; }

        [JsonProperty("keys")]
        public KeybindingsSettings Keybindings { get; set; }
    }
}

using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.Controls;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of UserSettingsV1. Common, Gamepad and
    // DeviceGyro changed at generation 2, so those are retyped to snapshots; the other device groups
    // are TODAY's classes, and one that changes later freezes its own leaf next to this file.

    /// <summary> The controls group as generation 1 (Release) of the user-settings domain wrote it.
    /// A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class ControlsSettingsV1 : IModel<ControlsSettingsV1>
    {
        /// <summary> Changed at generation 2: lost <c>cursor_return</c>, <c>selection</c> and
        /// <c>manual_device</c>. </summary>
        [JsonProperty("common")]
        public CommonControlsSettingsV1 Common { get; set; }

        /// <summary> Gone at generation 2: the platform's own order decides the first frame. </summary>
        [JsonProperty("priority")]
        public ControlDevice[] Priority { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("keyboard_mouse")]
        public KeyboardMouseControlsSettings KeyboardMouse { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("touchscreen")]
        public TouchscreenControlsSettings Touchscreen { get; set; }

        /// <summary> Changed at generation 2: lost <c>dash_buttons</c>. </summary>
        [JsonProperty("gamepad")]
        public GamepadControlsSettingsV1 Gamepad { get; set; }

        /// <summary> Changed at generation 2: lost every form of locality. </summary>
        [JsonProperty("device_gyro")]
        public DeviceGyroControlsSettingsV1 DeviceGyro { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public ControlsSettingsV1()
        {
            Common = new CommonControlsSettingsV1();
            Priority = new[]
            {
                ControlDevice.KeyboardMouse, ControlDevice.Touchscreen,
                ControlDevice.Gamepad, ControlDevice.DeviceGyro,
            };
            KeyboardMouse = new KeyboardMouseControlsSettings();
            Touchscreen = new TouchscreenControlsSettings();
            Gamepad = new GamepadControlsSettingsV1();
            DeviceGyro = new DeviceGyroControlsSettingsV1();
        }
    }
}

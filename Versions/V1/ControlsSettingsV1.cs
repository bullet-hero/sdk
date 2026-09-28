using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.Controls;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of UserSettingsV1. Every group changed
    // at generation 2, so every one is retyped to a snapshot of its own next to this file.

    /// <summary> The controls group as generation 1 (Release) of the user-settings domain wrote it.
    /// A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class ControlsSettingsV1 : IModel<ControlsSettingsV1>
    {
        /// <summary> Changed at generation 2: lost <c>cursor_return</c> (moved into the pointer groups),
        /// <c>selection</c> and <c>manual_device</c>. </summary>
        [JsonProperty("common")]
        public CommonControlsSettingsV1 Common { get; set; }

        /// <summary> Gone at generation 2: the platform's own order decides the first frame. </summary>
        [JsonProperty("priority")]
        public ControlDevice[] Priority { get; set; }

        /// <summary> Changed at generation 2: gained <c>cursor_return</c>. </summary>
        [JsonProperty("keyboard_mouse")]
        public KeyboardMouseControlsSettingsV1 KeyboardMouse { get; set; }

        /// <summary> Changed at generation 2: gained <c>cursor_return</c>. </summary>
        [JsonProperty("touchscreen")]
        public TouchscreenControlsSettingsV1 Touchscreen { get; set; }

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
            KeyboardMouse = new KeyboardMouseControlsSettingsV1();
            Touchscreen = new TouchscreenControlsSettingsV1();
            Gamepad = new GamepadControlsSettingsV1();
            DeviceGyro = new DeviceGyroControlsSettingsV1();
        }
    }
}

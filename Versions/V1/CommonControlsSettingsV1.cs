using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of UserSettingsV1, frozen because
    // generation 2 moved CursorReturn into the pointer groups and removed Selection and ManualDevice.

    /// <summary> The shared cursor/selection settings as generation 1 (Release) of the user-settings
    /// domain wrote them - with <c>cursor_return</c>, which generation 2 moved out. A frozen snapshot -
    /// never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class CommonControlsSettingsV1 : IModel<CommonControlsSettingsV1>
    {
        /// <summary> Auto follows the most recently used device; Manual pins one. </summary>
        [JsonProperty("selection")]
        public DeviceSelectionV1 Selection { get; set; }

        /// <summary> The pinned device. </summary>
        [JsonProperty("manual_device")]
        public ControlDevice ManualDevice { get; set; }

        /// <summary> Whether the in-world cursor is drawn. </summary>
        [JsonProperty("cursor_visible")]
        public bool CursorVisible { get; set; }

        /// <summary> How large the inframe cursor is drawn. </summary>
        [JsonProperty("cursor_sca")]
        public float CursorScale { get; set; }

        /// <summary> The cursor is born on the avatar rather than where it was left. </summary>
        [JsonProperty("cursor_recenter")]
        public bool CursorRecenter { get; set; }

        /// <summary> The cursor snapped back onto the avatar when steering stopped. Moved into the
        /// mouse and touchscreen groups at generation 2. </summary>
        [JsonProperty("cursor_return")]
        public bool CursorReturn { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public CommonControlsSettingsV1()
        {
            Selection = DeviceSelectionV1.Auto;
            ManualDevice = ControlDevice.KeyboardMouse;
            CursorVisible = true;
            CursorScale = 1f;
            CursorRecenter = true;
            CursorReturn = false;
        }
    }
}

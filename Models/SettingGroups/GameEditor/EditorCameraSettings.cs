using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The zoom limits used to live here while the pan and zoom TUNING lived in a project asset
    // (Services.GameEditor's CameraDragViewportSettings), which meant half of one gesture was the
    // author's and half was the project's - and the half that was the author's was read by nothing,
    // so both sliders in the settings screen moved a number the camera never consulted. The whole
    // gesture is here now. See docs/issues/EDITOR_SETTINGS_HISTORY.md.
    //
    // What stayed in the asset is what is not a preference at all: where the camera STARTS
    // (DefaultPosition/DefaultSize, the zoom every on-screen size in the editor is stated at) and
    // the epsilon below which two fingers count as not having pinched.

    /// <summary>
    /// How the editor's viewport camera pans and zooms, and how far it may zoom either way.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(MinSize), nameof(MaxSize))]
    [GenerateModel]
    public sealed partial class EditorCameraSettings : IModel<EditorCameraSettings>, IMoveable<EditorCameraSettings>
    {
        /// <summary> Closest the editor camera may zoom in. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.MinSize)]
        public float MinSize { get; set; }

        /// <summary> Furthest the editor camera may zoom out. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.MaxSize)]
        public float MaxSize { get; set; }

        // On by default, which is "drag the CONTENT" rather than "drag the camera" - the direction a
        // finger expects and the one every map and image viewer uses. The opposite is what a camera
        // operator expects, and both camps are certain the other is backwards, which is exactly what
        // makes this a setting rather than a decision.

        /// <summary> Whether a pan drag moves the content with the pointer instead of the camera. </summary>
        [JsonProperty(Names.Invert)]
        public bool Invert { get; set; }

        /// <summary> Horizontal pan speed, as a multiplier on the pointer's own travel. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.MoveSensitivityX)]
        public float MoveSensitivityX { get; set; }

        /// <summary> Vertical pan speed, as a multiplier on the pointer's own travel. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.MoveSensitivityY)]
        public float MoveSensitivityY { get; set; }

        /// <summary> How much one wheel notch zooms. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.WheelMultiplier)]
        public float WheelMultiplier { get; set; }

        /// <summary> Whether the wheel zooms towards the pointer instead of the viewport centre. </summary>
        [JsonProperty(Names.ZoomToMouse)]
        public bool ZoomToMouse { get; set; }

        public EditorCameraSettings()
        {
            ResetOwn();
        }
        public EditorCameraSettings(float minSize, float maxSize, bool invert, float moveSensitivityX,
            float moveSensitivityY, float wheelMultiplier, bool zoomToMouse)
        {
            MinSize = minSize;
            MaxSize = maxSize;
            Invert = invert;
            MoveSensitivityX = moveSensitivityX;
            MoveSensitivityY = moveSensitivityY;
            WheelMultiplier = wheelMultiplier;
            ZoomToMouse = zoomToMouse;
        }
        private void ResetOwn()
        {
            MinSize = 0.1f;
            MaxSize = 100f;
            Invert = true;
            MoveSensitivityX = 1f;
            MoveSensitivityY = 1f;
            WheelMultiplier = 0.1f;
            ZoomToMouse = true;
        }
    }
}

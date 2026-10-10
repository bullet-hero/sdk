using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The whole pan/zoom gesture is a preference; the project asset keeps what is not (start position and
    // size, the pinch epsilon).

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

        // On: drag the CONTENT, the direction a finger expects; both camps think the other backwards.

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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorCameraSettings()
        {
            ResetOwn();
        }
        /// <summary> Every member at once, in declaration order. </summary>
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
            MaxSize = 1000f;
            Invert = true;
            MoveSensitivityX = 1f;
            MoveSensitivityY = 1f;
            WheelMultiplier = 0.1f;
            ZoomToMouse = true;
        }
    }
}

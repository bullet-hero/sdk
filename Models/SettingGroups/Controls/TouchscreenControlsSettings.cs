using System;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Controls
{
    // The Direction-only block at the bottom is the on-screen layout, and it is stored here rather than
    // in a UI settings group on purpose: it only exists because THIS device is in Direction mode, and a
    // player who never touches a touchscreen never sees it.
    //
    // Sizes are fractions of screen height and anchors are the nine reach positions, both so a layout
    // authored on one phone lands in the same place on another. Handedness mirrors the whole set at
    // once - mirroring elements one by one is how a left-handed layout ends up half right-handed.

    /// <summary>
    /// The device's own touchscreen: drag-to-move by default, with an on-screen stick in Direction
    /// mode.
    /// </summary>
    [RuleContainer]
    public class TouchscreenControlsSettings : BaseDeviceControlsSettings,
        IModel<TouchscreenControlsSettings>, IMoveable<TouchscreenControlsSettings>
    {
        [RuleEnumValid(TouchscreenControlMode.Relative)]
        [JsonProperty(Names.Mode)]
        public TouchscreenControlMode Mode { get; set; }

        /// <summary> Absolute mode: how far the cursor sits from the finger, as a fraction of camera
        /// height, so a thumb does not cover the avatar. </summary>
        [RuleInRange(ControlsRules.MinFingerOffset, ControlsRules.MaxFingerOffset)]
        [JsonProperty(Names.FingerOffsetX)]
        public float FingerOffsetX { get; set; }

        [RuleInRange(ControlsRules.MinFingerOffset, ControlsRules.MaxFingerOffset)]
        [JsonProperty(Names.FingerOffsetY)]
        public float FingerOffsetY { get; set; }

        /// <summary> A second finger anywhere on screen dashes - needs no on-screen button and never
        /// covers the play area. </summary>
        [JsonProperty(Names.DashOnSecondFinger)]
        public bool DashOnSecondFinger { get; set; }

        [JsonProperty(Names.DashOnDoubleTap)]
        public bool DashOnDoubleTap { get; set; }

        [RuleInRange(ControlsRules.MinDoubleTapTime, ControlsRules.MaxDoubleTapTime)]
        [JsonProperty(Names.DoubleTapTime)]
        public float DoubleTapTime { get; set; }

        /// <summary> How far a finger may travel and still count as a tap, as a fraction of screen
        /// height. </summary>
        [RuleInRange(ControlsRules.MinTapTravel, ControlsRules.MaxTapTravel)]
        [JsonProperty(Names.TapMaxTravel)]
        public float TapMaxTravel { get; set; }

        /// <summary> Mirrors the whole on-screen layout. </summary>
        [RuleEnumValid(Handedness.Right)]
        [JsonProperty(Names.Handedness)]
        public Handedness Handedness { get; set; }

        [RuleEnumValid(ScreenAnchor.BottomLeft)]
        [JsonProperty(Names.JoystickAnchor)]
        public ScreenAnchor JoystickAnchor { get; set; }

        [RuleInRange(ControlsRules.MinControlSize, ControlsRules.MaxControlSize)]
        [JsonProperty(Names.JoystickSize)]
        public float JoystickSize { get; set; }

        /// <summary> Pixels the knob travels before the stick reads as fully deflected. </summary>
        [RuleInRange(ControlsRules.MinJoystickTravel, ControlsRules.MaxJoystickTravel)]
        [JsonProperty(Names.JoystickTravel)]
        public float JoystickTravel { get; set; }

        /// <summary> The stick's origin follows the first touch instead of staying where it is
        /// drawn. </summary>
        [JsonProperty(Names.JoystickDynamicOrigin)]
        public bool JoystickDynamicOrigin { get; set; }

        [RuleEnumValid(ScreenAnchor.BottomRight)]
        [JsonProperty(Names.DashButtonAnchor)]
        public ScreenAnchor DashButtonAnchor { get; set; }

        [RuleInRange(ControlsRules.MinControlSize, ControlsRules.MaxControlSize)]
        [JsonProperty(Names.DashButtonSize)]
        public float DashButtonSize { get; set; }

        /// <summary> Which icon the dash button draws, by index into the game's own set. </summary>
        [RuleMinValue(0)]
        [JsonProperty(Names.DashButtonIcon)]
        public int DashButtonIcon { get; set; }

        public override ControlMode GeneralMode => (ControlMode)Mode;
        public override ControlDevice Device => ControlDevice.Touchscreen;

        public TouchscreenControlsSettings()
        {
            ResetOwn();
            DeadZone = ControlsRules.DefaultTouchDeadZone;
        }
        public TouchscreenControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY, TouchscreenControlMode mode,
            float fingerOffsetX, float fingerOffsetY, bool dashOnSecondFinger, bool dashOnDoubleTap,
            float doubleTapTime, float tapMaxTravel, Handedness handedness, ScreenAnchor joystickAnchor,
            float joystickSize, float joystickTravel, bool joystickDynamicOrigin,
            ScreenAnchor dashButtonAnchor, float dashButtonSize, int dashButtonIcon)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
            FingerOffsetX = fingerOffsetX;
            FingerOffsetY = fingerOffsetY;
            DashOnSecondFinger = dashOnSecondFinger;
            DashOnDoubleTap = dashOnDoubleTap;
            DoubleTapTime = doubleTapTime;
            TapMaxTravel = tapMaxTravel;
            Handedness = handedness;
            JoystickAnchor = joystickAnchor;
            JoystickSize = joystickSize;
            JoystickTravel = joystickTravel;
            JoystickDynamicOrigin = joystickDynamicOrigin;
            DashButtonAnchor = dashButtonAnchor;
            DashButtonSize = dashButtonSize;
            DashButtonIcon = dashButtonIcon;
        }
        public override void Reset()
        {
            base.Reset();
            ResetOwn();
            DeadZone = ControlsRules.DefaultTouchDeadZone;
        }
        private void ResetOwn()
        {
            Mode = TouchscreenControlMode.Relative;
            FingerOffsetX = ControlsRules.DefaultFingerOffsetX;
            FingerOffsetY = ControlsRules.DefaultFingerOffsetY;
            DashOnSecondFinger = true;
            DashOnDoubleTap = false;
            DoubleTapTime = ControlsRules.DefaultDoubleTapTime;
            TapMaxTravel = ControlsRules.DefaultTapTravel;
            Handedness = Handedness.Right;
            JoystickAnchor = ScreenAnchor.BottomLeft;
            JoystickSize = ControlsRules.DefaultControlSize;
            JoystickTravel = ControlsRules.DefaultJoystickTravel;
            JoystickDynamicOrigin = false;
            DashButtonAnchor = ScreenAnchor.BottomRight;
            DashButtonSize = ControlsRules.DefaultControlSize;
            DashButtonIcon = 0;
        }

        public override object Clone() => CopyImpl();
        public override BaseDeviceControlsSettings Copy() => CopyImpl();
        TouchscreenControlsSettings ICopyable<TouchscreenControlsSettings>.Copy() => CopyImpl();

        private TouchscreenControlsSettings CopyImpl() => new(Active, Sensitivity, DeadZone,
            Smoothing, InvertX, InvertY, Mode, FingerOffsetX, FingerOffsetY, DashOnSecondFinger,
            DashOnDoubleTap, DoubleTapTime, TapMaxTravel, Handedness, JoystickAnchor, JoystickSize,
            JoystickTravel, JoystickDynamicOrigin, DashButtonAnchor, DashButtonSize, DashButtonIcon);

        public void Pull(TouchscreenControlsSettings source)
        {
            Active = source.Active;
            Sensitivity = source.Sensitivity;
            DeadZone = source.DeadZone;
            Smoothing = source.Smoothing;
            InvertX = source.InvertX;
            InvertY = source.InvertY;
            Mode = source.Mode;
            FingerOffsetX = source.FingerOffsetX;
            FingerOffsetY = source.FingerOffsetY;
            DashOnSecondFinger = source.DashOnSecondFinger;
            DashOnDoubleTap = source.DashOnDoubleTap;
            DoubleTapTime = source.DoubleTapTime;
            TapMaxTravel = source.TapMaxTravel;
            Handedness = source.Handedness;
            JoystickAnchor = source.JoystickAnchor;
            JoystickSize = source.JoystickSize;
            JoystickTravel = source.JoystickTravel;
            JoystickDynamicOrigin = source.JoystickDynamicOrigin;
            DashButtonAnchor = source.DashButtonAnchor;
            DashButtonSize = source.DashButtonSize;
            DashButtonIcon = source.DashButtonIcon;
        }

        public void Update(TouchscreenControlsSettings src)
        {
            base.Update(src);

            Mode = src.Mode;
            FingerOffsetX = src.FingerOffsetX;
            FingerOffsetY = src.FingerOffsetY;
            DashOnSecondFinger = src.DashOnSecondFinger;
            DashOnDoubleTap = src.DashOnDoubleTap;
            DoubleTapTime = src.DoubleTapTime;
            TapMaxTravel = src.TapMaxTravel;
            Handedness = src.Handedness;
            JoystickAnchor = src.JoystickAnchor;
            JoystickSize = src.JoystickSize;
            JoystickTravel = src.JoystickTravel;
            JoystickDynamicOrigin = src.JoystickDynamicOrigin;
            DashButtonAnchor = src.DashButtonAnchor;
            DashButtonSize = src.DashButtonSize;
            DashButtonIcon = src.DashButtonIcon;
        }

        public override bool Equals(object obj) => obj is TouchscreenControlsSettings value && Equals(value);
        public override int GetHashCode()
        {
            var hash = HashCode.Combine(base.GetHashCode(), Mode, FingerOffsetX, FingerOffsetY,
                DashOnSecondFinger, DashOnDoubleTap, DoubleTapTime, TapMaxTravel);
            return HashCode.Combine(hash, Handedness, JoystickAnchor, JoystickSize, JoystickTravel,
                JoystickDynamicOrigin, DashButtonAnchor, DashButtonSize);
        }

        public bool Equals(TouchscreenControlsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other)
                   && Mode == other.Mode
                   && FingerOffsetX.Equals(other.FingerOffsetX)
                   && FingerOffsetY.Equals(other.FingerOffsetY)
                   && DashOnSecondFinger == other.DashOnSecondFinger
                   && DashOnDoubleTap == other.DashOnDoubleTap
                   && DoubleTapTime.Equals(other.DoubleTapTime)
                   && TapMaxTravel.Equals(other.TapMaxTravel)
                   && Handedness == other.Handedness
                   && JoystickAnchor == other.JoystickAnchor
                   && JoystickSize.Equals(other.JoystickSize)
                   && JoystickTravel.Equals(other.JoystickTravel)
                   && JoystickDynamicOrigin == other.JoystickDynamicOrigin
                   && DashButtonAnchor == other.DashButtonAnchor
                   && DashButtonSize.Equals(other.DashButtonSize)
                   && DashButtonIcon == other.DashButtonIcon;
        }
    }
}

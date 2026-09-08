namespace BH.SDK.Rules
{
    // Limits for the control settings tree, in the "Min/Max/Default" shape the rest of this folder
    // uses for generic value limits. Nothing here clamps at runtime - same as every other UserSettings
    // group, these bound what the UI offers and what validation reports, and a hand-edited file with a
    // wilder number simply plays wilder.
    //
    // Two families of number live here and must not be mixed up. Angles and travel distances are
    // TILT/SCREEN quantities in their own units (degrees, pixels), while sizes, offsets and pad areas
    // are NORMALIZED - a fraction of screen height, or of the camera rect - because a thumb control has
    // to land in the same place on a 4:3 tablet and a 21:9 phone.

    /// <summary>
    /// Numeric bounds and defaults for <see cref="BH.SDK.Models.SettingGroups.Controls"/>.
    /// </summary>
    public static class ControlsRules
    {
        /// <summary> Multiplier applied to a cursor delta in Relative mode. </summary>
        public const float MinSensitivity = 0.05f;
        /// <summary> Upper bound of BaseDeviceControlsSettings.Sensitivity. </summary>
        public const float MaxSensitivity = 10f;
        /// <summary> The sensitivity used when nothing says otherwise, read by BaseDeviceControlsSettings. </summary>
        public const float DefaultSensitivity = 1f;
        /// <summary> The gamepad sensitivity used when nothing says otherwise, read by GamepadControlsSettings. </summary>
        public const float DefaultGamepadSensitivity = 2f;

        /// <summary> Fraction of a stick/tilt range treated as no input at all. </summary>
        public const float MinDeadZone = 0f;
        /// <summary> Upper bound of BaseDeviceControlsSettings.DeadZone. </summary>
        public const float MaxDeadZone = 0.9f;
        /// <summary> The dead zone used when nothing says otherwise, read by BaseDeviceControlsSettings. </summary>
        public const float DefaultDeadZone = 0.15f;

        /// <summary> The on-screen joystick's own dead zone, wider than a stick's: a thumb resting on
        /// glass never reads as exactly centred. </summary>
        public const float DefaultTouchDeadZone = 0.18f;

        /// <summary> A tilt's own dead zone, narrower than a stick's: a hand holding a device already
        /// rests wherever calibration put the neutral point, so a stick-sized band there reads as the
        /// device ignoring small tilts entirely. </summary>
        public const float DefaultGyroDeadZone = 0.05f;

        /// <summary> How much of the previous frame's input is carried over. 0 disables smoothing. </summary>
        public const float MinSmoothing = 0f;
        /// <summary> Upper bound of BaseDeviceControlsSettings.Smoothing. </summary>
        public const float MaxSmoothing = 1f;
        /// <summary> The smoothing used when nothing says otherwise, read by BaseDeviceControlsSettings. </summary>
        public const float DefaultSmoothing = 0f;

        /// <summary> A tilt is the one input read off a hand rather than off a switch, so it is the one
        /// that ships smoothed: the sensor faithfully reports a tremor no player meant to make. </summary>
        public const float DefaultGyroSmoothing = 0.06f;

        /// <summary> Size of the in-world cursor object, relative to its own default. </summary>
        public const float MinCursorScale = 0.1f;
        /// <summary> Upper bound of CommonControlsSettings.CursorScale. </summary>
        public const float MaxCursorScale = 4f;
        /// <summary> The cursor scale used when nothing says otherwise, read by CommonControlsSettings. </summary>
        public const float DefaultCursorScale = 1f;

        /// <summary> Seconds within which a second click counts as a double click. </summary>
        public const float MinDoubleClickTime = 0.05f;
        /// <summary> Upper bound of KeyboardMouseControlsSettings.DoubleClickTime. </summary>
        public const float MaxDoubleClickTime = 1f;
        /// <summary> The double click time used when nothing says otherwise, read by KeyboardMouseControlsSettings. </summary>
        public const float DefaultDoubleClickTime = 0.3f;

        /// <summary> Seconds within which a second tap counts as a double tap. </summary>
        public const float MinDoubleTapTime = 0.05f;
        /// <summary> Upper bound of TouchscreenControlsSettings.DoubleTapTime. </summary>
        public const float MaxDoubleTapTime = 1f;
        /// <summary> The double tap time used when nothing says otherwise, read by TouchscreenControlsSettings. </summary>
        public const float DefaultDoubleTapTime = 0.3f;

        /// <summary> How far a finger may travel and still count as a tap rather than a drag, as a
        /// fraction of screen height. </summary>
        public const float MinTapTravel = 0f;
        /// <summary> Upper bound of TouchscreenControlsSettings.TapMaxTravel. </summary>
        public const float MaxTapTravel = 0.5f;
        /// <summary> The tap travel used when nothing says otherwise, read by TouchscreenControlsSettings. </summary>
        public const float DefaultTapTravel = 0.05f;

        // The setting this bounds is itself called MaxTiltAngle - it is the tilt a player has to reach
        // for full deflection, and these are the range that number may be set to. Reading MaxTiltAngle
        // as "the largest tilt the game accepts" is the easy mistake: this is a limit on a limit.

        /// <summary> Degrees of tilt the player's own MaxTiltAngle may be set to. </summary>
        public const float MinTiltAngle = 5f;
        /// <summary> Upper bound of DeviceGyroControlsSettings.MaxTiltAngle. </summary>
        public const float MaxTiltAngle = 90f;

        // A wrist covers roughly 20 degrees comfortably, and the default has to be what a WRIST can
        // reach rather than what an arm can: at the old 35 the player ran out of comfortable travel
        // long before the avatar ran out of screen, which reads as the tilt barely responding.

        /// <summary> The tilt angle used when nothing says otherwise, read by DeviceGyroControlsSettings. </summary>
        public const float DefaultTiltAngle = 20f;

        /// <summary> Neutral tilt, per axis, in the same normalized deflection space input resolves
        /// to. Calibration writes it; 0 means "device held level". </summary>
        public const float MinTiltCenter = -1f;
        /// <summary> Upper bound of DeviceGyroControlsSettings.TiltCenterX, DeviceGyroControlsSettings.TiltCenterY. </summary>
        public const float MaxTiltCenter = 1f;
        /// <summary> The tilt center used when nothing says otherwise, read by DeviceGyroControlsSettings. </summary>
        public const float DefaultTiltCenter = 0f;

        /// <summary> Offset between the finger and the cursor in touch Absolute mode, as a fraction of
        /// camera height, so the avatar is not hidden under the thumb. </summary>
        public const float MinFingerOffset = -1f;
        /// <summary> Upper bound of TouchscreenControlsSettings.FingerOffsetX, TouchscreenControlsSettings.FingerOffsetY. </summary>
        public const float MaxFingerOffset = 1f;
        /// <summary> The finger offset X used when nothing says otherwise, read by TouchscreenControlsSettings. </summary>
        public const float DefaultFingerOffsetX = 0f;
        /// <summary> The finger offset Y used when nothing says otherwise, read by TouchscreenControlsSettings. </summary>
        public const float DefaultFingerOffsetY = 0.15f;

        /// <summary> Exponent applied to stick deflection: 1 is linear, higher favours small
        /// movements. </summary>
        public const float MinResponseCurve = 0.5f;
        /// <summary> Upper bound of GamepadControlsSettings.ResponseCurve. </summary>
        public const float MaxResponseCurve = 3f;
        /// <summary> The response curve used when nothing says otherwise, read by GamepadControlsSettings. </summary>
        public const float DefaultResponseCurve = 1f;

        /// <summary> Size of one on-screen control, as a fraction of screen height. </summary>
        public const float MinControlSize = 0.05f;
        /// <summary> Upper bound of DeviceGyroControlsSettings.DashButtonSize, TouchscreenControlsSettings.DashButtonSize, TouchscreenControlsSettings.JoystickSize. </summary>
        public const float MaxControlSize = 0.5f;
        /// <summary> The control size used when nothing says otherwise, read by DeviceGyroControlsSettings, TouchscreenControlsSettings. </summary>
        public const float DefaultControlSize = 0.18f;

        /// <summary> How far the on-screen stick's knob travels from its origin, in pixels, before it
        /// reads as fully deflected. </summary>
        public const float MinJoystickTravel = 20f;
        /// <summary> Upper bound of TouchscreenControlsSettings.JoystickTravel. </summary>
        public const float MaxJoystickTravel = 400f;
        /// <summary> The joystick travel used when nothing says otherwise, read by TouchscreenControlsSettings. </summary>
        public const float DefaultJoystickTravel = 100f;

    }
}

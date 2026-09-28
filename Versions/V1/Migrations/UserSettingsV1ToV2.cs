using System;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Controls;

namespace BH.SDK.Versions.V1.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> User settings, generation 1 to generation 2 - every group carried as it is except
    /// that the controls' shared <c>cursor_return</c> moves into the mouse and touchscreen groups, the
    /// device priority and manual selection (the most recently used device always leads), the
    /// gamepad's dash buttons and every form of motion-sensor locality; and the tutorial flag starts
    /// unset, so a player updating from 1.0.0 is offered the tutorial once. </summary>
    public class UserSettingsV1ToV2 : ModelMigration<UserSettingsV1, UserSettings>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override UserSettings Migrate(UserSettingsV1 from) => new(
            from.General,
            from.Audio,
            Migrate(from.Controls),
            from.Graphics,
            from.GameEditor,
            from.Interface,
            from.Keybindings,
            false); // the flag did not exist at generation 1

        private static ControlsSettings Migrate(ControlsSettingsV1 from) => new(
            new CommonControlsSettings(from.Common.CursorVisible, from.Common.CursorScale,
                from.Common.CursorRecenter),
            Migrate(from.KeyboardMouse, from.Common.CursorReturn),
            Migrate(from.Touchscreen, from.Common.CursorReturn),
            Migrate(from.Gamepad), Migrate(from.DeviceGyro));

        // cursor_return was ONE switch for every pointer at generation 1, default off, and playtests
        // read that default as a bug on a mouse - so a file still at the default takes each device's
        // own default instead, while a player who had turned it on keeps it on everywhere.
        private static KeyboardMouseControlsSettings Migrate(KeyboardMouseControlsSettingsV1 from,
            bool cursorReturn) => new(
            from.Active, from.Sensitivity, from.DeadZone, from.Smoothing, from.InvertX, from.InvertY,
            from.Mode, from.RequireHold, from.HoldButton, from.DashOnDoubleClick, from.DoubleClickTime,
            from.DashKeys, from.CursorHideAbsolute, from.CursorHideRelative, from.DashButton,
            cursorReturn || new KeyboardMouseControlsSettings().CursorReturn);

        private static TouchscreenControlsSettings Migrate(TouchscreenControlsSettingsV1 from,
            bool cursorReturn) => new(
            from.Active, from.Sensitivity, from.DeadZone, from.Smoothing, from.InvertX, from.InvertY,
            from.Mode, from.FingerOffsetX, from.FingerOffsetY, from.DashOnSecondFinger,
            from.DashOnDoubleTap, from.DoubleTapTime, from.TapMaxTravel, from.Handedness,
            from.JoystickAnchor, from.JoystickSize, from.JoystickTravel, from.JoystickDynamicOrigin,
            from.DashButtonAnchor, from.DashButtonSize, from.DashButtonIcon,
            cursorReturn || new TouchscreenControlsSettings().CursorReturn);

        // DashButtons is dropped rather than read: every button but Start and Select dashes now.
        private static GamepadControlsSettings Migrate(GamepadControlsSettingsV1 from) => new(
            from.Active, from.Sensitivity, from.DeadZone, from.Smoothing, from.InvertX, from.InvertY,
            from.Mode, from.MotionStick, from.ResponseCurve);

        // The calibration, the stored neutral point and the axis mapping are dropped: the tilt is read
        // against gravity with the phone lying flat as rest, which none of them can be translated into.
        // What IS carried is how far a player had to tilt - MaxTiltAngle becomes the sensitivity that
        // reaches full deflection at the same angle, sin(20) being the span a sensitivity of 1 means.
        private static DeviceGyroControlsSettings Migrate(DeviceGyroControlsSettingsV1 from) => new(
            from.Active, TiltSensitivity(from.MaxTiltAngle), from.DeadZone, from.Smoothing,
            from.InvertX, from.InvertY,
            from.Mode == DeviceGyroControlModeV1.Absolute
                ? DeviceGyroControlMode.Absolute
                : DeviceGyroControlMode.Direction, // Relative is gone; Direction was the default
            from.DashSource, from.DashButtonAnchor, from.DashButtonSize);

        private static float TiltSensitivity(float maxTiltAngle)
        {
            const double defaultTiltAngle = 20.0; // what a sensitivity of 1 means, frozen here
            var angle = Math.Clamp(maxTiltAngle, 5.0, 90.0) * Math.PI / 180.0;
            var sensitivity = Math.Sin(defaultTiltAngle * Math.PI / 180.0) / Math.Sin(angle);
            return (float)Math.Clamp(sensitivity, 0.05, 10.0);
        }
    }
}

using System;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Controls;
using BH.SDK.Models.SettingGroups.GameEditor;

namespace BH.SDK.Versions.V1.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> User settings, generation 1 to 2: cursor_return moves from the shared controls group
    /// onto the mouse and touchscreen groups; device priority, gamepad dash buttons and motion-sensor
    /// locality are dropped or defaulted; the tutorial flag starts unset, so 1.0.0 players see it. </summary>
    public class UserSettingsV1ToV2 : ModelMigration<UserSettingsV1, UserSettings>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override UserSettings Migrate(UserSettingsV1 from) => new(
            from.General,
            from.Audio,
            Migrate(from.Controls),
            Migrate(from.Graphics),
            Migrate(from.GameEditor),
            Migrate(from.Interface),
            from.Keybindings,
            false); // the flag did not exist at generation 1

        private static GraphicsSettings Migrate(GraphicsSettingsV1 from) => new(
            from.FpsTarget, from.FpsFixed, from.Audio, from.Effects, from.Avatar, from.PostProcessing,
            from.AntiAliasing, from.Textures, from.Display);

        // The constructor takes only six of the fifteen members (InterfaceSettings says why), so the
        // rest go through the initializer; LevelsLayout keeps its default, the grid V1 always opened on.
        private static InterfaceSettings Migrate(InterfaceSettingsV1 from) => new(
            from.OpenMenuOnLose, from.StatsActive, from.StatsAlignmentX, from.StatsAlignmentY,
            from.MenuBackground, from.ScreenOrientation)
        {
            StatsFrameObjects = from.StatsFrameObjects,
            StatsLevelObjects = from.StatsLevelObjects,
            StatsMemory = from.StatsMemory,
            StatsProfiling = from.StatsProfiling,
            ShowGameProgress = from.ShowGameProgress,
            ShowGamePause = from.ShowGamePause,
            ShowGameInterface = from.ShowGameInterface,
            HitboxRingOpacity = from.HitboxRingOpacity,
            AlertOnException = from.AlertOnException,
        };

        private static GameEditorSettings Migrate(GameEditorSettingsV1 from) => new(
            from.Savings, from.Camera, from.Player, from.Grid, from.Effects, from.Selection, from.Gizmos,
            from.Creation, from.Timeline, Migrate(from.Interface), from.Serialization,
            new EditorPublishingSettings());

        private static EditorInterfaceSettings Migrate(EditorInterfaceSettingsV1 from) => new(
            from.DirtyFieldDelay, from.RotationDisplayUnit, from.LogValueClamps, from.RenderInframes,
            from.LinkColliderToShape, from.SelectionAutoOpenActive, from.SyncTimelineExpansion,
            from.LogRuleFindings, from.ExpansionMask, from.HierarchyExpansion, from.TimelineExpansion);

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

        // Calibration, neutral point and axis mapping are dropped (no translation to tilt against
        // gravity); MaxTiltAngle becomes the sensitivity reaching full deflection at the same angle.
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

namespace BH.SDK.Models.Enums.Controls
{
    // Four sources, each supporting all three ControlModes; a gamepad is ONE device. Docs/ENUMS.md

    /// <summary>
    /// A source of avatar-control input. Ordering is the enum's own and carries no priority - which
    /// device leads is the most recently used one, and the platform's own order decides the first frame.
    /// </summary>
    public enum ControlDevice : byte
    {
        /// <summary>Keyboard and mouse together, as one device - they are never used apart.</summary>
        KeyboardMouse = 0,

        /// <summary>The device's own touchscreen.</summary>
        Touchscreen = 1,

        /// <summary>A gamepad, through the Input System's own layout - sticks and buttons.</summary>
        Gamepad = 2,

        /// <summary>The phone/tablet's own motion sensor.</summary>
        DeviceGyro = 3,
    }
}

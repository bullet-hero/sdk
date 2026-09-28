namespace BH.SDK.Models.Enums.Controls
{
    // Four sources, and every one of them genuinely supports all three ControlModes - that uniformity is
    // what lets one driver interface cover them instead of twelve special cases. A device is listed here
    // whether or not the running platform can reach it: what a platform PERMITS and what is PRESENT right
    // now are separate gates, decided outside the format (the game's own InputPlatformRules and each
    // driver's IsPresent).
    //
    // A gamepad is ONE device here, not three. An earlier design split its sticks, its touchpad and its
    // motion sensor into separate entries; both extra entries are gone, because the Input System exposes
    // neither a pad's touchpad position nor its gyro on any layout it ships, and reaching them meant
    // per-brand HID work for every pad family in existence. Everything gamepad-shaped goes through the
    // Input System's own Gamepad, and the phone's own sensor stays its own device (DeviceGyro), since that
    // one is genuinely readable.

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

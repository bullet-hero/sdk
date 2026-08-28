using System;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // WHICH DEVICE ACTUALLY STEERS, measured rather than assumed. This game treats four control
    // devices as equals and switches between them on the first press with no menu, so the honest
    // answer to "how do people play this" is not a setting anyone chose - it is the time each device
    // spent leading. That makes this the one group here whose value is to the project rather than to
    // the player.
    //
    // FOUR FIELDS RATHER THAN A DICTIONARY. The enum is closed at four, a value-type key would need
    // a converter for nothing, and a field per device means a build that stops supporting one leaves
    // its number readable in the file rather than dropping it.
    //
    // Only the LEADING device is charged. Every active driver is sampled every frame and exactly one
    // steers, so charging all of them would count one second of play four times.

    /// <summary> Real seconds each control device spent steering the avatar. </summary>
    [RuleContainer]
    public class DeviceTimeStatistics : IModel<DeviceTimeStatistics>
    {
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.KeyboardMouseSeconds)]
        public double KeyboardMouseSeconds { get; set; }

        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.TouchscreenSeconds)]
        public double TouchscreenSeconds { get; set; }

        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.GamepadSeconds)]
        public double GamepadSeconds { get; set; }

        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.DeviceGyroSeconds)]
        public double DeviceGyroSeconds { get; set; }

        public DeviceTimeStatistics() => Reset();

        public void Reset()
        {
            KeyboardMouseSeconds = 0.0;
            TouchscreenSeconds = 0.0;
            GamepadSeconds = 0.0;
            DeviceGyroSeconds = 0.0;
        }

        /// <summary> Charges seconds to one device. An unknown device is ignored rather than
        /// defaulted onto keyboard, since a wrong attribution is worse than a missing one. </summary>
        public void Add(ControlDevice device, double seconds)
        {
            switch (device)
            {
                case ControlDevice.KeyboardMouse: KeyboardMouseSeconds += seconds; break;
                case ControlDevice.Touchscreen: TouchscreenSeconds += seconds; break;
                case ControlDevice.Gamepad: GamepadSeconds += seconds; break;
                case ControlDevice.DeviceGyro: DeviceGyroSeconds += seconds; break;
            }
        }

        public double Get(ControlDevice device) => device switch
        {
            ControlDevice.KeyboardMouse => KeyboardMouseSeconds,
            ControlDevice.Touchscreen => TouchscreenSeconds,
            ControlDevice.Gamepad => GamepadSeconds,
            ControlDevice.DeviceGyro => DeviceGyroSeconds,
            _ => 0.0,
        };

        public object Clone() => Copy();

        public DeviceTimeStatistics Copy()
        {
            var copy = new DeviceTimeStatistics();
            copy.Update(this);
            return copy;
        }

        public void Update(DeviceTimeStatistics src)
        {
            KeyboardMouseSeconds = src.KeyboardMouseSeconds;
            TouchscreenSeconds = src.TouchscreenSeconds;
            GamepadSeconds = src.GamepadSeconds;
            DeviceGyroSeconds = src.DeviceGyroSeconds;
        }

        public void Pull(DeviceTimeStatistics source) => Update(source);

        public override bool Equals(object obj) => obj is DeviceTimeStatistics value && Equals(value);

        public bool Equals(DeviceTimeStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return KeyboardMouseSeconds.Equals(other.KeyboardMouseSeconds)
                   && TouchscreenSeconds.Equals(other.TouchscreenSeconds)
                   && GamepadSeconds.Equals(other.GamepadSeconds)
                   && DeviceGyroSeconds.Equals(other.DeviceGyroSeconds);
        }

        public override int GetHashCode() =>
            HashCode.Combine(KeyboardMouseSeconds, TouchscreenSeconds, GamepadSeconds, DeviceGyroSeconds);
    }
}

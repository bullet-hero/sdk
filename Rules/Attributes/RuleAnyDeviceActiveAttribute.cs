using System;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Controls;
using BH.SDK.Services.Controls;

namespace BH.SDK.Rules.Attributes
{
    // The one invariant in the control tree a player must not be able to break: with every device
    // inactive there is no way left to move the avatar, and no way inside the game to notice why. The UI
    // refuses to clear the last checkbox and settings load falls back to platform defaults, but neither
    // helps a hand-edited or foreign file - which is exactly what validation is for.
    //
    // A class rule rather than a property one because the invariant spans the four device groups.

    /// <summary>
    /// At least one control device must be active. Fix activates the first one in the catalog's order.
    /// </summary>
    [AttributeUsage(ClassTarget)]
    public class RuleAnyDeviceActiveAttribute : BaseObjectRuleAttribute
    {
        /// <summary> <c>"rule_any_device_active"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_any_device_active";

        // Warning, not Error: this is UserSettings, not a level. A player who turned every device off
        // did it to themselves and can turn one back on; nothing about the file is malformed.

        /// <summary> A warning: the level still plays, but this is not what the author meant. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> Sits on the controls settings tree, which is the only place the invariant is visible. </summary>
        protected override bool IsValidTypeInternal(Type type)
            => typeof(ControlsSettings).IsAssignableFrom(type);

        /// <summary> Passes while at least one device is active. </summary>
        protected override bool IsValidInternal(object target, RuleContext context)
            => target is ControlsSettings settings && settings.HasActiveDevice();

        /// <summary> Activates the first device of the catalog. Which one a platform would rather have is
        /// the consumer's call - the format only guarantees that one is on. </summary>
        protected override void FixInternal(object target, RuleContext context)
        {
            if (target is not ControlsSettings settings) return;
            if (settings.HasActiveDevice()) return;

            settings.GetDevice(ControlDeviceCatalog.Devices[0]).Active = true;
        }
    }
}

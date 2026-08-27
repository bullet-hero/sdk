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
    // A class rule rather than a property one because the invariant spans five properties: four device
    // groups and the priority order the fix picks from.

    /// <summary>
    /// At least one control device must be active. Fix activates the first one in the authored priority
    /// order.
    /// </summary>
    [AttributeUsage(ClassTarget)]
    public class RuleAnyDeviceActiveAttribute : BaseObjectRuleAttribute
    {
        public override string RuleNameKey => "rule_any_device_active";

        protected override bool IsValidTypeInternal(Type type)
            => typeof(ControlsSettings).IsAssignableFrom(type);

        protected override bool IsValidInternal(object target, RuleContext context)
            => target is ControlsSettings settings && settings.HasActiveDevice();

        protected override void FixInternal(object target, RuleContext context)
        {
            if (target is not ControlsSettings settings) return;
            if (settings.HasActiveDevice()) return;

            var priority = settings.Priority;
            if (priority != null)
            {
                foreach (var device in priority)
                {
                    if (!System.Enum.IsDefined(typeof(ControlDevice), device)) continue;
                    settings.GetDevice(device).Active = true;
                    return;
                }
            }

            settings.GetDevice(ControlDeviceCatalog.Devices[0]).Active = true;
        }
    }
}

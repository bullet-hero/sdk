using System;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Services.Controls;

namespace BH.SDK.Rules.Attributes
{
    // A permutation, not a list, and that is what makes this its own rule rather than a pair of
    // generic collection ones. RuleCollectionCount would accept six copies of the same device;
    // RuleCollectionUnique would accept four devices out of six; neither knows the enum, so neither
    // catches a value no device declares. All three failures have the same silent outcome - a device
    // the player can never reach, or one that appears twice in a reorderable list - so the check has to
    // see the array against the catalog.
    //
    // Fix rebuilds rather than resets: the surviving order is what the player arranged, and throwing it
    // away over one bad entry would silently reshuffle a working layout.

    /// <summary>
    /// A control-priority array must list every <see cref="ControlDevice"/> exactly once. Fix keeps the
    /// valid entries in their authored order and appends whatever is missing, in catalog order.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleControlPriorityAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_control_priority";

        // Warning, not Error: a broken permutation makes one device unreachable or listed twice. The
        // game runs and the other devices steer; this is a settings file describing preferences,
        // not a level describing content.
        public override RuleGroup Group => RuleGroup.Warning;

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => property.PropertyType == typeof(ControlDevice[]);

        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not ControlDevice[] priority) return false;
            if (priority.Length != ControlDeviceCatalog.DeviceCount) return false;

            var seen = new HashSet<ControlDevice>();
            foreach (var device in priority)
            {
                if (!System.Enum.IsDefined(typeof(ControlDevice), device)) return false;
                if (!seen.Add(device)) return false;
            }
            return true;
        }

        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var authored = property.GetValue(target) as ControlDevice[];

            var result = new List<ControlDevice>(ControlDeviceCatalog.DeviceCount);
            var seen = new HashSet<ControlDevice>();

            if (authored != null)
            {
                foreach (var device in authored)
                {
                    if (!System.Enum.IsDefined(typeof(ControlDevice), device)) continue;
                    if (!seen.Add(device)) continue;
                    result.Add(device);
                }
            }

            foreach (var device in ControlDeviceCatalog.Devices)
                if (seen.Add(device)) result.Add(device);

            property.SetValue(target, result.ToArray());
        }
    }
}

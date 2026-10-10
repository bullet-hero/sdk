using System;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Rules.Attributes
{
    // Lives on the object because both id slots and the type are visible there. The level cover
    // (ResourceType.LevelLogo) is no resource and has neither id: its type is the whole address.

    /// <summary>
    /// A resource record fills exactly the id slot its <see cref="ResourceMeta.ResourceType"/> uses:
    /// a user-range int for a file resource, a guid for a data resource, neither for the level cover.
    /// </summary>
    [AttributeUsage(ClassTarget)]
    public class RuleResourceMetaIdAttribute : BaseObjectRuleAttribute
    {
        /// <summary> <c>"rule_resource_meta_id"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_resource_meta_id";

        // Warning, not Error: this is metadata.json, and a misfiled credit leaves the level exactly as
        // playable as before. What it breaks is the paperwork, which publishing checks on its own.

        /// <summary> A warning: the level plays, the credit points nowhere useful. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> Sits on resource records only. </summary>
        protected override bool IsValidTypeInternal(Type type)
            => typeof(ResourceMeta).IsAssignableFrom(type);

        /// <summary> Passes while the used slot is set and the unused one is empty. </summary>
        protected override bool IsValidInternal(object target, RuleContext context)
        {
            if (target is not ResourceMeta meta) return false;

            if (meta.ResourceType.IsTypeAddressed())
                return meta.ResourceId == TypedResourceId.Null && meta.ResourceGuid == Guid.Empty;

            return meta.ResourceType.IsGuidAddressed()
                ? meta.ResourceId == TypedResourceId.Null && meta.ResourceGuid != Guid.Empty
                : meta.ResourceId.value <= TypedResourceId.MaxUserDefinedValue && meta.ResourceGuid == Guid.Empty;
        }

        // What the property rule it replaced did for a file resource - clamp into the user range - plus
        // clearing the slot the type does not use. A data resource with no guid cannot be repaired: which
        // resource it meant is not recoverable, and the record stays reported.

        /// <summary> Clears the unused slot and clamps a file resource's id into the user range. </summary>
        protected override void FixInternal(object target, RuleContext context)
        {
            if (target is not ResourceMeta meta) return;

            if (meta.ResourceType.IsTypeAddressed())
            {
                meta.ResourceId = TypedResourceId.Null;
                meta.ResourceGuid = Guid.Empty;
                return;
            }

            if (meta.ResourceType.IsGuidAddressed())
            {
                meta.ResourceId = TypedResourceId.Null;
                return;
            }

            meta.ResourceGuid = Guid.Empty;
            if (meta.ResourceId.value > TypedResourceId.MaxUserDefinedValue)
                meta.ResourceId = new TypedResourceId(TypedResourceId.MaxUserDefinedValue);
        }
    }
}
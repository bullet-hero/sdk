using System;
using System.Reflection;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Rules.Attributes
{
    // Catches a dangling texture/font/clip id. Only the int-backed id family is checked (negative =
    // this level's own, positive = the game's registries, which the SDK cannot see); Guid-backed ids
    // belong to the graph pass. No level in context: stands down. Scope: BH.SDK/Docs/RULES.md.

    /// <summary>
    /// A user-defined resource id must have a matching entry in the level's own resource dictionary.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleReferenceExistsAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_reference_exists"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_reference_exists";

        // Warning, not Error, and no Fix: a dangling reference is unrepairable by design, so Error would
        // be a level that can never stop being one.

        /// <summary> A warning: the level still plays, but this is not what the author meant. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> Which resource collection the reference must be found in. </summary>
        public ResourceReferenceKind Kind { get; }

        /// <summary> Whether an unset reference is a legitimate authored state rather than a
        /// dangling one. </summary>
        public bool AllowNull { get; set; }

        /// <summary> Takes which resource collection the reference must be found in. </summary>
        public RuleReferenceExistsAttribute(ResourceReferenceKind kind)
        {
            Kind = kind;
        }

        /// <summary> Takes which resource collection the reference must be found in and whether an unset reference passes. </summary>
        public RuleReferenceExistsAttribute(ResourceReferenceKind kind, bool allowNull) : this(kind)
        {
            AllowNull = allowNull;
        }

        /// <summary> Applies to int id wrappers. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IPrimitiveInt).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the id names a resource the level actually carries, or is unset where that is allowed. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IPrimitiveInt primitive) return false;

            var id = primitive.Value;
            if (id == TypedResourceId.NullValue) return AllowNull;

            // Game-defined: permanent, owned by the game's own registries, invisible from here.
            if (id > TypedResourceId.NullValue) return true;

            var level = context?.Level;
            if (level?.Resources == null) return true;

            return Kind switch
            {
                ResourceReferenceKind.Texture =>
                    level.Resources.Textures.ContainsKey(new TextureResourceId(id)),
                ResourceReferenceKind.Font =>
                    level.Resources.Fonts.ContainsKey(new FontResourceId(id)),
                ResourceReferenceKind.Audio =>
                    level.Resources.Audios.ContainsKey(new AudioResourceId(id)),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        // Not repairable: every candidate repair invents content. Reports and stops.

        /// <summary> Off: which resource was meant is not knowable here, so a dangling reference is reported and left alone. </summary>
        public override bool HasFix => false;

        /// <summary> Nothing - see HasFix. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context) { }
    }
}

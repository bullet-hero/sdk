using System;
using System.Reflection;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Models.Primitives.Resources;

namespace BH.SDK.Rules.Attributes
{
    // Catches the dangling reference: a texture, font or clip id that no entry in the level's own
    // Resources answers to. Until now nothing did - a reference was checked for being non-zero and
    // in the right numeric range, never for pointing at anything.
    //
    // Scope, and why it is narrower than the name suggests:
    //
    // - Only the int-backed id family (TypedResourceId and its typed wrappers) can be checked here,
    //   because only there does the id's own sign say where it must resolve: negative = defined by
    //   this level, positive = defined by the game and baked into its registries, which the SDK
    //   cannot see and must therefore accept.
    // - The Guid-backed ids (ShapeId, ThemeId, EffectId, PrefabId) carry no such split - a Guid
    //   has no sign - so "does it exist" can only be answered against the game's own registry PLUS
    //   the level's, and the SDK has half the answer. Checking them here would report every
    //   game-defined collider preset as dangling. They belong to the graph pass, where a consumer
    //   can supply its registry.
    // - With no level in context (a standalone Prefab, a LevelMeta) there is nothing to resolve
    //   against, so the rule stands down rather than reporting everything as missing.

    /// <summary>
    /// A user-defined resource id must have a matching entry in the level's own resource dictionary.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleReferenceExistsAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_reference_exists";

        // Warning, not Error, and Fix has nothing to do with it: a dangling reference is UNREPAIRABLE by
        // design - inventing an id would point the object at some other resource - so reporting it as
        // Error would mean a level that can never stop being an error. The consumer substitutes its
        // fallback and the level plays; what it loses is one texture, font or clip.
        public override RuleGroup Group => RuleGroup.Warning;

        public ResourceReferenceKind Kind { get; }

        /// <summary> Whether an unset reference is a legitimate authored state rather than a
        /// dangling one. </summary>
        public bool AllowNull { get; set; }

        public RuleReferenceExistsAttribute(ResourceReferenceKind kind)
        {
            Kind = kind;
        }

        public RuleReferenceExistsAttribute(ResourceReferenceKind kind, bool allowNull) : this(kind)
        {
            AllowNull = allowNull;
        }

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IPrimitiveInt).IsAssignableFrom(property.PropertyType);

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

        // Not repairable. Every candidate repair invents content: pointing the reference at some
        // other resource shows the wrong asset, and clearing it to Null either hides an object
        // entirely or - where Null is a real state - changes what the object means. A dangling
        // reference is a decision for whoever is editing the level, so this reports and stops.
        public override bool HasFix => false;

        protected override void FixInternal(object target, PropertyInfo property, RuleContext context) { }
    }
}

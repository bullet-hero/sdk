using System;
using System.Reflection;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // ModificationKey is a struct used as a dictionary key, which puts it in the analyzer's blind
    // spot twice over: keys are not walked, and a struct can never be a [RuleContainer] anyway. So
    // its two fields - the template-inner ObjectId being overridden and the field path - were never
    // validated at all, while a path that fails to resolve degrades into "the override silently
    // does not apply" rather than anything visible.
    //
    // This checks the key's shape from the property that owns it (Modification.Key), which the
    // analyzer does walk. What it deliberately does NOT check is whether that ObjectId exists in the
    // referenced template or whether the path resolves against that object's real type - both need
    // the placement, its PrefabId and the template it points at, i.e. the graph pass.

    /// <summary>
    /// A modification key must address a real user-space object and carry a non-empty, bounded field
    /// path.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleModificationKeyValidAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_modification_key_valid";

        // Warning, not Error, and its own header already says why: a key that does not resolve degrades
        // into "the override silently does not apply". The placement still materializes and the
        // level still plays - it plays the template's value instead of the author's.
        public override RuleGroup Group => RuleGroup.Warning;

        public int MaxPathLength { get; set; }

        public RuleModificationKeyValidAttribute(int maxPathLength)
        {
            MaxPathLength = maxPathLength;
        }

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ModificationKey).IsAssignableFrom(property.PropertyType);

        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not ModificationKey key) return false;
            if (!key.ObjectId.IsValid()) return false;

            return !string.IsNullOrEmpty(key.Path) && key.Path.Length <= MaxPathLength;
        }

        // Only an over-long path is repairable, and even that only by truncation, which will usually
        // leave a path that no longer resolves - the issue simply moves from "malformed" to
        // "dangling", where the graph pass can see it.
        //
        // A broken ObjectId is deliberately left alone: repointing it at some other template object
        // would apply the author's override to the wrong object, silently and plausibly. Dropping
        // the whole entry is the right repair and it belongs to whoever owns the dictionary.
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not ModificationKey key) return;
            if (key.Path == null || key.Path.Length <= MaxPathLength) return;

            property.SetValue(target, new ModificationKey(key.ObjectId, key.Path[..MaxPathLength]));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The safety net for this whole folder. Adding a rule attribute without a test file is easy and
    // invisible; this makes it a compile-and-run failure instead. The registry is deliberately a
    // hand-written list rather than a naming convention, because several rules are covered by a
    // shared file (the IVector3/IVector4 families, the IPrimitiveInt trio) and a convention would
    // either forbid that or be too loose to catch anything.

    /// <summary>
    /// Every concrete BaseRuleAttribute in the SDK has a test file, and every entry in the registry
    /// still refers to a rule that exists.
    /// </summary>
    public class RuleCoverageTests
    {
        private static readonly Dictionary<Type, string> Covered = new()
        {
            { typeof(RuleMinValueAttribute), nameof(RuleMinTests) },
            { typeof(RuleMaxValueAttribute), nameof(RuleMaxTests) },
            { typeof(RuleInRangeAttribute), nameof(RuleInRangeTests) },
            { typeof(RuleNotNullAttribute), nameof(RuleNotNullTests) },
            { typeof(RuleStringMaxAttribute), nameof(RuleStringMaxTests) },

            { typeof(RuleCollectionCountAttribute), nameof(RuleCollectionCountTests) },
            { typeof(RuleCollectionMaxCountAttribute), nameof(RuleCollectionMaxCountTests) },
            { typeof(RuleCollectionUniqueAttribute), nameof(RuleCollectionUniqueTests) },

            { typeof(RuleIFloatInRangeAttribute), nameof(RuleIFloatInRangeTests) },
            { typeof(RuleIFloatMinAttribute), nameof(RuleIFloatMinTests) },
            { typeof(RuleIFloatMaxAttribute), nameof(RuleIFloatMaxTests) },
            { typeof(RuleIIntInRangeAttribute), nameof(RuleIIntInRangeTests) },
            { typeof(RuleIStringMaxAttribute), nameof(RuleIStringMaxTests) },

            { typeof(RuleIVector2InRangeAttribute), nameof(RuleIVector2InRangeTests) },
            { typeof(RuleIVector2MinAttribute), nameof(RuleIVector2MinTests) },
            { typeof(RuleIVector2MaxAttribute), nameof(RuleIVector2MaxTests) },
            { typeof(RuleIVector3InRangeAttribute), nameof(RuleIVector3RangeTests) },
            { typeof(RuleIVector3MinAttribute), nameof(RuleIVector3RangeTests) },
            { typeof(RuleIVector3MaxAttribute), nameof(RuleIVector3RangeTests) },
            { typeof(RuleIVector4InRangeAttribute), nameof(RuleIVector4RangeTests) },
            { typeof(RuleIVector4MinAttribute), nameof(RuleIVector4RangeTests) },
            { typeof(RuleIVector4MaxAttribute), nameof(RuleIVector4RangeTests) },

            { typeof(RuleIPrimitiveIntNotNullAttribute), nameof(RuleIPrimitiveIntTests) },
            { typeof(RuleIPrimitiveIntMinAttribute), nameof(RuleIPrimitiveIntTests) },
            { typeof(RuleIPrimitiveIntMaxAttribute), nameof(RuleIPrimitiveIntTests) },
            { typeof(RuleIPrimitiveGuidNotNullAttribute), nameof(RuleIPrimitiveGuidNotNullTests) },

            { typeof(RuleLevelFrameAttribute), nameof(RuleLevelFrameTests) },
            { typeof(RuleObjectIdValidAttribute), nameof(RuleObjectIdValidTests) },
            { typeof(RuleParentObjectIdValidAttribute), nameof(RuleParentObjectIdValidTests) },

            { typeof(RuleEnumValidAttribute), nameof(RuleEnumValidTests) },
            { typeof(RuleEnumFlagsValidAttribute), nameof(RuleEnumFlagsValidTests) },
            { typeof(RuleFiniteNumberAttribute), nameof(RuleFiniteTests) },
            { typeof(RuleIVector2OrderedAttribute), nameof(RuleIVector2OrderedTests) },
            { typeof(RuleCollectionMinCountAttribute), nameof(RuleCollectionMinCountTests) },
            { typeof(RuleCollectionNoNullItemsAttribute), nameof(RuleCollectionNoNullItemsTests) },
            { typeof(RuleCollectionSortedAttribute), nameof(RuleCollectionSortedTests) },
            { typeof(RuleStringPatternAttribute), nameof(RuleStringPatternTests) },
            { typeof(RuleDictionaryKeyMatchesAttribute), nameof(RuleDictionaryKeyMatchesTests) },
            { typeof(RuleModificationKeyValidAttribute), nameof(RuleModificationKeyValidTests) },
            { typeof(RuleReferenceExistsAttribute), nameof(RuleReferenceExistsTests) },

            { typeof(RulePropertyOrderAttribute), nameof(RulePropertyOrderTests) },
            { typeof(RuleShapeGeometryAttribute), nameof(RuleShapeGeometryTests) },

            { typeof(RuleControlPriorityAttribute), nameof(RuleControlPriorityTests) },
            { typeof(RuleAnyDeviceActiveAttribute), nameof(RuleAnyDeviceActiveTests) },
            { typeof(RuleShortcutBindingsAttribute), nameof(RuleShortcutBindingsTests) },
        };

        private static IEnumerable<Type> ConcreteRules => typeof(BaseRuleAttribute).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(BaseRuleAttribute).IsAssignableFrom(type));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestEveryRuleHasTests()
        {
            var missing = ConcreteRules.Where(type => !Covered.ContainsKey(type))
                .Select(type => type.Name).ToList();

            CollectionAssert.IsEmpty(missing,
                "Rule attributes without a test file - add one under Tests/Rules/ and register it here: "
                + string.Join(", ", missing));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestRegistryHasNoStaleEntries()
        {
            var live = new HashSet<Type>(ConcreteRules);
            var stale = Covered.Keys.Where(type => !live.Contains(type))
                .Select(type => type.Name).ToList();

            CollectionAssert.IsEmpty(stale,
                "Registry names rules that no longer exist: " + string.Join(", ", stale));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestEveryRuleHasNameKey()
        {
            var unnamed = ConcreteRules
                .Where(type => !NameKeyOf(type).StartsWith(KeyPrefix, StringComparison.Ordinal))
                .Select(type => type.Name).ToList();

            CollectionAssert.IsEmpty(unnamed,
                "Rules whose RuleNameKey is empty or not prefixed with \"" + KeyPrefix + "\": "
                + string.Join(", ", unnamed));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestEveryRuleNameKeyIsUnique()
        {
            var duplicates = ConcreteRules.GroupBy(NameKeyOf)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key + " -> " + string.Join("/", group.Select(type => type.Name)))
                .ToList();

            CollectionAssert.IsEmpty(duplicates,
                "Rules sharing one RuleNameKey: " + string.Join(", ", duplicates));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TestEveryRuleNameKeyMatchesTypeName()
        {
            var mismatched = ConcreteRules
                .Where(type => NameKeyOf(type) != ExpectedNameKey(type))
                .Select(type => type.Name + ": \"" + NameKeyOf(type) + "\" != \"" + ExpectedNameKey(type) + "\"")
                .ToList();

            CollectionAssert.IsEmpty(mismatched,
                "RuleNameKey does not follow the naming scheme: " + string.Join(", ", mismatched));
        }

        // Rule attributes have no parameterless constructor (every one of them takes its bounds), so
        // reading an instance property off a Type means building the instance without running a
        // constructor. That is safe here precisely because RuleNameKey is a literal on every rule -
        // it reads no field, so an uninitialized instance answers exactly what a real one would.
        private static string NameKeyOf(Type type)
            => ((BaseRuleAttribute)FormatterServices.GetUninitializedObject(type)).RuleNameKey ?? string.Empty;

        /// <summary> The key a rule of this type is expected to declare: "rule_" plus the type name
        /// without its Rule prefix and Attribute suffix, in snake_case, with a leading interface "I"
        /// glued to the word it prefixes (IFloat -> ifloat). </summary>
        private static string ExpectedNameKey(Type type)
        {
            var name = type.Name;
            if (name.StartsWith("Rule", StringComparison.Ordinal)) name = name["Rule".Length..];
            if (name.EndsWith("Attribute", StringComparison.Ordinal)) name = name[..^"Attribute".Length];

            var words = new List<string>();
            var start = 0;
            for (var i = 1; i < name.Length; i++)
            {
                if (!char.IsUpper(name[i])) continue;
                words.Add(name[start..i]);
                start = i;
            }

            words.Add(name[start..]);

            if (words.Count > 1 && words[0] == "I")
            {
                words[1] = words[0] + words[1];
                words.RemoveAt(0);
            }

            return KeyPrefix + string.Join("_", words.Select(word => word.ToLowerInvariant()));
        }

        private const string KeyPrefix = "rule_";
    }
}
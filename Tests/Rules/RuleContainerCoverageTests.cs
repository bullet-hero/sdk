using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Clipboard;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Statistics;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // Which types the walk REACHES, asserted not assumed: a model without the marker takes its subtree out of
    // validation silently. [RuleContainer] is inherited. BH.SDK/Docs/TESTING.md, "Rules fixtures".


    /// <summary> Which types the walk actually reaches, asserted rather than assumed - a model reachable from a
    /// root without the marker takes its whole subtree out of validation silently. [RuleContainer] is
    /// INHERITED, which is the fact reading the sources for a declared attribute gets wrong. </summary>
    [TestFixture]
    public class RuleContainerCoverageTests
    {
        /// <summary> Every aggregate root a consumer may hand to ValidationFacade. </summary>
        private static readonly Type[] Roots =
        {
            typeof(Level), typeof(LevelMeta), typeof(UserSettings), typeof(Prefab),
            typeof(EffectData), typeof(ThemeData), typeof(CompositeShape), typeof(ClipboardData),
            typeof(GameStatistics), typeof(LevelStatistics),
        };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryReachableModel_IsARuleContainer()
        {
            var missing = new List<string>();

            foreach (var root in Roots)
            {
                var visited = new HashSet<Type>();
                Walk(root, root.Name, visited, missing);
            }

            Assert.That(missing, Is.Empty, "these models are reachable from an aggregate root and "
                                           + "carry [GenerateModel], but the walk stops before them:\n"
                                           + string.Join("\n", missing));
        }

        // Reachability is RuleAnalyzer's own: readable+writable public properties; list element, dictionary
        // VALUE, array element. Anything outside this assembly is a leaf.

        private static void Walk(Type type, string path, HashSet<Type> visited, List<string> missing)
        {
            if (type == null || !visited.Add(type)) return;
            if (type.Assembly != typeof(Level).Assembly) return;
            if (type.IsValueType || type == typeof(string)) return;

            if (type.GetCustomAttribute<GenerateModelAttribute>(true) != null
                && type.GetCustomAttribute<RuleContainerAttribute>(true) == null)
            {
                missing.Add($"  {type.FullName}  (reached as {path})");
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite);

            foreach (var property in properties)
            {
                var next = Unwrap(property.PropertyType);
                Walk(next, path + "." + property.Name, visited, missing);

                // A declared type says nothing about what it holds: every polymorphic member here is
                // typed as its interface or its base, and the variant carrying the rules is the
                // runtime one. So each implementor is reachable too.
                foreach (var implementor in Implementors(next))
                    Walk(implementor, path + "." + property.Name, visited, missing);
            }
        }

        private static Type Unwrap(Type type)
        {
            if (type.IsList()) return type.GetGenericArguments()[0];
            if (type.IsDictionary()) return type.GetDictionaryValueGenericParameterOrDefault();
            if (type.IsArray) return type.GetElementType();
            return type;
        }

        private static IEnumerable<Type> Implementors(Type type)
        {
            if (type == null || type.IsValueType || type.IsSealed) yield break;
            if (type.Assembly != typeof(Level).Assembly) yield break;

            foreach (var candidate in typeof(Level).Assembly.GetTypes())
            {
                if (candidate.IsAbstract || candidate.IsInterface) continue;
                if (candidate == type || !type.IsAssignableFrom(candidate)) continue;
                yield return candidate;
            }
        }

        // [RuleContainer] is INHERITED but the generator sees only DECLARED attributes, so every container
        // must declare it (two once did not, and were validated all along).

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryRuleContainer_DeclaresTheAttributeRatherThanInheritingIt()
        {
            var declared = 0;
            var inherited = new List<string>();

            foreach (var type in typeof(Level).Assembly.GetTypes())
            {
                if (type.GetCustomAttribute<RuleContainerAttribute>(true) == null) continue;

                if (type.GetCustomAttribute<RuleContainerAttribute>(false) != null) declared++;
                else inherited.Add(type.Name);
            }

            var report = new StringBuilder()
                .Append(declared).Append(" declared, ").Append(inherited.Count).Append(" inherited");
            TestContext.WriteLine(report.ToString());

            Assert.That(inherited, Is.Empty, "these types are rule containers by inheritance alone, "
                                             + "so ValidationGenerator cannot see them and they keep "
                                             + "the reflective walk:\n  "
                                             + string.Join("\n  ", inherited));
        }

        // Every model of the format has a generated walk (scoped to the SDK assembly): a type losing it
        // falls back to reflection silently. The nested fixtures here are deliberately non-partial.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryRuleContainerInTheSdk_HasAGeneratedWalk()
        {
            var missing = new List<string>();
            var generated = 0;

            foreach (var type in typeof(Level).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || type.IsValueType) continue;
                if (type.GetCustomAttribute<RuleContainerAttribute>(true) == null) continue;

                if (typeof(IValidatable).IsAssignableFrom(type)) generated++;
                else missing.Add(type.FullName);
            }

            TestContext.WriteLine($"{generated} generated walks, {missing.Count} still reflective");

            Assert.That(missing, Is.Empty,
                "these [RuleContainer] types have no generated walk and fall back to reflection. "
                + "The usual cause is a missing `partial`; rebuilding BH.SDK.Roslyn "
                + "(Tools > BH.SDK.Roslyn > Build Analyzer) is the other:\n  "
                + string.Join("\n  ", missing));
        }

        // Transitive pruning buys almost nothing (measured): Keyframe.Frame and .Ease carry rules, so a least
        // fixed point leaves leaves and no subtree. Worth it in the generator, not the reflective walk.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TransitivePruning_WouldSkipOnlyLeaves()
        {
            var containers = typeof(Level).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .Where(type => type.GetCustomAttribute<RuleContainerAttribute>(true) != null)
                .ToArray();

            var hasRules = new HashSet<Type>(containers.Where(HasOwnRules));

            // Least fixed point: nothing has rules until something proves it does, so a cycle stays
            // out rather than propagating a rule it never had.
            bool moved;
            do
            {
                moved = false;
                foreach (var type in containers)
                {
                    if (hasRules.Contains(type)) continue;
                    if (!Reaches(type, hasRules)) continue;

                    hasRules.Add(type);
                    moved = true;
                }
            } while (moved);

            var prunable = containers.Where(type => !hasRules.Contains(type)).ToArray();

            TestContext.WriteLine($"{prunable.Length} of {containers.Length} containers carry no "
                                  + "rule anywhere below them:\n  "
                                  + string.Join("\n  ", prunable.Select(type => type.Name)));

            Assert.That(prunable.Length, Is.LessThan(20),
                "a tripwire, not a budget: if this jumps, the prize transitive pruning was supposed "
                + "to pay out has appeared, and the generator should start emitting the flag");
        }

        private static bool HasOwnRules(Type type)
        {
            if (type.GetCustomAttributes<BaseObjectRuleAttribute>(true).Any()) return true;

            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite)
                .Any(property => property.GetCustomAttributes<BasePropertyRuleAttribute>(true).Any());
        }

        /// <summary> Does anything this type can descend into already carry a rule? </summary>
        private static bool Reaches(Type type, HashSet<Type> hasRules)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite) continue;

                var next = Unwrap(property.PropertyType);
                if (next == null) continue;
                if (hasRules.Contains(next)) return true;

                // A declared type is not what is walked - the runtime one is - so a base whose
                // subtype carries rules is reached just as surely.
                if (Implementors(next).Any(hasRules.Contains)) return true;
            }

            return false;
        }
    }
}
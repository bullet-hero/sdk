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
    // WHICH TYPES THE WALK ACTUALLY REACHES, asserted rather than assumed. RuleAnalyzer stops dead
    // at the first type that is not a [RuleContainer] - it is the walk's only gate - so a model
    // reachable from an aggregate root without the marker takes its whole subtree out of validation
    // silently, and every rule inside it stops running with nothing to show for it.
    //
    // [RuleContainer] IS INHERITED, and that is the fact worth pinning here rather than merely
    // knowing. The attribute declares [AttributeUsage(AttributeTargets.Class)] and does not turn
    // Inherited off, and IsRuleContainer reads it with GetCustomAttribute<T>(), which searches the
    // base chain - so Resource, BaseDeviceControlsSettings and BaseGraphicsSettings opt their whole
    // families in whether or not a derived type says so. Reading the sources for the DECLARED
    // attribute says the opposite, and did: AudioGraphicsSettings and PostProcessingGraphicsSettings
    // were reported as an unvalidated gap on exactly that mistake, having been validated all along.
    //
    // The same inheritance is the generator's blind spot, which is what makes this folder's three
    // assertions worth more than the gap none of them currently finds: Roslyn's
    // ForAttributeWithMetadataName matches DECLARED attributes only, so a container that inherits
    // its marker is invisible to ValidationGenerator and silently keeps the reflective walk.

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

        // The reachability is RuleAnalyzer's own, deliberately: public instance properties that can
        // be both read and written, unwrapping a list's element, a dictionary's VALUE (never its
        // key) and an array's element - because those are the only three things the walk descends
        // into. Anything outside this assembly is a leaf here, since nothing outside it can carry
        // the marker in the first place.

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

        // THE INHERITANCE IS REAL AND IT IS THE GENERATOR'S BLIND SPOT. The walk reads the attribute
        // with GetCustomAttribute<T>(), which searches the base chain, so a derived type is a
        // container whether or not it says so. Roslyn's ForAttributeWithMetadataName matches
        // DECLARED attributes only, so a type in that state is invisible to ValidationGenerator and
        // silently keeps the reflective walk - correct, but not what anyone intended.
        //
        // So every container must DECLARE it. Two did not (AudioGraphicsSettings,
        // PostProcessingGraphicsSettings) and now do; this is what keeps the count at zero. Reading
        // the sources for the declared attribute and concluding those two were unvalidated is the
        // opposite mistake, and it was made once - they were validated the whole time.

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

        // EVERY MODEL OF THE FORMAT HAS A GENERATED WALK, and this is where that is said. It was a
        // generator diagnostic first (BHS1101, "a [RuleContainer] must be partial") and had to be
        // withdrawn: an analyzer cannot tell the format's own models from a test fixture, and dozens
        // of the private nested containers in this very folder are deliberately NOT partial, because
        // they are the only coverage RuleWalk.Node's reflective branch has.
        //
        // Scoping it to the SDK assembly is what makes the claim sayable. A type here that loses its
        // generated walk - a forgotten `partial`, a new model, a rename that breaks the attribute -
        // falls back to reflection silently and costs a level's load path what the generator was
        // written to remove.

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

        // WHAT TRANSITIVE PRUNING WOULD ACTUALLY BUY, measured rather than assumed - and the answer
        // is "almost nothing", which is why no runtime flag was added for it.
        //
        // Docs/issues/VALIDATION_GENERATOR_PLAN.md called this "the second order-of-magnitude" and
        // named transforms and keyframe payloads as rule-free branches to skip. They are not:
        // Keyframe.Frame is [RuleLevelFrame], Keyframe.Ease is [RuleEnumValid], and every value
        // model is range-checked. A least fixed point over the type graph - start from "no type has
        // rules", add a type once one of its own properties or one of its reachable subtypes has -
        // leaves a handful of LEAVES and no subtree at all.
        //
        // The step is still worth taking in the GENERATOR, where the answer is a const the compiler
        // folds away. It is not worth taking in the reflective walk, where consulting it costs the
        // very dictionary lookup it would be saving.

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
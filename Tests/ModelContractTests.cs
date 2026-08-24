using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BH.SDK.Models.Interfaces;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // A sweep rather than a file per model, because the thing being checked is the CONTRACT and
    // there are 180-odd implementations of it: one forgotten field in one Pull is exactly the kind
    // of defect a per-class test never gets written for. Two invariants are checkable without
    // knowing what any given model holds, and between them they cover what the two operations
    // actually promise:
    //
    //   Update leaves NOTHING aliased to its source - a nested model of the receiver may never be
    //   the same instance as the source's, or editing one silently edits the other.
    //   Pull keeps every nested instance it already had, which is the entire reason it exists
    //   beside Update.
    //
    // Both run over default-constructed pairs, which is enough: they ask about references, not
    // values. The aggregate round trips below are the other half - those DO need real data, so they
    // go through MockData and check the values instead.

    public class ModelContractTests
    {
        // Below this, the reflection filter has stopped matching and the sweep is passing vacuously.
        private const int MinimumSweptTypes = 150;

        #region Reflection helpers

        private static bool IsModelOf(Type type, Type argument) => type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IModel<>)
            && i.GetGenericArguments()[0] == argument);

        /// <summary> Every concrete model addressing IModel by its OWN type, i.e. everything the
        /// contract is total for. </summary>
        private static List<Type> ModelTypes()
        {
            var types = new List<Type>();
            foreach (var type in typeof(IModel<>).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) continue;
                if (type.Namespace == null || type.Namespace.Contains(".Versions.")) continue;
                if (!IsModelOf(type, type)) continue;
                if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null) continue;
                types.Add(type);
            }
            return types;
        }

        /// <summary> Properties holding another model by reference - the only ones either invariant
        /// has anything to say about. A struct model (FrameSpan) is copied by assignment, and a
        /// collection is replaced wholesale by both operations on purpose. </summary>
        private static List<PropertyInfo> NestedModelProperties(Type type) => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Where(p => !p.PropertyType.IsValueType && IsModelOf(p.PropertyType, p.PropertyType))
            .ToList();

        private static void Invoke(Type type, string name, object target, object argument)
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance,
                null, new[] { type }, null);
            Assert.IsNotNull(method, $"{type.Name} has no public {name}({type.Name})");
            method.Invoke(target, new[] { argument });
        }

        private static string Report(List<string> failures)
        {
            var builder = new StringBuilder();
            builder.Append(failures.Count).Append(" model(s) break the contract:");
            foreach (var failure in failures) builder.Append('\n').Append("  ").Append(failure);
            return builder.ToString();
        }

        #endregion

        #region The sweep

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryModel_ExposesUpdateAndPullForItsOwnType()
        {
            var types = ModelTypes();
            var failures = new List<string>();

            foreach (var type in types)
            {
                var argument = new[] { type };
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                if (type.GetMethod("Update", flags, null, argument, null) == null)
                    failures.Add($"{type.Name}: no public Update({type.Name})");
                if (type.GetMethod("Pull", flags, null, argument, null) == null)
                    failures.Add($"{type.Name}: no public Pull({type.Name})");
            }

            Assert.GreaterOrEqual(types.Count, MinimumSweptTypes, "the reflection filter matched almost nothing");
            Assert.IsEmpty(failures, Report(failures));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryModel_Update_LeavesNothingAliasedToTheSource()
        {
            var failures = new List<string>();
            var checkedProperties = 0;

            foreach (var type in ModelTypes())
            {
                var nested = NestedModelProperties(type);
                if (nested.Count == 0) continue;

                var target = Activator.CreateInstance(type);
                var source = Activator.CreateInstance(type);
                Invoke(type, "Update", target, source);

                foreach (var property in nested)
                {
                    var mine = property.GetValue(target);
                    var theirs = property.GetValue(source);
                    if (mine == null || theirs == null) continue;
                    checkedProperties++;
                    if (ReferenceEquals(mine, theirs))
                        failures.Add($"{type.Name}.{property.Name} is still the source's own instance");
                }
            }

            Assert.Greater(checkedProperties, 0, "no nested model property was reachable at all");
            Assert.IsEmpty(failures, Report(failures));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryModel_Pull_KeepsTheInstancesItAlreadyHad()
        {
            var failures = new List<string>();
            var checkedProperties = 0;

            foreach (var type in ModelTypes())
            {
                var nested = NestedModelProperties(type);
                if (nested.Count == 0) continue;

                var target = Activator.CreateInstance(type);
                var source = Activator.CreateInstance(type);
                var before = nested.ToDictionary(p => p, p => p.GetValue(target));
                Invoke(type, "Pull", target, source);

                foreach (var property in nested)
                {
                    var was = before[property];
                    if (was == null) continue;
                    var theirs = property.GetValue(source);

                    // An interface-typed field may only keep its instance while the concrete types
                    // agree; on a default-constructed pair they always do, so this stays a real check.
                    if (theirs != null && was.GetType() != theirs.GetType()) continue;

                    checkedProperties++;
                    if (!ReferenceEquals(was, property.GetValue(target)))
                        failures.Add($"{type.Name}.{property.Name} was replaced instead of pulled into");
                }
            }

            Assert.Greater(checkedProperties, 0, "no nested model property was reachable at all");
            Assert.IsEmpty(failures, Report(failures));
        }

        #endregion

        #region Aggregate round trips - the "did you forget a field" half

        // Update/Pull against a fully populated fixture, checked by the model's own Equals. Every
        // one of these walks a whole subtree, so a field missing from any Update or Pull under it
        // shows up here rather than as a value that quietly stops travelling.

        private static void AssertCarriesEverything<T>(T source) where T : class, IModel<T>, new()
        {
            var updated = new T();
            var pulled = new T();

            updated.Update(source);
            pulled.Pull(source);

            Assert.AreEqual(source, updated, $"{typeof(T).Name}.Update dropped something");
            Assert.AreEqual(source, pulled, $"{typeof(T).Name}.Pull dropped something");
            Assert.AreNotSame(source, updated);
            Assert.AreNotSame(source, pulled);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void Level_UpdateAndPull_CarryTheWholeAggregate()
            => AssertCarriesEverything(MockData.CreateTestLevel());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void LevelMeta_UpdateAndPull_CarryTheWholeAggregate()
            => AssertCarriesEverything(MockData.CreateTestLevelMeta());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void UserSettings_UpdateAndPull_CarryTheWholeAggregate()
            => AssertCarriesEverything(MockData.CreateValidTestSettings());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Prefab_UpdateAndPull_CarryTheWholeAggregate()
            => AssertCarriesEverything(MockData.CreateTestPrefab());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EffectData_UpdateAndPull_CarryTheWholeAggregate()
            => AssertCarriesEverything(MockData.CreateTestEffectData());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ThemeData_UpdateAndPull_CarryTheWholeAggregate()
            => AssertCarriesEverything(MockData.CreateTestTheme());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CompositeShape_UpdateAndPull_CarryTheWholeAggregate()
            => AssertCarriesEverything(MockData.CreateTestCompositeShape());

        #endregion

        #region Update agrees with Copy

        // Copy is what every Update and Pull body was derived from, so the two must not be able to
        // drift apart: whatever Copy carries, Update must carry too. This is the cheap total check -
        // it needs no fixture, so it runs over every model rather than the seven above.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryModel_UpdateAndPull_AgreeWithCopy()
        {
            var failures = new List<string>();

            foreach (var type in ModelTypes())
            {
                try
                {
                    var source = Activator.CreateInstance(type);
                    var copy = type.GetMethod("Copy", BindingFlags.Public | BindingFlags.Instance,
                        null, Type.EmptyTypes, null)?.Invoke(source, null);
                    if (copy == null) { failures.Add($"{type.Name}: no public Copy()"); continue; }

                    var updated = Activator.CreateInstance(type);
                    var pulled = Activator.CreateInstance(type);
                    Invoke(type, "Update", updated, source);
                    Invoke(type, "Pull", pulled, source);

                    if (!copy.Equals(updated)) failures.Add($"{type.Name}: Update does not agree with Copy");
                    if (!copy.Equals(pulled)) failures.Add($"{type.Name}: Pull does not agree with Copy");
                }
                catch (Exception exception)
                {
                    failures.Add($"{type.Name}: threw {(exception.InnerException ?? exception).GetType().Name}");
                }
            }

            Assert.IsEmpty(failures, Report(failures));
        }

        #endregion
    }
}

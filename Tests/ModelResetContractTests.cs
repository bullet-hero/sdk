using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BH.SDK.Models.Interfaces;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // WHAT RESET MEANS IS "BACK TO WHAT THE CONSTRUCTOR BUILT", and until now nothing said so. Every
    // Reset() here is hand-written and restates its constructor's defaults a second time -
    // `Active = true`, `ShapeId.Square.Fill`, `Array.Fill(Matrix, Color4Value.white)` - so the two
    // can drift, and a drift is invisible: a reset object simply is not the object a fresh one
    // would be, in a way no round-trip test can see.
    //
    // This sweep is what makes the constructor the single source of those defaults. It is also a
    // PRECONDITION of the generated Reset(): BH.SDK.Roslyn emits `Reset()` by assigning from a
    // prototype built by `new T()` rather than by restating literals, which is only equivalent to
    // today's behaviour where these two already agree. A failure here is a real defect either way -
    // one of the two copies is wrong, and a person has to say which.
    //
    // Collections are the one deliberate asymmetry and the reason this compares by value rather
    // than by reference: Reset CLEARS a collection in place while the constructor allocates a fresh
    // one. Both end up with the same contents, which is what the model contract promises.
    //
    // TWO EXEMPTIONS, AND BOTH ARE FACTS ABOUT C# RATHER THAN CONCESSIONS.
    //
    // A STRUCT has no parameterless constructor to be a source of truth for (C# 9), so `new T()` is
    // just zeroed memory and "back to what the constructor built" means nothing. ModificationKey is
    // the case: its Reset normalizes Path to string.Empty, which is BETTER than the default null it
    // would be compared against. Value-type models are skipped, and they are also the two types the
    // generator does not cover at all.
    //
    // A NON-DETERMINISTIC MEMBER cannot be compared at all: LevelMeta's constructor calls
    // LevelId.NewId(), so two fresh instances legitimately differ, and its Reset draws a new id
    // too - deliberately, since a reset level is a different level. Those members are found rather
    // than listed: two fresh instances are built and every property they disagree on is excluded.
    // That is also why the generated Reset() builds a FRESH instance instead of copying from one
    // shared prototype - a prototype would hand every reset the same id.

    public class ModelResetContractTests
    {
        // Below this, the reflection filter has stopped matching and the sweep passes vacuously.
        private const int MinimumSweptTypes = 150;

        private static bool IsModelOf(Type type, Type argument) => type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IModel<>)
            && i.GetGenericArguments()[0] == argument);

        /// <summary> Every concrete model addressing IModel by its OWN type and constructible with
        /// no arguments - the same filter ModelContractTests sweeps. </summary>
        private static List<Type> ModelTypes()
        {
            var types = new List<Type>();
            foreach (var type in typeof(IModel<>).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) continue;
                if (type.Namespace == null || type.Namespace.Contains(".Versions.")) continue;
                if (!IsModelOf(type, type)) continue;
                if (type.IsValueType) continue; // see the exemptions in the header
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                types.Add(type);
            }
            return types;
        }

        /// <summary> Everything a Reset has to answer for: the readable public properties. </summary>
        private static List<PropertyInfo> Members(Type type) => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToList();

        /// <summary> Equality that also holds for the collections a model exposes, which compare by
        /// reference on their own and would otherwise report every model as broken. </summary>
        private static bool ValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            if (left.Equals(right)) return true;

            if (left is System.Collections.IEnumerable leftItems &&
                right is System.Collections.IEnumerable rightItems)
            {
                var l = leftItems.Cast<object>().ToList();
                var r = rightItems.Cast<object>().ToList();
                return l.Count == r.Count && l.Zip(r, ValuesEqual).All(equal => equal);
            }

            return false;
        }

        private static string Report(List<string> failures)
        {
            var builder = new StringBuilder();
            builder.Append(failures.Count).Append(" model(s) break the contract:");
            foreach (var failure in failures) builder.Append('\n').Append("  ").Append(failure);
            return builder.ToString();
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryModel_Reset_RestoresWhatTheConstructorBuilt()
        {
            var types = ModelTypes();
            var failures = new List<string>();

            foreach (var type in types)
            {
                var method = type.GetMethod(nameof(IResetable.Reset),
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (method == null)
                {
                    failures.Add($"{type.Name}: no public Reset()");
                    continue;
                }

                var fresh = Activator.CreateInstance(type)!;
                var other = Activator.CreateInstance(type)!;
                var reset = Activator.CreateInstance(type)!;
                method.Invoke(reset, null);

                // Whole-object equality first - it covers every member, including any the property
                // walk below cannot see. Only when that fails is it worth naming which member.
                if (fresh.Equals(other) && fresh.Equals(reset)) continue;

                foreach (var property in Members(type))
                {
                    var freshValue = property.GetValue(fresh);
                    var otherValue = property.GetValue(other);
                    // Non-deterministic: two fresh instances already disagree, so Reset cannot be
                    // held to it. Found rather than listed, so a new one needs no edit here.
                    if (!ValuesEqual(freshValue, otherValue)) continue;

                    if (!ValuesEqual(freshValue, property.GetValue(reset)))
                        failures.Add($"{type.Name}.{property.Name}: Reset() leaves " +
                                     $"'{property.GetValue(reset) ?? "null"}' where the constructor " +
                                     $"builds '{freshValue ?? "null"}'");
                }
            }

            Assert.GreaterOrEqual(types.Count, MinimumSweptTypes, "the reflection filter matched almost nothing");
            Assert.IsEmpty(failures, Report(failures));
        }

        // THE OTHER HALF OF WHAT RESET PROMISES, and the half a value comparison cannot see: a
        // reset object is the SAME object, so whatever holds its nested models and its collections
        // still holds them. Every hand-written Reset here cleared in place and called Reset() on a
        // nested model for exactly that reason, and the generated one reproduces it through
        // ModelUtils.PullFrom and ResetTo rather than by assigning a fresh instance over the top.
        //
        // A POLYMORPHIC field is the deliberate exception and is skipped: a Vector2Rect cannot be
        // reset into the Vector2Value its constructor wanted, so the instance has to go.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryModel_Reset_KeepsTheInstancesItAlreadyHad()
        {
            var types = ModelTypes();
            var failures = new List<string>();

            foreach (var type in types)
            {
                var method = type.GetMethod(nameof(IResetable.Reset),
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (method == null) continue;

                var instance = Activator.CreateInstance(type)!;
                // string and System.Version are reference types with no mutable state, so the
                // generator treats them as values and identity says nothing about either.
                var before = Members(type)
                    .Where(p => p.PropertyType.IsClass
                                && p.PropertyType != typeof(string)
                                && p.PropertyType != typeof(Version))
                    .ToDictionary(p => p, p => p.GetValue(instance));

                method.Invoke(instance, null);

                foreach (var pair in before)
                {
                    if (pair.Value is null) continue;
                    // Only a field whose DECLARED type is concrete can promise identity.
                    if (pair.Key.PropertyType.IsInterface) continue;

                    if (!ReferenceEquals(pair.Value, pair.Key.GetValue(instance)))
                        failures.Add($"{type.Name}.{pair.Key.Name}: Reset() replaced the instance");
                }
            }

            Assert.GreaterOrEqual(types.Count, MinimumSweptTypes, "the reflection filter matched almost nothing");
            Assert.IsEmpty(failures, Report(failures));
        }
    }
}

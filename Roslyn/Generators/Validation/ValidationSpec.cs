using System;
using BH.SDK.Roslyn.Model;

namespace BH.SDK.Roslyn.Validation
{
    /// <summary> How a walkable property's value is descended into. </summary>
    internal enum DescentShape : byte
    {
        /// <summary> Not walkable: no [RuleContainer] can be under it, so it is never descended and
        /// - when it carries no rules either - never even read. </summary>
        None = 0,

        /// <summary> Declared List&lt;T&gt;. A subclass of it is still an IList and indexes the
        /// same way, so binding this statically is safe. </summary>
        List,

        /// <summary> Declared Dictionary&lt;K,V&gt;. Values only, never keys. </summary>
        Dictionary,

        Array,

        /// <summary> A sealed reference type that is provably not a collection, so exactly one node
        /// hangs off it. </summary>
        One,

        /// <summary> Anything whose declared type does not pin the shape - an interface, an open
        /// base, object. Its runtime value may be a list, so the dispatch stays where the reflective
        /// walk has it. </summary>
        Runtime,
    }

    /// <summary> One walked property, as the generator sees it. </summary>
    internal sealed class PropertySpec : IEquatable<PropertySpec>
    {
        /// <summary> One property as the walk sees it - whether it carries rules, and what descending into it means. </summary>
        public PropertySpec(string name, string owner, bool hasRules, DescentShape shape)
        {
            Name = name;
            Owner = owner;
            HasRules = hasRules;
            Shape = shape;
        }

        /// <summary> The type's own name. </summary>
        public string Name { get; }

        /// <summary> The declaring type's simple name. Half of the ordinal contract RuleTable
        /// checks: a hidden pair shares a name, and only the pair (owner, name) separates them.
        /// </summary>
        public string Owner { get; }

        /// <summary> True when rules sit on it, so phase A has something to run. </summary>
        public bool HasRules { get; }
        /// <summary> How phase B descends into it - a value, a list, a dictionary, or not at all. </summary>
        public DescentShape Shape { get; }

        /// <summary> True when phase B descends into it at all. </summary>
        public bool Walkable => Shape != DescentShape.None;

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
        public bool Equals(PropertySpec other) => other is not null
            && Name == other.Name && Owner == other.Owner
            && HasRules == other.HasRules && Shape == other.Shape;

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is PropertySpec other && Equals(other);

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
        public override int GetHashCode() => unchecked(
            Name.GetHashCode() * 397 ^ Owner.GetHashCode() ^ (HasRules ? 1 : 0) ^ ((int)Shape << 8));
    }

    /// <summary> One [RuleContainer] type's walk, as the generator sees it. </summary>
    internal sealed class ValidationSpec : IEquatable<ValidationSpec>
    {
        /// <summary> Everything the emitter needs, and deliberately no ISymbol. </summary>
        public ValidationSpec(string ns, string name, string accessibility, bool isSealed,
            bool isFrameScope, bool hasObjectRules, EquatableArray<PropertySpec> properties,
            string hintName)
        {
            Namespace = ns;
            Name = name;
            Accessibility = accessibility;
            IsSealed = isSealed;
            IsFrameScope = isFrameScope;
            HasObjectRules = hasObjectRules;
            Properties = properties;
            HintName = hintName;
        }

        /// <summary> Namespace the generated partial is written into. </summary>
        public string Namespace { get; }
        /// <summary> The type's own name. </summary>
        public string Name { get; }
        /// <summary> The declared accessibility, which the generated half has to match. </summary>
        public string Accessibility { get; }
        /// <summary> Whether the walk can be non-virtual. </summary>
        public bool IsSealed { get; }

        /// <summary> Whether entering this object rebases the scope. Resolved here rather than at
        /// the call site because the walk dispatches on the RUNTIME type: a member declared
        /// RectObject or IObjectScope can hold a scope, so only the callee knows. </summary>
        public bool IsFrameScope { get; }

        /// <summary> True when whole-object rules run before the properties. </summary>
        public bool HasObjectRules { get; }

        /// <summary> Flattened, DERIVED-FIRST, which is GetProperties' own order. A chain of
        /// per-level helpers would be the ModelEmitter habit and is wrong here twice over: that one
        /// calls its base FIRST, and the walk's two phases would have to thread the descent gate
        /// across every chain boundary. </summary>
        public EquatableArray<PropertySpec> Properties { get; }

        /// <summary> The generated file's name, unique across the compilation. </summary>
        public string HintName { get; }

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
        public bool Equals(ValidationSpec other) => other is not null
            && Namespace == other.Namespace && Name == other.Name
            && Accessibility == other.Accessibility && IsSealed == other.IsSealed
            && IsFrameScope == other.IsFrameScope && HasObjectRules == other.HasObjectRules
            && Properties.Equals(other.Properties) && HintName == other.HintName;

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is ValidationSpec other && Equals(other);

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
        public override int GetHashCode() => unchecked(
            (Namespace?.GetHashCode() ?? 0) * 397 ^ Name.GetHashCode()
            ^ Properties.GetHashCode() ^ (IsFrameScope ? 2 : 0) ^ (HasObjectRules ? 4 : 0));
    }
}

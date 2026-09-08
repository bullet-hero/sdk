using System;
using System.Collections.Generic;
using System.Linq;

namespace BH.SDK.Generators
{
    // A parameters class carries NO attributes: the SDK core would otherwise have to define UI
    // vocabulary it can't render, and every generator author would have to learn it. Everything a
    // host can work out from the field's own type (which editor to show, what to serialize) it works
    // out by reflection; everything it can't comes from here, as plain data.
    //
    // Order is mandatory rather than optional. Type.GetFields() order is explicitly unspecified by
    // the CLI - it happens to be declaration order on today's runtimes and is free to stop being
    // that after a recompile - so without Order the form's field order is a coin flip.
    //
    // Section is Order plus a header: it lists fields exactly like Order does and additionally says
    // which group they belong to, so grouping can never disagree with ordering the way two separate
    // mechanisms would. A generator states its sections in the order a form shows them, Main first.

    /// <summary>
    /// Presentation facts about a generator's parameter fields that the fields' own types can't
    /// carry. Keys are field names, always written through nameof so a rename is a compile error
    /// instead of a silently ignored hint.
    /// </summary>
    public sealed class GeneratorHints
    {
        /// <summary> Field names in the order a form shows them. Fields missing from here are shown
        /// after the listed ones, in reflection order - a test asserts every generator lists all of
        /// its own fields, so that fallback only ever covers a mistake. </summary>
        public IReadOnlyList<string> Order { get; }

        /// <summary> Field name -> the section key it was listed under. A field listed through plain
        /// Order instead of Section isn't in here at all - see SectionOf. </summary>
        public IReadOnlyDictionary<string, string> Sections { get; }

        /// <summary> Field name -> label key override. Without one, a host humanizes
        /// "{NameKey}_{field}" itself. </summary>
        public IReadOnlyDictionary<string, string> Labels { get; }

        /// <summary> Field name -> allowed numeric range. A host both clamps writes and configures
        /// its slider/field from this. </summary>
        public IReadOnlyDictionary<string, GeneratorRange> Ranges { get; }

        /// <summary> Field name -> drag/step granularity. </summary>
        public IReadOnlyDictionary<string, float> Steps { get; }

        /// <summary> Field name -> unit suffix shown after the value ("px", "deg", "frames"). </summary>
        public IReadOnlyDictionary<string, string> Units { get; }

        /// <summary> Field name -> predicate over the parameters instance. False hides the field
        /// entirely: irrelevant-in-this-mode fields, and every ExternalAnalysis input (which the
        /// host fills in, so showing it would invite the author to fight the host over it). </summary>
        public IReadOnlyDictionary<string, Func<object, bool>> Visible { get; }

        /// <summary> Field name -> the explicit set of values it accepts, for an int/string field
        /// that is really a closed choice but isn't an enum. </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<GeneratorChoice>> Choices { get; }

        /// <summary> Field names a host must show but must not let anyone edit. Distinct from
        /// Visible=false, which hides the field: this one is for a value the author needs to SEE to
        /// make sense of the field next to it (the level's current framerate beside the target one),
        /// filled in by the host and meaningless to type into. </summary>
        public IReadOnlyCollection<string> ReadOnly => _readOnly;

        private readonly HashSet<string> _readOnly;

        /// <summary> For a generator with no parameters at all. Not a "hints are optional" escape
        /// hatch - a generator WITH fields must list them in Order. </summary>
        public static readonly GeneratorHints Empty = new Builder().Build();

        private GeneratorHints(IReadOnlyList<string> order,
            IReadOnlyDictionary<string, string> sections,
            IReadOnlyDictionary<string, string> labels,
            IReadOnlyDictionary<string, GeneratorRange> ranges,
            IReadOnlyDictionary<string, float> steps,
            IReadOnlyDictionary<string, string> units,
            IReadOnlyDictionary<string, Func<object, bool>> visible,
            IReadOnlyDictionary<string, IReadOnlyList<GeneratorChoice>> choices,
            HashSet<string> readOnly)
        {
            Order = order;
            Sections = sections;
            Labels = labels;
            Ranges = ranges;
            Steps = steps;
            Units = units;
            Visible = visible;
            Choices = choices;
            _readOnly = readOnly;
        }

        /// <summary> The section a field belongs to. An unlisted field falls into
        /// GeneratorSections.Default rather than a group of its own. </summary>
        public string SectionOf(string field)
            => Sections.TryGetValue(field, out var section) ? section : GeneratorSections.Default;

        /// <summary> The bounds a host should clamp one field to, where any were declared. </summary>
        public bool TryGetRange(string field, out GeneratorRange range) => Ranges.TryGetValue(field, out range);
        /// <summary> The increment one field's control should move by. </summary>
        public bool TryGetStep(string field, out float step) => Steps.TryGetValue(field, out step);
        /// <summary> The localization key one field is labelled with, where it is not just its own name. </summary>
        public bool TryGetLabel(string field, out string label) => Labels.TryGetValue(field, out label);
        /// <summary> The unit shown beside one field. </summary>
        public bool TryGetUnit(string field, out string unit) => Units.TryGetValue(field, out unit);
        /// <summary> The fixed set one field is chosen from, where it is not a free number. </summary>
        public bool TryGetChoices(string field, out IReadOnlyList<GeneratorChoice> choices)
            => Choices.TryGetValue(field, out choices);

        /// <summary> Whether a field should currently be shown, given the live parameters instance.
        /// A field with no predicate is always visible. </summary>
        public bool IsVisible(string field, object parameters)
            => !Visible.TryGetValue(field, out var predicate) || predicate(parameters);

        /// <summary> Whether a host must render this field as a display-only value. </summary>
        public bool IsReadOnly(string field) => ReadOnly.Contains(field);

        /// <summary> Fluent construction, so a generator declares its hints as one readonly static
        /// initializer instead of assembling seven dictionaries by hand. </summary>
        public sealed class Builder
        {
            private readonly List<string> _order = new();
            private readonly Dictionary<string, string> _sections = new();
            private readonly Dictionary<string, string> _labels = new();
            private readonly Dictionary<string, GeneratorRange> _ranges = new();
            private readonly Dictionary<string, float> _steps = new();
            private readonly Dictionary<string, string> _units = new();
            private readonly Dictionary<string, Func<object, bool>> _visible = new();
            private readonly Dictionary<string, IReadOnlyList<GeneratorChoice>> _choices = new();
            private readonly HashSet<string> _readOnly = new();

            /// <summary> The order fields are laid out in; anything unnamed follows in declaration order. </summary>
            public Builder Order(params string[] fields)
            {
                _order.AddRange(fields);
                return this;
            }

            /// <summary> Lists fields exactly like Order does and puts them under one header. Call it
            /// more than once with the same key to add to that section - a spawning generator does
            /// exactly that, splicing SpawnParameters' shared fields in before its own. </summary>
            public Builder Section(string section, params string[] fields)
            {
                _order.AddRange(fields);
                foreach (var field in fields) _sections[field] = section;
                return this;
            }
            /// <summary> Labels one field with a localization key. </summary>
            public Builder Label(string field, string labelKey)
            {
                _labels[field] = labelKey;
                return this;
            }
            /// <summary> Bounds one field. </summary>
            public Builder Range(string field, float min, float max)
            {
                _ranges[field] = new GeneratorRange(min, max);
                return this;
            }
            /// <summary> Gives one field's control an increment. </summary>
            public Builder Step(string field, float step)
            {
                _steps[field] = step;
                return this;
            }
            /// <summary> Names the unit one field is measured in. </summary>
            public Builder Unit(string field, string unit)
            {
                _units[field] = unit;
                return this;
            }
            /// <summary> Shows one field only while the predicate holds of the parameters - which is how a form hides what a mode does not use. </summary>
            public Builder VisibleWhen(string field, Func<object, bool> predicate)
            {
                _visible[field] = predicate;
                return this;
            }
            /// <summary> Keeps one field out of the form entirely. </summary>
            public Builder Hidden(string field)
            {
                _visible[field] = _ => false;
                return this;
            }
            /// <summary> Turns one field into a fixed set of choices rather than a free number. </summary>
            public Builder Choice(string field, params GeneratorChoice[] choices)
            {
                _choices[field] = choices;
                return this;
            }

            /// <summary> Marks a field display-only: shown, never editable. Still needs listing in
            /// Order/Section like any other field - it is a real parameter, just not the author's. </summary>
            public Builder ReadOnly(params string[] fields)
            {
                foreach (var field in fields) _readOnly.Add(field);
                return this;
            }

            /// <summary> Freezes the hints; the builder is not reused afterwards. </summary>
            public GeneratorHints Build()
                => new(_order, _sections, _labels, _ranges, _steps, _units, _visible, _choices, _readOnly);
        }
    }

    /// <summary> The section vocabulary every shipped generator uses. Keys are label keys, never
    /// display strings - a host humanizes/localizes them exactly like a generator's NameKey. </summary>
    public static class GeneratorSections
    {
        /// <summary> What the generator is fundamentally driven by: the resources it spawns with and
        /// the few numbers deciding how much of what appears. An author who edits nothing else still
        /// gets a sensible result. </summary>
        public const string Main = "gen_section_main";

        /// <summary> Placement, timing, easing, per-mode switches - everything tuning a result the
        /// Main fields already define. </summary>
        public const string Additional = "gen_section_additional";

        /// <summary> Where a field listed through plain Order lands. </summary>
        public const string Default = Additional;
    }

    /// <summary> Inclusive numeric bounds for one parameter field. </summary>
    public readonly struct GeneratorRange
    {
        /// <summary> Lowest value the field accepts. </summary>
        public readonly float Min;
        /// <summary> Highest. </summary>
        public readonly float Max;

        /// <summary> Both bounds. </summary>
        public GeneratorRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        /// <summary> The value held inside the range. </summary>
        public float Clamp(float value) => value < Min ? Min : value > Max ? Max : value;
        /// <summary> The same for an int field. </summary>
        public int Clamp(int value) => value < Min ? (int)Min : value > Max ? (int)Max : value;

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"[{Min}, {Max}]";
    }

    /// <summary> One entry of a closed choice set - the stored value plus the key a host labels it
    /// with (never a display string: localization is a host concern, see GeneratorTextService). </summary>
    public readonly struct GeneratorChoice
    {
        /// <summary> The number this choice stands for. </summary>
        public readonly int Value;
        /// <summary> The localization key it is shown as. </summary>
        public readonly string LabelKey;

        /// <summary> One choice: what it means and what it reads as. </summary>
        public GeneratorChoice(int value, string labelKey)
        {
            Value = value;
            LabelKey = labelKey;
        }

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"{LabelKey}={Value}";
    }
}

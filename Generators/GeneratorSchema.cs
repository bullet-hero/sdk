using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Agent;
using BH.SDK.Models.Interfaces.Primitives;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Generators
{
    // Pure and host-free: the same fields and hints a form is reflected from, written as data a client that
    // cannot see C# can read. Visible is a delegate, so the schema states its answer for the DEFAULT values only.

    /// <summary> A generator's parameters as a JSON schema, and the reading of a caller's values back into
    /// a parameters instance with the hints applied. </summary>
    public static class GeneratorSchema
    {
        /// <summary> The key every schema lists its fields' names under, in form order. </summary>
        public const string OrderKey = "order";

        /// <summary> The schema of one generator. Fields are listed in <see cref="GeneratorHints.Order"/>
        /// order, then any field Order forgot, in reflection order. </summary>
        public static JObject Build(IGenerator generator)
        {
            var defaults = generator.CreateDefaultParameters();
            var properties = new JObject();
            var order = new JArray();

            foreach (var field in OrderedFields(generator))
            {
                order.Add(field.Name);
                properties[field.Name] = Describe(generator, field, defaults);
            }

            var schema = new JObject
            {
                ["id"] = generator.NameKey,
                ["kind"] = generator.Kind.ToString(),
                ["type"] = "object",
                [OrderKey] = order,
                ["properties"] = properties,
                ["additionalProperties"] = false,
            };

            var requirements = new JArray();
            foreach (GeneratorRequirements flag in Enum.GetValues(typeof(GeneratorRequirements)))
                if (flag != GeneratorRequirements.None && generator.Requirements.HasFlag(flag))
                    requirements.Add(flag.ToString());
            schema["requirements"] = requirements;
            schema["allowsEmptyRun"] = generator is IScopeGenerator scope && scope.AllowsEmptyRun;
            return schema;
        }

        /// <summary> The public instance fields of the parameters class in form order. </summary>
        public static IReadOnlyList<FieldInfo> OrderedFields(IGenerator generator)
        {
            var fields = generator.ParametersType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var byName = new Dictionary<string, FieldInfo>(fields.Length);
            foreach (var field in fields) byName[field.Name] = field;

            var result = new List<FieldInfo>(fields.Length);
            var emitted = new HashSet<string>();
            foreach (var name in generator.Hints.Order)
                if (byName.TryGetValue(name, out var field) && emitted.Add(name))
                    result.Add(field);

            foreach (var field in fields)
                if (emitted.Add(field.Name))
                    result.Add(field);

            return result;
        }

        /// <summary> Names of the fields whose <see cref="GeneratorHints.Visible"/> predicate answers false for
        /// <paramref name="parameters"/>: the values the caller actually gave, not the defaults. </summary>
        public static IReadOnlyList<string> HiddenFields(IGenerator generator, object parameters)
        {
            var hidden = new List<string>();
            foreach (var field in OrderedFields(generator))
                if (!generator.Hints.IsVisible(field.Name, parameters))
                    hidden.Add(field.Name);
            return hidden;
        }

        /// <summary> Writes the caller's values over <paramref name="parameters"/>. A number outside the
        /// field's range is clamped and reported in <paramref name="warnings"/>, as is a value for a field the
        /// host fills or that the given values hide. </summary>
        /// <exception cref="AgentValueException"> A name is not a parameter, a value has the wrong shape, or
        /// the field cannot be set from outside. </exception>
        public static void Apply(IGenerator generator, object parameters, JObject values, ICollection<string> warnings)
        {
            if (values == null) return;

            var fields = OrderedFields(generator);
            var given = new List<string>();

            foreach (var pair in values)
            {
                var field = fields.FirstOrDefault(f => f.Name == pair.Key)
                            ?? fields.FirstOrDefault(f => string.Equals(f.Name, pair.Key,
                                StringComparison.OrdinalIgnoreCase));
                var path = "parameters." + pair.Key;
                if (field == null)
                    throw new AgentValueException(path,
                        $"{generator.NameKey} has no parameter \"{pair.Key}\"; it has " +
                        $"{string.Join(", ", fields.Select(f => f.Name))}.");

                if (!IsSettable(field.FieldType))
                    throw new AgentValueException(path, $"{field.Name} is filled in by the editor and cannot be set.");

                if (generator.Hints.IsReadOnly(field.Name))
                {
                    warnings?.Add($"{field.Name} is read-only and was ignored.");
                    continue;
                }

                var value = AgentValueCodec.Decode(field.FieldType, pair.Value);
                value = Clamp(generator.Hints, field, value, warnings);
                if (!IsValidChoice(generator.Hints, field, value))
                    throw new AgentValueException(path,
                        $"{value} is not one of " +
                        $"{string.Join(", ", generator.Hints.Choices[field.Name].Select(c => $"{c.Value} ({c.LabelKey})"))}.");

                field.SetValue(parameters, value);
                given.Add(field.Name);
            }

            foreach (var name in given)
                if (!generator.Hints.IsVisible(name, parameters))
                    warnings?.Add($"{name} has no effect with these values: the generator hides it.");
        }

        private static JObject Describe(IGenerator generator, FieldInfo field, object defaults)
        {
            var hints = generator.Hints;
            var type = field.FieldType;
            var property = DescribeType(type);

            if (hints.TryGetRange(field.Name, out var range))
            {
                property["minimum"] = range.Min;
                property["maximum"] = range.Max;
            }

            if (hints.TryGetStep(field.Name, out var step)) property["x-step"] = step;
            if (hints.TryGetUnit(field.Name, out var unit) && !string.IsNullOrEmpty(unit)) property["x-unit"] = unit;

            if (hints.TryGetChoices(field.Name, out var choices))
            {
                property["enum"] = new JArray(choices.Select(c => (object)c.Value).ToArray());
                property["x-choices"] = new JArray(choices.Select(c => new JObject
                {
                    ["value"] = c.Value,
                    ["label"] = c.LabelKey,
                }));
            }

            property["x-section"] = hints.SectionOf(field.Name);
            property["x-readOnly"] = hints.IsReadOnly(field.Name);
            property["x-visible"] = hints.IsVisible(field.Name, defaults);

            if (IsSettable(type))
            {
                try
                {
                    property["default"] = AgentValueCodec.Encode(field.GetValue(defaults));
                }
                catch (AgentValueException)
                {
                    // No readable form: the field stays settable but states no default.
                }
            }

            return property;
        }

        private static JObject DescribeType(Type type)
        {
            if (type == typeof(bool)) return new JObject { ["type"] = "boolean" };
            if (type == typeof(int)) return new JObject { ["type"] = "integer" };
            if (type == typeof(uint)) return new JObject { ["type"] = "integer", ["minimum"] = 0 };
            if (type == typeof(float) || type == typeof(double)) return new JObject { ["type"] = "number" };
            if (type == typeof(string)) return new JObject { ["type"] = "string" };

            if (type.IsEnum)
                return new JObject { ["type"] = "string", ["enum"] = new JArray(Enum.GetNames(type)) };

            if (typeof(IPrimitiveInt).IsAssignableFrom(type))
                return new JObject { ["type"] = "integer", ["x-id"] = type.Name };

            if (typeof(IPrimitiveGuid).IsAssignableFrom(type))
                return new JObject { ["type"] = "string", ["format"] = "uuid", ["x-id"] = type.Name };

            if (AgentValueCodec.Families.Contains(type))
            {
                return new JObject
                {
                    ["type"] = "object",
                    ["x-family"] = type.Name,
                    ["x-kinds"] = new JArray(AgentValueCodec.ImplementationsOf(type).Select(t => t.Name)),
                };
            }

            if (!IsSettable(type)) return new JObject { ["x-hostFilled"] = type.Name };

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return new JObject
                {
                    ["type"] = "array",
                    ["items"] = DescribeType(type.GetGenericArguments()[0]),
                };
            }

            var members = new JObject();
            foreach (var member in AgentValueCodec.MembersOf(type))
                members[AgentValueCodec.MemberName(member)] = DescribeType(member.PropertyType);
            return new JObject { ["type"] = "object", ["properties"] = members };
        }

        // Anything the codec cannot decode is data the host supplies (decoded audio, pixels, beat frames).
        private static bool IsSettable(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return true;
            if (typeof(IPrimitiveInt).IsAssignableFrom(type) || typeof(IPrimitiveGuid).IsAssignableFrom(type))
                return true;
            if (AgentValueCodec.Families.Contains(type)) return true;
            if (type.IsArray) return false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return IsSettable(type.GetGenericArguments()[0]);
            return type.IsClass && !type.IsAbstract && AgentValueCodec.MembersOf(type).Count > 0;
        }

        private static object Clamp(GeneratorHints hints, FieldInfo field, object value, ICollection<string> warnings)
        {
            if (!hints.TryGetRange(field.Name, out var range)) return value;

            object clamped;
            if (value is int i) clamped = range.Clamp(i);
            else if (value is uint u) clamped = (uint)Math.Max(0, range.Clamp((int)Math.Min(u, int.MaxValue)));
            else if (value is float f) clamped = range.Clamp(f);
            else return value;

            if (!clamped.Equals(value))
                warnings?.Add($"{field.Name} {value} is outside {range}; clamped to {clamped}.");
            return clamped;
        }

        private static bool IsValidChoice(GeneratorHints hints, FieldInfo field, object value)
        {
            if (!hints.TryGetChoices(field.Name, out var choices) || !(value is int number)) return true;
            return choices.Any(c => c.Value == number);
        }
    }
}

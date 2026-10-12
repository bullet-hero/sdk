using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent
{
    /// <summary> A value the codec cannot read or write; <see cref="Path"/> says where in the value.
    /// </summary>
    public sealed class AgentValueException : Exception
    {
        /// <summary> Where the problem is, such as <c>value.color4BL</c>. </summary>
        public string Path { get; }

        /// <summary> A failure at <paramref name="path"/>. </summary>
        public AgentValueException(string path, string message) : base($"{path}: {message}")
        {
            Path = path;
        }
    }

    /// <summary>
    /// The readable form of the polymorphic values: <c>{ "kind": "Vector2Rect", "minX": 0, ... }</c> instead
    /// of the file's <c>[typeEnum, payload]</c>, found by reflection so a new value type needs no edit here.
    /// </summary>
    public static class AgentValueCodec
    {
        /// <summary> The member naming the implementation of a family. </summary>
        public const string KindKey = "kind";

        /// <summary> The member of a theme reference: <c>{ "theme": 3 }</c>. </summary>
        public const string ThemeKey = "theme";

        private const string RootPath = "value";

        /// <summary> Every polymorphic family the codec writes with a <c>kind</c>. </summary>
        public static IReadOnlyList<Type> Families { get; } = new[]
        {
            typeof(IFloat), typeof(IInt), typeof(IVector2), typeof(IVector3), typeof(IVector4),
            typeof(IColor3), typeof(IColor4), typeof(IString), typeof(IScreenLimit), typeof(ILicense),
            typeof(IColor4X4Key),
            typeof(IEffectAngle), typeof(IEffectColor), typeof(IEffectScale), typeof(IEffectShape),
            typeof(IEffectShapeSpread),
        };

        private static readonly object Gate = new object();
        private static readonly Dictionary<Type, Type[]> Implementations = new Dictionary<Type, Type[]>();
        private static readonly Dictionary<Type, PropertyInfo[]> Members = new Dictionary<Type, PropertyInfo[]>();

        /// <summary> The concrete classes of a family, by name. </summary>
        public static IReadOnlyList<Type> ImplementationsOf(Type family)
        {
            lock (Gate)
            {
                if (Implementations.TryGetValue(family, out var found)) return found;

                found = typeof(IFloat).Assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && family.IsAssignableFrom(t)
                                && t.GetConstructor(Type.EmptyTypes) != null)
                    .OrderBy(t => t.Name, StringComparer.Ordinal)
                    .ToArray();
                Implementations[family] = found;
                return found;
            }
        }

        /// <summary> The members the codec writes: public get/set properties carrying <c>[JsonProperty]</c>,
        /// base class first, minus <c>Keyframe.Frame</c> (a global frame is the caller's to give). </summary>
        public static IReadOnlyList<PropertyInfo> MembersOf(Type type)
        {
            lock (Gate)
            {
                if (Members.TryGetValue(type, out var found)) return found;

                var chain = new List<Type>();
                for (var t = type; t != null && t != typeof(object); t = t.BaseType) chain.Insert(0, t);

                found = chain
                    .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance
                                                                         | BindingFlags.DeclaredOnly))
                    .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                                && p.GetCustomAttribute<JsonPropertyAttribute>() != null
                                && !IsKeyframeFrame(p))
                    .ToArray();
                Members[type] = found;
                return found;
            }
        }

        /// <summary> The name a member has in the readable form: its C# name in camelCase. </summary>
        public static string MemberName(PropertyInfo property)
        {
            var name = property.Name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary> A value in its readable form. </summary>
        /// <exception cref="AgentValueException"> The value has a part with no readable form. </exception>
        public static JToken Encode(object value) => Encode(value, RootPath);

        /// <summary> A readable value back into a model value of type <typeparamref name="T"/>. </summary>
        /// <exception cref="AgentValueException"> The token is not a <typeparamref name="T"/>. </exception>
        public static T Decode<T>(JToken token) => (T)Decode(typeof(T), token);

        /// <summary> A readable value back into a model value of <paramref name="target"/>. </summary>
        /// <exception cref="AgentValueException"> The token is not a <paramref name="target"/>. </exception>
        public static object Decode(Type target, JToken token) => Decode(target, token, RootPath);

        private static JToken Encode(object value, string path)
        {
            switch (value)
            {
                case null: return JValue.CreateNull();
                case Color4Value c4: return new JValue(FormatHex(c4.R, c4.G, c4.B, c4.A));
                case Color3Value c3: return new JValue(FormatHex(c3.R, c3.G, c3.B, null));
                case Color4ThemeRef t4: return new JObject { [ThemeKey] = t4.ThemeColorIndex };
                case Color3ThemeRef t3: return new JObject { [ThemeKey] = t3.ThemeColorIndex };
                case string s: return new JValue(s);
                case bool or int or uint or long or float or double: return new JValue(value);
                case Guid g: return new JValue(g.ToString());
                case Enum e: return new JValue(e.ToString());
                case IPrimitiveInt i: return new JValue(i.Value);
                case IPrimitiveGuid g: return new JValue(g.Value.ToString());
                case IEnumerable list:
                {
                    var array = new JArray();
                    var index = 0;
                    foreach (var item in list) array.Add(Encode(item, $"{path}[{index++}]"));
                    return array;
                }
            }

            var type = value.GetType();
            if (!type.IsClass)
                throw new AgentValueException(path, $"{type.Name} has no readable form.");

            var obj = new JObject();
            if (Families.Any(f => f.IsAssignableFrom(type))) obj[KindKey] = type.Name;
            foreach (var member in MembersOf(type))
            {
                var name = MemberName(member);
                obj[name] = Encode(member.GetValue(value), $"{path}.{name}");
            }

            return obj;
        }

        private static object Decode(Type target, JToken token, string path)
        {
            var nullable = Nullable.GetUnderlyingType(target);
            if (token == null || token.Type == JTokenType.Null)
            {
                if (target.IsValueType && nullable == null)
                    throw new AgentValueException(path, $"null is not a {target.Name}.");
                return null;
            }

            if (nullable != null) target = nullable;

            if (token.Type == JTokenType.String && IsColorTarget(target, out var withAlpha))
                return ParseHex(path, token.Value<string>(), withAlpha);

            if (target == typeof(string))
                return token.Type == JTokenType.String
                    ? token.Value<string>()
                    : throw new AgentValueException(path, $"expected text, got {token.Type}.");

            if (target == typeof(bool))
                return token.Type == JTokenType.Boolean
                    ? token.Value<bool>()
                    : throw new AgentValueException(path, $"expected true or false, got {token.Type}.");

            if (target == typeof(int) || target == typeof(uint) || target == typeof(long))
                return DecodeInteger(target, token, path);

            if (target == typeof(float) || target == typeof(double))
            {
                if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
                    throw new AgentValueException(path, $"expected a number, got {token.Type}.");
                return target == typeof(float) ? (object)token.Value<float>() : token.Value<double>();
            }

            if (target == typeof(Guid) || typeof(IPrimitiveGuid).IsAssignableFrom(target))
            {
                if (token.Type != JTokenType.String || !Guid.TryParse(token.Value<string>(), out var guid))
                    throw new AgentValueException(path, "expected a guid string.");
                return target == typeof(Guid) ? guid : Activator.CreateInstance(target, guid);
            }

            if (typeof(IPrimitiveInt).IsAssignableFrom(target))
                return Activator.CreateInstance(target, DecodeInteger(typeof(int), token, path));

            if (target.IsEnum) return DecodeEnum(target, token, path);

            if (target.IsGenericType && target.GetGenericTypeDefinition() == typeof(List<>))
                return DecodeList(target, token, path);

            // A bare number is the literal of a numeric family: 3 reads as { "kind": "FloatValue", "value": 3 }.
            if (target == typeof(IFloat) && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer))
                return new FloatValue(token.Value<float>());
            if (target == typeof(IInt) && token.Type == JTokenType.Integer)
                return new IntValue((int)DecodeInteger(typeof(int), token, path));

            if (token.Type != JTokenType.Object)
                throw new AgentValueException(path, $"expected an object for {target.Name}, got {token.Type}.");

            return DecodeObject(target, (JObject)token, path);
        }

        private static object DecodeInteger(Type target, JToken token, string path)
        {
            if (token.Type != JTokenType.Integer)
                throw new AgentValueException(path, $"expected a whole number, got {token.Type}.");

            try
            {
                if (target == typeof(int)) return token.Value<int>();
                if (target == typeof(uint)) return token.Value<uint>();
                return token.Value<long>();
            }
            catch (OverflowException)
            {
                throw new AgentValueException(path, $"{token} does not fit {target.Name}.");
            }
        }

        private static object DecodeEnum(Type target, JToken token, string path)
        {
            if (token.Type == JTokenType.String && Enum.IsDefined(target, token.Value<string>()))
                return Enum.Parse(target, token.Value<string>());

            if (token.Type == JTokenType.String)
            {
                var match = Enum.GetNames(target).FirstOrDefault(n =>
                    string.Equals(n, token.Value<string>(), StringComparison.OrdinalIgnoreCase));
                if (match != null) return Enum.Parse(target, match);
            }
            else if (token.Type == JTokenType.Integer && Enum.IsDefined(target, token.Value<int>()))
            {
                return Enum.ToObject(target, token.Value<int>());
            }

            throw new AgentValueException(path,
                $"{token} is not a {target.Name}; use one of {string.Join(", ", Enum.GetNames(target))}.");
        }

        private static object DecodeList(Type target, JToken token, string path)
        {
            if (token.Type != JTokenType.Array)
                throw new AgentValueException(path, $"expected a list, got {token.Type}.");

            var list = (IList)Activator.CreateInstance(target);
            var element = target.GetGenericArguments()[0];
            var index = 0;
            foreach (var item in (JArray)token) list.Add(Decode(element, item, $"{path}[{index++}]"));
            return list;
        }

        private static object DecodeObject(Type target, JObject obj, string path)
        {
            var concrete = ResolveType(target, obj, path);

            if (concrete == typeof(Color4ThemeRef) || concrete == typeof(Color3ThemeRef))
            {
                var index = (int)DecodeInteger(typeof(int), obj[ThemeKey], $"{path}.{ThemeKey}");
                if (index < ValueRules.MinThemeIndex || index > ValueRules.MaxThemeIndex)
                    throw new AgentValueException($"{path}.{ThemeKey}",
                        $"{index} is outside {ValueRules.MinThemeIndex}-{ValueRules.MaxThemeIndex}.");
                return Activator.CreateInstance(concrete, index);
            }

            var members = MembersOf(concrete);
            var result = Activator.CreateInstance(concrete);
            foreach (var pair in obj)
            {
                if (pair.Key == KindKey) continue;

                var member = members.FirstOrDefault(m => string.Equals(MemberName(m), pair.Key,
                    StringComparison.OrdinalIgnoreCase));
                if (member == null)
                    throw new AgentValueException($"{path}.{pair.Key}",
                        $"{concrete.Name} has no member \"{pair.Key}\"; it has " +
                        $"{string.Join(", ", members.Select(MemberName))}.");

                member.SetValue(result, Decode(member.PropertyType, pair.Value, $"{path}.{MemberName(member)}"));
            }

            return result;
        }

        private static Type ResolveType(Type target, JObject obj, string path)
        {
            var kind = obj[KindKey]?.Value<string>();

            if (!target.IsInterface && !target.IsAbstract)
            {
                if (kind != null && !string.Equals(kind, target.Name, StringComparison.OrdinalIgnoreCase))
                    throw new AgentValueException($"{path}.{KindKey}", $"\"{kind}\" is not a {target.Name}.");
                return target;
            }

            if (kind == null && obj.ContainsKey(ThemeKey))
            {
                if (target == typeof(IColor4)) return typeof(Color4ThemeRef);
                if (target == typeof(IColor3)) return typeof(Color3ThemeRef);
            }

            var options = ImplementationsOf(target);
            if (kind == null)
                throw new AgentValueException(path,
                    $"a {target.Name} needs \"{KindKey}\": one of {string.Join(", ", options.Select(t => t.Name))}.");

            var match = options.FirstOrDefault(t => string.Equals(t.Name, kind, StringComparison.OrdinalIgnoreCase));
            return match ?? throw new AgentValueException($"{path}.{KindKey}",
                $"\"{kind}\" is not a {target.Name}; use one of {string.Join(", ", options.Select(t => t.Name))}.");
        }

        private static bool IsKeyframeFrame(PropertyInfo property)
            => property.Name == nameof(IFrame.Frame) && typeof(IFrame).IsAssignableFrom(property.DeclaringType);

        private static bool IsColorTarget(Type target, out bool withAlpha)
        {
            withAlpha = target == typeof(IColor4) || target == typeof(Color4Value);
            return withAlpha || target == typeof(IColor3) || target == typeof(Color3Value);
        }

        private static string FormatHex(float r, float g, float b, float? a)
        {
            var text = "#" + Byte(r).ToString("X2") + Byte(g).ToString("X2") + Byte(b).ToString("X2");
            return a.HasValue ? text + Byte(a.Value).ToString("X2") : text;
        }

        private static int Byte(float channel) => (int)Math.Round(Math.Min(1f, Math.Max(0f, channel)) * 255f);

        private static object ParseHex(string path, string text, bool withAlpha)
        {
            var digits = text.Length > 0 && text[0] == '#' ? text.Substring(1) : null;
            var ok = digits != null && (digits.Length == 6 || (withAlpha && digits.Length == 8))
                                    && digits.All(Uri.IsHexDigit);
            if (!ok)
                throw new AgentValueException(path,
                    $"\"{text}\" is not a colour; write {(withAlpha ? "\"#RRGGBBAA\" or " : "")}\"#RRGGBB\".");

            float Channel(int index) => int.Parse(digits.Substring(index * 2, 2), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture) / 255f;

            if (!withAlpha) return new Color3Value(Channel(0), Channel(1), Channel(2));
            return new Color4Value(Channel(0), Channel(1), Channel(2), digits.Length == 8 ? Channel(3) : 1f);
        }
    }
}
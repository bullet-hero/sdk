using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Agent;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The readable value form: every implementation of every family round-trips; a new SDK value
    /// type is covered the day it is added. </summary>
    [TestFixture]
    public class AgentValueCodecTests
    {
        private int _counter;

        public static IEnumerable<TestCaseData> AllImplementations()
        {
            foreach (var family in AgentValueCodec.Families)
            foreach (var type in AgentValueCodec.ImplementationsOf(family))
                yield return new TestCaseData(family, type).SetName($"{family.Name}.{type.Name}");
        }

        #region Every implementation

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryFamily_HasImplementations_WithUniqueKinds()
        {
            foreach (var family in AgentValueCodec.Families)
            {
                var names = AgentValueCodec.ImplementationsOf(family).Select(t => t.Name).ToList();
                CollectionAssert.IsNotEmpty(names, family.Name);
                CollectionAssert.AllItemsAreUnique(names, family.Name);
            }
        }

        [TestCaseSource(nameof(AllImplementations))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Implementation_PopulatedInstance_RoundTripsThroughText(Type family, Type type)
        {
            var original = Populate(type, 0);

            var encoded = AgentValueCodec.Encode(original);
            if (!IsColor(type)) Assert.AreEqual(type.Name, (string)encoded[AgentValueCodec.KindKey]);

            var wire = JToken.Parse(encoded.ToString(Newtonsoft.Json.Formatting.None));
            var decoded = AgentValueCodec.Decode(family, wire);

            Assert.IsInstanceOf(type, decoded);
            Assert.IsTrue(JToken.DeepEquals(encoded, AgentValueCodec.Encode(decoded)), encoded.ToString());
            Assert.AreEqual(original, decoded, encoded.ToString());
        }

        [TestCaseSource(nameof(AllImplementations))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Implementation_DefaultInstance_RoundTrips(Type family, Type type)
        {
            var original = Activator.CreateInstance(type);

            var decoded = AgentValueCodec.Decode(family, JToken.Parse(AgentValueCodec.Encode(original).ToString(Newtonsoft.Json.Formatting.None)));

            Assert.AreEqual(original, decoded);
        }

        #endregion

        #region Shapes of the readable form

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Vector_WritesKindAndCamelCaseMembers()
        {
            var json = (JObject)AgentValueCodec.Encode(new Vector2Rect(0f, 1f, 2f, 3f));

            Assert.AreEqual("Vector2Rect", (string)json["kind"]);
            Assert.AreEqual(1f, (float)json["minY"]);
            Assert.AreEqual(3f, (float)json["maxY"]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Decode_KindAndMemberNames_AreCaseInsensitive()
        {
            var value = AgentValueCodec.Decode<IVector2>(JObject.Parse("{\"kind\":\"vector2value\",\"X\":4,\"y\":5}"));

            Assert.IsInstanceOf<Vector2Value>(value);
            Assert.AreEqual(new Vector2Value(4f, 5f), value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Decode_MissingMember_KeepsTheConstructorDefault()
        {
            var value = (Vector2Rect)AgentValueCodec.Decode<IVector2>(
                JObject.Parse("{\"kind\":\"Vector2Rect\",\"minX\":2}"));

            Assert.AreEqual(2f, value.MinX);
            Assert.AreEqual(new Vector2Rect().MaxX, value.MaxX);
        }

        #endregion

        #region Colours and theme references

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Color4_IsAnEightDigitString()
        {
            var json = AgentValueCodec.Encode(new Color4Value(1f, 0f, 128f / 255f, 64f / 255f));

            Assert.AreEqual(JTokenType.String, json.Type);
            Assert.AreEqual("#FF008040", (string)json);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Color3_IsASixDigitString()
        {
            Assert.AreEqual("#00FF80", (string)AgentValueCodec.Encode(new Color3Value(0f, 1f, 128f / 255f)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Color_Decode_AcceptsBothLengthsForColor4_AndSixForColor3()
        {
            Assert.AreEqual(new Color4Value(1f, 0f, 0f, 1f), AgentValueCodec.Decode<IColor4>(new JValue("#ff0000")));
            Assert.AreEqual(new Color4Value(1f, 0f, 0f, 0f), AgentValueCodec.Decode<IColor4>(new JValue("#FF000000")));
            Assert.AreEqual(new Color3Value(0f, 1f, 0f), AgentValueCodec.Decode<IColor3>(new JValue("#00FF00")));
        }

        [TestCase("red")]
        [TestCase("#FFF")]
        [TestCase("#GG0000")]
        [TestCase("#FF000000FF")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Color_Decode_BadString_Fails(string text)
        {
            var ex = Assert.Throws<AgentValueException>(() => AgentValueCodec.Decode<IColor4>(new JValue(text)));
            StringAssert.Contains(text, ex.Message);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Color3_Decode_RefusesAnAlphaString()
        {
            Assert.Throws<AgentValueException>(() => AgentValueCodec.Decode<IColor3>(new JValue("#00FF0080")));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ThemeRef_IsAThemeObject_ForBothFamilies()
        {
            var four = AgentValueCodec.Encode(new Color4ThemeRef(5));
            var three = AgentValueCodec.Encode(new Color3ThemeRef(7));

            Assert.AreEqual("{\"theme\":5}", four.ToString(Newtonsoft.Json.Formatting.None));
            Assert.AreEqual("{\"theme\":7}", three.ToString(Newtonsoft.Json.Formatting.None));
            Assert.AreEqual(new Color4ThemeRef(5), AgentValueCodec.Decode<IColor4>(four));
            Assert.AreEqual(new Color3ThemeRef(7), AgentValueCodec.Decode<IColor3>(three));
        }

        [TestCase(ValueRules.MinThemeIndex - 1)]
        [TestCase(ValueRules.MaxThemeIndex + 1)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ThemeRef_OutsideTheThemeRange_Fails(int index)
        {
            var ex = Assert.Throws<AgentValueException>(() =>
                AgentValueCodec.Decode<IColor4>(new JObject { ["theme"] = index }));
            StringAssert.Contains(index.ToString(), ex.Message);
        }

        #endregion

        #region Nested values and keyframes

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Color4X4Key_NestsFourColours_AndRoundTrips()
        {
            var original = new Color4X4Key(
                new Color4Value(1f, 0f, 0f, 1f), new Color4ThemeRef(3),
                new Color4Lerp(0f, 0f, 0f, 1f, 1f, 1f, 1f, 1f), new Color4Value(0f, 0f, 1f, 128f / 255f),
                25, EaseType.Linear);

            var json = (JObject)AgentValueCodec.Encode(original);

            Assert.AreEqual("#FF0000FF", (string)json["color4BL"]);
            Assert.AreEqual(3, (int)json["color4BR"]["theme"]);
            Assert.AreEqual("Color4Lerp", (string)json["color4TL"]["kind"]);
            Assert.AreEqual("#0000FF80", (string)json["color4TR"]);

            var decoded = AgentValueCodec.Decode<IColor4X4Key>(JToken.Parse(json.ToString()));
            Assert.IsInstanceOf<Color4X4Key>(decoded);
            Assert.AreEqual(original.Color4BR, ((Color4X4Key)decoded).Color4BR);
            Assert.AreEqual(original.Color4TL, ((Color4X4Key)decoded).Color4TL);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Keyframe_FrameIsNeitherWritten_NorRead()
        {
            var key = new Color4Key(new Color4Value(1f, 1f, 1f, 1f), 77, EaseType.Linear);

            var json = (JObject)AgentValueCodec.Encode(key);

            Assert.IsFalse(json.ContainsKey("frame"), json.ToString());
            Assert.IsTrue(json.ContainsKey("ease"));

            json["frame"] = 99;
            Assert.Throws<AgentValueException>(() => AgentValueCodec.Decode<IColor4X4Key>(json));

            json.Remove("frame");
            Assert.AreEqual(new Color4Key().Frame, ((Color4Key)AgentValueCodec.Decode<IColor4X4Key>(json)).Frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void StringLocalized_ListOfLanguages_RoundTrips()
        {
            var original = new StringLocalized(new StringLanguage("en", "Hello"), new StringLanguage("ru", "Privet"));

            var json = (JObject)AgentValueCodec.Encode(original);

            Assert.AreEqual(2, json["strings"].Count());
            Assert.AreEqual(original, AgentValueCodec.Decode<IString>(json));
        }

        #endregion

        #region Errors

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Decode_UnknownKind_NamesTheValidOnes()
        {
            var ex = Assert.Throws<AgentValueException>(() =>
                AgentValueCodec.Decode<IFloat>(JObject.Parse("{\"kind\":\"Nope\"}")));

            StringAssert.Contains("Nope", ex.Message);
            StringAssert.Contains(nameof(FloatValue), ex.Message);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Decode_MissingKind_NamesTheValidOnes()
        {
            var ex = Assert.Throws<AgentValueException>(() => AgentValueCodec.Decode<IFloat>(JObject.Parse("{}")));

            StringAssert.Contains("kind", ex.Message);
            StringAssert.Contains(nameof(FloatMinMax), ex.Message);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Decode_UnknownMember_NamesItsPath()
        {
            var ex = Assert.Throws<AgentValueException>(() => AgentValueCodec.Decode<IVector2>(
                JObject.Parse("{\"kind\":\"Vector2Value\",\"z\":1}")));

            Assert.AreEqual("value.z", ex.Path);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Decode_WrongMemberType_NamesItsPath()
        {
            var ex = Assert.Throws<AgentValueException>(() => AgentValueCodec.Decode<IVector2>(
                JObject.Parse("{\"kind\":\"Vector2Value\",\"x\":\"far\"}")));

            Assert.AreEqual("value.x", ex.Path);
        }

        #endregion

        #region Populating

        private object Populate(Type type, int depth)
        {
            if (type == typeof(float)) return Next() / 255f;
            if (type == typeof(int)) return Next();
            if (type == typeof(uint)) return (uint)Next();
            if (type == typeof(bool)) return true;
            if (type == typeof(string)) return "s" + Next();
            if (type.IsEnum) return PickEnum(type);
            if (typeof(IPrimitiveInt).IsAssignableFrom(type)) return Activator.CreateInstance(type, Next());
            if (typeof(IPrimitiveGuid).IsAssignableFrom(type))
                return Activator.CreateInstance(type, new Guid(Next(), 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = (IList)Activator.CreateInstance(type);
                list.Add(Populate(type.GetGenericArguments()[0], depth + 1));
                list.Add(Populate(type.GetGenericArguments()[0], depth + 1));
                return list;
            }

            if (type.IsInterface || type.IsAbstract)
            {
                var options = AgentValueCodec.ImplementationsOf(type);
                var pick = depth >= 3
                    ? options.FirstOrDefault(t => t.Name.EndsWith("Value")) ?? options[0]
                    : options[Next() % options.Count];
                return Populate(pick, depth + 1);
            }

            var instance = Activator.CreateInstance(type);
            foreach (var member in AgentValueCodec.MembersOf(type))
            {
                // A theme slot is a 0..63 index; anything else is refused on the way back in.
                var value = member.Name == nameof(Color4ThemeRef.ThemeColorIndex)
                    ? Next() % (ValueRules.MaxThemeIndex + 1)
                    : Populate(member.PropertyType, depth + 1);
                member.SetValue(instance, value);
            }

            return instance;
        }

        private int Next() => (_counter++ % 200) + 1;

        private static object PickEnum(Type type)
        {
            var values = Enum.GetValues(type).Cast<object>().ToList();
            var zero = Activator.CreateInstance(type);
            return values.FirstOrDefault(v => !v.Equals(zero)) ?? values[0];
        }

        private static bool IsColor(Type type) => type == typeof(Color4Value) || type == typeof(Color3Value)
                                                                              || type == typeof(Color4ThemeRef) ||
                                                                              type == typeof(Color3ThemeRef);

        #endregion
    }
}
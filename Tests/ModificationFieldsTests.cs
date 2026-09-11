using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // A HAND-WRITTEN NUMBER IS CHECKED TWICE, from both sides, and neither check makes the other
    // redundant. BH.SDK.Roslyn refuses a collision at compile time but sees only the members it is
    // handed - a constant declared here and marking nothing is invisible to it. This fixture reads
    // the table itself, so a number that collides with one no member has taken yet is caught the
    // day it is written rather than the day a member takes it.
    //
    // Modelled on Core's RandomTracksTests, which exists for the regression this is meant to
    // prevent: two bands were declared on top of each other, nothing reported it, and the defect
    // stayed latent until the two families would have shared a source. Reflection rather than a
    // hand-written list, because a hand-written list is what failed to notice the first time.

    /// <summary> The field-id table read back from itself - uniqueness, bands, and the one number that means "no field". </summary>
    public class ModificationFieldsTests
    {
        /// <summary> The five bands ModificationFields' own header declares. </summary>
        private static readonly int[] DeclaredBands = { 0x01, 0x02, 0x03, 0x04, 0x05 };

        private static IEnumerable<FieldInfo> Constants() =>
            typeof(ModificationFields)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(field => field.IsLiteral && field.FieldType == typeof(int));

        private static Dictionary<string, int> Fields() =>
            Constants()
                .Where(field => field.Name != nameof(ModificationFields.None))
                .ToDictionary(field => field.Name, field => (int)field.GetRawConstantValue());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryFieldId_IsUnique()
        {
            var collisions = Fields()
                .GroupBy(field => field.Value)
                .Where(group => group.Count() > 1)
                .Select(group => $"0x{group.Key:X4}: {string.Join(", ", group.Select(f => f.Key))}")
                .ToArray();

            Assert.IsEmpty(collisions, "Two fields sharing an id means one override addresses both, " +
                                       "and which one it reaches is whichever the table answers " +
                                       "first:\n" + string.Join("\n", collisions));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // A mistyped digit is the failure this catches - 0x2005 for 0x0205 reads almost the same and
        // lands in a band no declaring type owns, where nothing else would ever notice it.
        public void EveryFieldId_SitsInADeclaredBand()
        {
            var strays = Fields()
                .Where(field => !DeclaredBands.Contains((field.Value >> 8) & 0xFF)
                                || (field.Value & 0xFF) == 0
                                || field.Value > 0xFFFF)
                .Select(field => $"{field.Key} = 0x{field.Value:X4}")
                .ToArray();

            Assert.IsEmpty(strays, "A field id is one band byte and one non-zero index byte, and the " +
                                   "band must be one the header declares:\n" + string.Join("\n", strays));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // None replaces what used to be a null path. It has to stay the ONLY zero, because every
        // consumer reads a zero as "this operation takes part in no override at all".
        public void OnlyNone_IsZero()
        {
            Assert.AreEqual(0, ModificationFields.None);

            var zeroes = Fields().Where(field => field.Value == 0).Select(field => field.Key).ToArray();

            Assert.IsEmpty(zeroes, "A field whose id is zero is indistinguishable from no field:\n"
                                   + string.Join("\n", zeroes));
        }

        /// <summary> Every property in the model assembly carrying the attribute, with the id it claims. </summary>
        private static List<(PropertyInfo Property, int Field)> MarkedMembers() =>
            typeof(ModificationFields).Assembly.GetTypes()
                .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance
                                                       | BindingFlags.DeclaredOnly))
                .Select(property => (Property: property,
                    Attribute: property.GetCustomAttribute<ModificationFieldAttribute>()))
                .Where(pair => pair.Attribute != null)
                .Select(pair => (pair.Property, pair.Attribute.Field))
                .ToList();

        private static string Describe(PropertyInfo property) =>
            property.DeclaringType?.Name + "." + property.Name;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        // The canary is the point of the count assertion: a reflection sweep that finds nothing
        // passes every other check in this file while proving none of them.
        public void EveryMarkedMember_UsesADeclaredConstant()
        {
            var declared = new HashSet<int>(Fields().Values);
            var marked = MarkedMembers();

            Assert.Greater(marked.Count, 25, "The sweep found almost no marked members, which means "
                                             + "it is asking the wrong assembly rather than that the "
                                             + "models lost their attributes.");

            var strays = marked
                .Where(member => !declared.Contains(member.Field))
                .Select(member => Describe(member.Property) + " claims 0x" + member.Field.ToString("X4"))
                .ToArray();

            Assert.IsEmpty(strays, "A member claiming a number ModificationFields does not declare is "
                                   + "addressable by nothing: " + string.Join("; ", strays));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        // BH.SDK.Roslyn refuses this too (BHS1201), and it is still worth asserting here: a wrong
        // constant on the right member - ShapeColors pasted onto TextObject.Colors - reads as a
        // plausible line and turns one override into an address for two fields.
        public void NoTwoMembers_ShareAFieldId()
        {
            var collisions = MarkedMembers()
                .GroupBy(member => member.Field)
                .Where(group => group.Count() > 1)
                .Select(group => "0x" + group.Key.ToString("X4") + ": "
                                 + string.Join(", ", group.Select(m => Describe(m.Property))))
                .ToArray();

            Assert.IsEmpty(collisions, "Two members sharing an id: " + string.Join(" | ", collisions));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // An overridable member that is not serialized cannot be overridden in any sense that
        // survives a save, so the two attributes travel together or neither is true.
        public void EveryMarkedMember_IsSerialized()
        {
            var unserialized = MarkedMembers()
                .Where(member => member.Property.GetCustomAttribute<JsonPropertyAttribute>() == null)
                .Select(member => Describe(member.Property))
                .ToArray();

            Assert.IsEmpty(unserialized, "A marked member carrying no [JsonProperty]: "
                                         + string.Join(", ", unserialized));
        }
    }
}

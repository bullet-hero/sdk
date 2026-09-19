using System.Collections.Generic;
using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // ThemeData.ColorNames is the first member in the format that is BOTH a collection and legally
    // null, so it is the first place [RuleOptional] and [RuleCollectionCount] meet. RuleWalk returns
    // on the optional check BEFORE the rule loop runs, which means optional suppresses every rule on
    // the property rather than only the null safety net - these fixtures are what pins that reading,
    // since getting it backwards turns an unnamed palette into an issue on every load of a fine level.

    /// <summary> ThemeData.ColorNames: null is legal, a non-null list is exactly 64 long. </summary>
    public class ThemeColorNamesRuleTests : BaseRuleTests
    {
        private static ThemeData Theme(List<string> colorNames)
        {
            var theme = new ThemeData(ThemeId.NewGuid(), "Theme");
            theme.ColorNames = colorNames;
            return theme;
        }

        private static List<string> Named(int count)
        {
            var names = new List<string>(count);
            for (var i = 0; i < count; i++)
                names.Add(i == 0 ? "fallback" : string.Empty);
            return names;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NullColorNames_ReportsNothing()
        {
            AssertValid(Theme(null));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void FullColorNames_ReportsNothing()
        {
            AssertValid(Theme(Named(ValueRules.ThemeCount)));
        }

        /// <summary> A null ENTRY means that one slot is unnamed and is not a violation - only the
        /// list's own length is. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ANullEntry_IsNotAViolation()
        {
            var names = Named(ValueRules.ThemeCount);
            names[5] = null;

            AssertValid(Theme(names));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ShortColorNames_ReportsCollectionCount()
        {
            AssertInvalid<RuleCollectionCountAttribute>(Theme(Named(3)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ShortColorNames_IsPaddedToThemeCount()
        {
            var theme = Theme(Named(3));

            AssertFixedTo(theme, () => theme.ColorNames.Count, ValueRules.ThemeCount);
        }

        /// <summary> A theme carrying no names must survive a copy - the generated Copy used to hand
        /// List's own copy constructor a null and throw. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ANamelessTheme_CopiesAndEqualsItsCopy()
        {
            var theme = Theme(null);

            var copy = theme.Copy();

            Assert.IsNull(copy.ColorNames);
            Assert.IsTrue(theme.Equals(copy), "a nameless theme does not equal its own copy");
        }
    }
}

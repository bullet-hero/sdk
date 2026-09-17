using System;
using BH.SDK.Models.Enums;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary>
    /// ObjectTypeMask - one bit per <see cref="ObjectType"/>, a bit PRESENT meaning that kind is
    /// shown unfolded. The property worth pinning is the pairing itself: ToMask is a shift by the
    /// member's own ordinal, so a kind added to ObjectType and forgotten here silently lands on
    /// whatever bit sits at that ordinal, and a surface then folds the wrong rows with nothing
    /// failing to compile.
    /// </summary>
    public class ObjectTypeMaskTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void EveryObjectTypeHasItsOwnBit()
        {
            var seen = ObjectTypeMask.None;

            foreach (ObjectType type in Enum.GetValues(typeof(ObjectType)))
            {
                var bit = type.ToMask();

                Assert.AreNotEqual(ObjectTypeMask.None, bit, $"{type} maps to no bit");
                Assert.AreEqual(ObjectTypeMask.None, seen & bit, $"{type} shares a bit with another kind");
                Assert.IsTrue(Enum.IsDefined(typeof(ObjectTypeMask), bit),
                    $"{type} maps to a bit ObjectTypeMask does not declare");

                seen |= bit;
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AllIsExactlyEveryObjectTypesBit()
        {
            var seen = ObjectTypeMask.None;
            foreach (ObjectType type in Enum.GetValues(typeof(ObjectType))) seen |= type.ToMask();

            Assert.AreEqual(ObjectTypeMask.All, seen);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void HasAnswersForEveryKindOfAFullAndAnEmptyMask()
        {
            foreach (ObjectType type in Enum.GetValues(typeof(ObjectType)))
            {
                Assert.IsTrue(ObjectTypeMask.All.Has(type));
                Assert.IsFalse(ObjectTypeMask.None.Has(type));
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void WithAndWithoutRoundTrip()
        {
            foreach (ObjectType type in Enum.GetValues(typeof(ObjectType)))
            {
                Assert.IsTrue(ObjectTypeMask.None.With(type).Has(type));
                Assert.IsFalse(ObjectTypeMask.All.Without(type).Has(type));
                Assert.AreEqual(ObjectTypeMask.All, ObjectTypeMask.All.Without(type).With(type));
                Assert.AreEqual(ObjectTypeMask.None, ObjectTypeMask.None.With(type).Without(type));
            }
        }

        // The default the feature was asked for: everything open except the placements.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void EverythingButPrefabsIsWhatItSays()
        {
            var mask = ObjectTypeMask.All & ~ObjectTypeMask.PrefabObject;

            Assert.IsFalse(mask.Has(ObjectType.PrefabObject));
            Assert.IsTrue(mask.Has(ObjectType.RectObject));
            Assert.IsTrue(mask.Has(ObjectType.ShapeObject));
            Assert.IsTrue(mask.Has(ObjectType.EffectObject));
            Assert.IsTrue(mask.Has(ObjectType.TextObject));
        }
    }
}

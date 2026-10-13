using System;
using System.Linq;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Publishing;
using NUnit.Framework;

namespace BH.SDK.Tests.Publishing
{
    /// <summary>
    /// LicenseTerms: what the Creative Commons legal codes say, so a change in what these assert is a
    /// change in what the licenses mean, never a policy choice.
    /// </summary>
    public class LicenseTermsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEveryCreativeCommonsLicenseHasElements()
        {
            var creativeCommons = Enum.GetValues(typeof(TypicalLicenseType)).Cast<TypicalLicenseType>()
                .Where(type => (short)type >= (short)TypicalLicenseType.CC0_1_0);

            foreach (var type in creativeCommons)
            {
                Assert.IsTrue(LicenseTerms.TryGetElements(type, out var elements), type.ToString());
                if (type != TypicalLicenseType.CC0_1_0)
                    Assert.IsTrue((elements & LicenseElements.Attribution) != 0, type.ToString());
            }
        }

        [TestCase(TypicalLicenseType.MIT)]
        [TestCase(TypicalLicenseType.Proprietary)]
        [TestCase(TypicalLicenseType.GPL_3_0)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNonCreativeCommonsHasNoElements(TypicalLicenseType type)
        {
            Assert.IsFalse(LicenseTerms.TryGetElements(type, out var elements));
            Assert.AreEqual(LicenseElements.None, elements);
        }

        [TestCase(TypicalLicenseType.CC_BY_NC_ND_3_0,
            LicenseElements.Attribution | LicenseElements.NonCommercial | LicenseElements.NoDerivatives)]
        [TestCase(TypicalLicenseType.CC_BY_NC_SA_4_0,
            LicenseElements.Attribution | LicenseElements.NonCommercial | LicenseElements.ShareAlike)]
        [TestCase(TypicalLicenseType.CC0_1_0, LicenseElements.None)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestElementsOf(TypicalLicenseType type, LicenseElements expected)
        {
            LicenseTerms.TryGetElements(type, out var elements);
            Assert.AreEqual(expected, elements);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestShareAlikeTargets()
        {
            CollectionAssert.AreEqual(new[] { TypicalLicenseType.CC_BY_SA_4_0 },
                LicenseTerms.ShareAlikeTargets(TypicalLicenseType.CC_BY_SA_4_0));
            CollectionAssert.AreEqual(new[] { TypicalLicenseType.CC_BY_SA_3_0, TypicalLicenseType.CC_BY_SA_4_0 },
                LicenseTerms.ShareAlikeTargets(TypicalLicenseType.CC_BY_SA_3_0));
            CollectionAssert.AreEqual(new[] { TypicalLicenseType.CC_BY_NC_SA_4_0 },
                LicenseTerms.ShareAlikeTargets(TypicalLicenseType.CC_BY_NC_SA_4_0));
            CollectionAssert.AreEqual(
                new[] { TypicalLicenseType.CC_BY_NC_SA_3_0, TypicalLicenseType.CC_BY_NC_SA_4_0 },
                LicenseTerms.ShareAlikeTargets(TypicalLicenseType.CC_BY_NC_SA_3_0));
        }

        [TestCase(TypicalLicenseType.CC_BY_4_0)]
        [TestCase(TypicalLicenseType.CC_BY_ND_4_0)]
        [TestCase(TypicalLicenseType.MIT)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNonShareAlikeHasNoTargets(TypicalLicenseType type)
        {
            Assert.AreEqual(0, LicenseTerms.ShareAlikeTargets(type).Count);
        }
    }
}

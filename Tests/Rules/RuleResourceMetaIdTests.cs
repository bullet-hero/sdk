using System;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleResourceMetaId: a file record fills the int slot in the user range, a data record fills the
    /// guid slot, the cover's record fills neither, and none fills a slot its type does not use.
    /// </summary>
    public class RuleResourceMetaIdTests : BaseRuleTests
    {
        private static ResourceMeta Record(ResourceType type, int id, Guid guid)
            => new() { ResourceType = type, ResourceId = new TypedResourceId(id), ResourceGuid = guid };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_FileRecord_Passes() => AssertValid(Record(ResourceType.Texture, -1, Guid.Empty));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_DataRecord_Passes() => AssertValid(Record(ResourceType.Prefab, 0, Guid.NewGuid()));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_LogoRecordWithNeitherSlot_Passes() => AssertValid(Record(ResourceType.LevelLogo, 0, Guid.Empty));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_LogoRecordWithAnInt_Reported()
            => AssertInvalid<RuleResourceMetaIdAttribute>(Record(ResourceType.LevelLogo, -1, Guid.Empty));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_LogoRecordWithAGuid_Reported()
            => AssertInvalid<RuleResourceMetaIdAttribute>(Record(ResourceType.LevelLogo, 0, Guid.NewGuid()));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Fix_ClearsBothSlotsOfALogoRecord()
        {
            var record = Record(ResourceType.LevelLogo, -3, Guid.NewGuid());

            AssertFixed(record);

            Assert.AreEqual(TypedResourceId.Null, record.ResourceId);
            Assert.AreEqual(Guid.Empty, record.ResourceGuid);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_FileRecordInTheGameRange_Reported()
            => AssertInvalid<RuleResourceMetaIdAttribute>(Record(ResourceType.Audio, 3, Guid.Empty));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_FileRecordWithAGuid_Reported()
            => AssertInvalid<RuleResourceMetaIdAttribute>(Record(ResourceType.Font, -2, Guid.NewGuid()));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_DataRecordWithoutAGuid_Reported()
            => AssertInvalid<RuleResourceMetaIdAttribute>(Record(ResourceType.Theme, 0, Guid.Empty));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_DataRecordWithAnInt_Reported()
            => AssertInvalid<RuleResourceMetaIdAttribute>(Record(ResourceType.Shape, -4, Guid.NewGuid()));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Fix_ClampsAFileRecordIntoTheUserRange()
        {
            var record = Record(ResourceType.Texture, 5, Guid.NewGuid());

            AssertFixed(record);

            Assert.AreEqual(TypedResourceId.MaxUserDefinedValue, record.ResourceId.value);
            Assert.AreEqual(Guid.Empty, record.ResourceGuid);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Fix_ClearsADataRecordsInt()
        {
            var guid = Guid.NewGuid();
            var record = Record(ResourceType.Effect, -1, guid);

            AssertFixed(record);

            Assert.AreEqual(TypedResourceId.Null, record.ResourceId);
            Assert.AreEqual(guid, record.ResourceGuid);
        }
    }
}

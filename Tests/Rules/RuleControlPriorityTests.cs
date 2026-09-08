using BH.SDK.Models.Enums.Controls;
using BH.SDK.Rules.Attributes;
using BH.SDK.Services.Controls;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleControlPriority: the device priority array lists every ControlDevice exactly once, and Fix
    /// repairs it without discarding the order the player arranged.
    /// </summary>
    public class RuleControlPriorityTests : BaseRuleTests
    {
        /// <summary> A control-device priority list, which must name every device exactly once. </summary>
        [RuleContainer]
        private class PriorityModel
        {
            [RuleControlPriority]
            public ControlDevice[] Priority { get; set; } = ControlDeviceCatalog.Devices;
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleControlPriority]
            public float Value { get; set; }
        }

        private static PriorityModel Valid() => new()
        {
            Priority = new[]
            {
                ControlDevice.Gamepad,
                ControlDevice.KeyboardMouse,
                ControlDevice.Touchscreen,
                ControlDevice.DeviceGyro,
            },
        };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Valid_FullPermutation_Passes() => AssertValid(Valid());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Valid_CatalogOrder_Passes() => AssertValid(new PriorityModel());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Invalid_Duplicate_Reported()
        {
            var model = Valid();
            model.Priority[1] = ControlDevice.Gamepad;
            AssertInvalid<RuleControlPriorityAttribute>(model);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Invalid_TooShort_Reported()
        {
            var model = new PriorityModel { Priority = new[] { ControlDevice.KeyboardMouse } };
            AssertInvalid<RuleControlPriorityAttribute>(model);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Invalid_UndeclaredValue_Reported()
        {
            var model = Valid();
            model.Priority[0] = (ControlDevice)42;
            AssertInvalid<RuleControlPriorityAttribute>(model);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Fix_KeepsAuthoredOrderAndAppendsMissing()
        {
            var model = new PriorityModel
            {
                Priority = new[] { ControlDevice.DeviceGyro, ControlDevice.DeviceGyro, ControlDevice.Gamepad },
            };

            AssertFixed(model);

            Assert.AreEqual(ControlDeviceCatalog.DeviceCount, model.Priority.Length);
            Assert.AreEqual(ControlDevice.DeviceGyro, model.Priority[0]);
            Assert.AreEqual(ControlDevice.Gamepad, model.Priority[1]);
            CollectionAssert.AllItemsAreUnique(model.Priority);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Fix_NullBecomesCatalogOrder()
        {
            var model = new PriorityModel { Priority = null };

            AssertFixed(model);
            CollectionAssert.AreEqual(ControlDeviceCatalog.Devices, model.Priority);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void WrongType_Throws() => AssertWrongType(new WrongTypeModel());
    }
}

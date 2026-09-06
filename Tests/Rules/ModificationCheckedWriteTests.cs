using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Services;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // A per-instance override is the one write in the whole format that reaches a model without
    // passing anything that could judge it - ModificationService resolves a path and assigns. So an
    // override could hold a value outside its property's declared range while the level it belongs
    // to validated clean, and nothing would notice until playback.

    /// <summary>
    /// ModificationService.IsValueAllowed / SetValueChecked: the rules of the target property,
    /// applied to a value on its way in.
    /// </summary>
    public class ModificationCheckedWriteTests
    {
        private static ModificationService ServiceFor(params System.Type[] types)
        {
            var service = new ModificationService();
            foreach (var type in types) service.Add(type);
            return service;
        }

        private static RuleContext ContextOfLength(int frameDuration)
        {
            var level = new Level();
            level.Settings.FrameDuration = frameDuration;
            return RuleContext.ForRoot(level);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestAllowedValueIsWritten()
        {
            var service = ServiceFor(typeof(RectObject));
            var obj = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsTrue(service.SetValueChecked(obj, 50, Names.LayerShort, ContextOfLength(100)));
            Assert.AreEqual(50, obj.Layer);
        }

        // The case the whole feature exists for: a value outside what the property allows, arriving
        // through an override rather than through the editor.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestOutOfRangeValueIsRefused()
        {
            var service = ServiceFor(typeof(RectObject));
            var obj = new RectObject { ObjectId = new ObjectId(1), Layer = 10 };

            Assert.IsFalse(service.SetValueChecked(obj, ValueRules.MaxLayer + 1, Names.LayerShort,
                ContextOfLength(100)));
            Assert.AreEqual(10, obj.Layer, "A refused write must change nothing");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestOutOfRangeLayerIsRefused()
        {
            var service = ServiceFor(typeof(RectObject));
            var obj = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsFalse(service.IsValueAllowed(obj, ValueRules.MaxLayer + 1, Names.LayerShort,
                ContextOfLength(100)));
            Assert.IsTrue(service.IsValueAllowed(obj, ValueRules.MaxLayer, Names.LayerShort,
                ContextOfLength(100)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestNullIntoNotNullPropertyIsRefused()
        {
            var service = ServiceFor(typeof(RectObject));
            var obj = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsFalse(service.IsValueAllowed(obj, null, Names.Name, ContextOfLength(100)));
        }

        // The plain write is left as it was: existing callers keep their behaviour, and opting into
        // checking is explicit.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestUncheckedWriteStillBypassesRules()
        {
            var service = ServiceFor(typeof(RectObject));
            var obj = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsTrue(service.SetValue(obj, ValueRules.MaxLayer + 1, Names.LayerShort));
            Assert.AreEqual(ValueRules.MaxLayer + 1, obj.Layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestUnresolvablePathIsRefused()
        {
            var service = ServiceFor(typeof(RectObject));
            var obj = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsFalse(service.IsValueAllowed(obj, 1, "no_such_field", ContextOfLength(100)));
            Assert.IsFalse(service.SetValueChecked(obj, 1, "no_such_field", ContextOfLength(100)));
        }
    }
}

using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // A per-instance override is the one write in the whole format that reaches a model without
    // passing anything that could judge it - the table resolves a field and assigns. So an override
    // could hold a value outside its property's declared range while the level it belongs to
    // validated clean, and nothing would notice until playback.
    //
    // THE MECHANISM MOVED AND THE CAPABILITY DID NOT. This used to walk a dotted path through
    // ModificationService's reflection to reach a PropertyInfo; it now asks the generated table for
    // the same PropertyInfo by field id. Both halves of the pair are still here on purpose: what a
    // rule-checked write refuses is the whole point, and the fact that the PLAIN write still does
    // not check is what makes opting in explicit rather than accidental.

    /// <summary>
    /// ModificationUtils.IsValueAllowed / SetValueChecked: the rules of the target property,
    /// applied to a value on its way in.
    /// </summary>
    public class ModificationCheckedWriteTests
    {
        private static RuleContext ContextOfLength(int frameDuration)
        {
            var level = new Level();
            level.Settings.FrameDuration = frameDuration;
            return RuleContext.ForRoot(level);
        }

        private static Modification Override(RectObject obj, int field, object value)
            => new(new ModificationKey(obj.ObjectId, field), value);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestAllowedValueIsWritten()
        {
            var obj = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsTrue(obj.SetValueChecked(Override(obj, ModificationFields.Layer, 50),
                ContextOfLength(100)));
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
            var obj = new RectObject { ObjectId = new ObjectId(1), Layer = 10 };

            Assert.IsFalse(obj.SetValueChecked(
                Override(obj, ModificationFields.Layer, ValueRules.MaxLayer + 1), ContextOfLength(100)));
            Assert.AreEqual(10, obj.Layer, "A refused write must change nothing");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestOutOfRangeLayerIsRefused()
        {
            Assert.IsFalse(ModificationUtils.IsValueAllowed(ModificationFields.Layer,
                ValueRules.MaxLayer + 1, ContextOfLength(100)));
            Assert.IsTrue(ModificationUtils.IsValueAllowed(ModificationFields.Layer,
                ValueRules.MaxLayer, ContextOfLength(100)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestNullIntoNotNullPropertyIsRefused()
        {
            Assert.IsFalse(ModificationUtils.IsValueAllowed(ModificationFields.Name, null,
                ContextOfLength(100)));
        }

        // The plain write is left as it was: existing callers keep their behaviour, and opting into
        // checking is explicit.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestUncheckedWriteStillBypassesRules()
        {
            var obj = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsTrue(obj.Apply(Override(obj, ModificationFields.Layer, ValueRules.MaxLayer + 1)));
            Assert.AreEqual(ValueRules.MaxLayer + 1, obj.Layer);
        }

        // What used to be an unresolvable PATH is an unregistered ID now, and the answer is the
        // same: refused by both halves, with nothing written.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestUnregisteredFieldIsRefused()
        {
            var obj = new RectObject { ObjectId = new ObjectId(1) };
            const int noSuchField = 0x7F01;

            Assert.IsFalse(ModificationUtils.IsValueAllowed(noSuchField, 1, ContextOfLength(100)));
            Assert.IsFalse(obj.SetValueChecked(Override(obj, noSuchField, 1), ContextOfLength(100)));
        }
    }
}

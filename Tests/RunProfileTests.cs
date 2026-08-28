using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Statistics;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    [TestFixture]
    public class RunProfileTests
    {
        // THE WHOLE REASON SPEED IS AN INT. The speed control is a continuous slider whose readout
        // shows two decimals, so a float key would file "1.00" and "1.00" under two different
        // records whenever the two floats differed in a bit nobody can see - and both would sit in
        // the file forever, each claiming to be the best run under the same conditions.
        [Test]
        [TestCase(1f, 100)]
        [TestCase(0.999f, 100)]
        [TestCase(1.004f, 100)]
        [TestCase(1.006f, 101)]
        [TestCase(0.5f, 50)]
        [TestCase(2f, 200)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ToCenti_QuantizesToHundredths(float speed, int expected)
        {
            Assert.AreEqual(expected, RunProfile.ToCenti(speed));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FromLaunch_NearlyEqualSpeeds_ProduceOneKey()
        {
            var a = RunProfile.FromLaunch(3, 1f, true, BotKind.None);
            var b = RunProfile.FromLaunch(3, 0.9999999f, true, BotKind.None);

            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equals_DiffersOnEveryAxis()
        {
            var baseline = new RunProfile(3, 100, true, BotKind.None);

            Assert.AreNotEqual(baseline, new RunProfile(1, 100, true, BotKind.None));
            Assert.AreNotEqual(baseline, new RunProfile(3, 200, true, BotKind.None));
            Assert.AreNotEqual(baseline, new RunProfile(3, 100, false, BotKind.None));
            Assert.AreNotEqual(baseline, new RunProfile(3, 100, true, BotKind.Reflex));
            Assert.AreEqual(baseline, new RunProfile(3, 100, true, BotKind.None));
        }

        // Zen mode is lives = 0, a real choice rather than an unset value, so it has to be its own
        // key rather than collapsing into whatever the default life count happens to be.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Immortality_IsItsOwnProfile()
        {
            var zen = new RunProfile(0, 100, true, BotKind.None);
            var three = new RunProfile(3, 100, true, BotKind.None);

            Assert.IsTrue(zen.Immortality);
            Assert.IsFalse(three.Immortality);
            Assert.AreNotEqual(zen, three);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Speed_RoundTripsThroughCenti()
        {
            var profile = RunProfile.FromLaunch(3, 1.25f, false, BotKind.Warm);

            Assert.AreEqual(125, profile.SpeedCenti);
            Assert.AreEqual(1.25f, profile.Speed, 0.0001f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CompareTo_OrdersByEveryAxisInTurn()
        {
            var a = new RunProfile(1, 100, false, BotKind.None);
            var b = new RunProfile(1, 100, false, BotKind.Reflex);
            var c = new RunProfile(1, 100, true, BotKind.None);
            var d = new RunProfile(1, 200, false, BotKind.None);
            var e = new RunProfile(3, 100, false, BotKind.None);

            Assert.Less(a.CompareTo(b), 0);
            Assert.Less(a.CompareTo(c), 0);
            Assert.Less(a.CompareTo(d), 0);
            Assert.Less(a.CompareTo(e), 0);
            Assert.AreEqual(0, a.CompareTo(new RunProfile(1, 100, false, BotKind.None)));
        }
    }
}

using System;
using BH.SDK.Services.AudioAnalysis;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    /// <summary> The radix-2 FFT against a sine whose spectrum is known exactly. </summary>
    [TestFixture]
    public class FftTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Forward_SineOnABin_PutsAmplitudeTimesHalfNThereAndNothingElsewhere()
        {
            const int n = 1024;
            const int bin = 37;
            const float amplitude = 0.75f;
            var re = new float[n];
            var im = new float[n];
            for (var i = 0; i < n; i++) re[i] = amplitude * (float)Math.Sin(2 * Math.PI * bin * i / n);

            Fft.Forward(re, im);

            for (var k = 0; k < n; k++)
            {
                var magnitude = Math.Sqrt(re[k] * re[k] + im[k] * im[k]);
                var expected = k == bin || k == n - bin ? amplitude * n / 2 : 0;
                Assert.AreEqual(expected, magnitude, 1e-2, $"bin {k}");
            }

            Assert.AreEqual(-amplitude * n / 2, im[bin], 1e-2, "a sine is a negative imaginary part");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Inverse_UndoesForward()
        {
            const int n = 4096;
            var random = new Random(7);
            var re = new float[n];
            var im = new float[n];
            var original = new float[n];
            for (var i = 0; i < n; i++) original[i] = re[i] = (float)(random.NextDouble() * 2 - 1);

            Fft.Forward(re, im);
            Fft.Inverse(re, im);

            for (var i = 0; i < n; i++)
            {
                Assert.AreEqual(original[i], re[i], 1e-4, $"re {i}");
                Assert.AreEqual(0f, im[i], 1e-4, $"im {i}");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Forward_RefusesALengthThatIsNotAPowerOfTwo()
        {
            Assert.Throws<ArgumentException>(() => Fft.Forward(new float[1000], new float[1000]));
            Assert.Throws<ArgumentException>(() => Fft.Forward(new float[8], new float[16]));
            Assert.False(Fft.IsPowerOfTwo(0));
            Assert.True(Fft.IsPowerOfTwo(2048));
        }
    }
}

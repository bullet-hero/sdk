using System;

namespace BH.SDK.Services.AudioAnalysis
{
    /// <summary> Radix-2 complex FFT, in place over separate real and imaginary arrays. Stateless and
    /// thread-safe; the length must be a power of two. </summary>
    public static class Fft
    {
        /// <summary> Is this a length the transform accepts. </summary>
        public static bool IsPowerOfTwo(int length) => length > 0 && (length & (length - 1)) == 0;

        /// <summary> The forward transform, unscaled: a sine of amplitude A on bin k reads A * N / 2 there.
        /// </summary>
        public static void Forward(float[] re, float[] im) => Transform(re, im, false);

        /// <summary> The inverse transform, scaled by 1/N, so Inverse(Forward(x)) is x. </summary>
        public static void Inverse(float[] re, float[] im)
        {
            Transform(re, im, true);

            var scale = 1f / re.Length;
            for (var i = 0; i < re.Length; i++)
            {
                re[i] *= scale;
                im[i] *= scale;
            }
        }

        private static void Transform(float[] re, float[] im, bool inverse)
        {
            if (re == null) throw new ArgumentNullException(nameof(re));
            if (im == null) throw new ArgumentNullException(nameof(im));
            if (re.Length != im.Length) throw new ArgumentException("Real and imaginary lengths differ.");

            var n = re.Length;
            if (!IsPowerOfTwo(n)) throw new ArgumentException($"FFT length {n} is not a power of two.");

            for (int i = 1, j = 0; i < n; i++)
            {
                var bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;

                if (i >= j) continue;
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }

            var sign = inverse ? 1.0 : -1.0;
            for (var length = 2; length <= n; length <<= 1)
            {
                var half = length >> 1;
                var angle = sign * 2.0 * Math.PI / length;
                var stepRe = Math.Cos(angle);
                var stepIm = Math.Sin(angle);

                for (var start = 0; start < n; start += length)
                {
                    // The twiddle recurrence runs in double: in float it drifts visibly by N = 4096.
                    var wRe = 1.0;
                    var wIm = 0.0;

                    for (var k = 0; k < half; k++)
                    {
                        var a = start + k;
                        var b = a + half;
                        var tRe = (float)(re[b] * wRe - im[b] * wIm);
                        var tIm = (float)(re[b] * wIm + im[b] * wRe);

                        re[b] = re[a] - tRe;
                        im[b] = im[a] - tIm;
                        re[a] += tRe;
                        im[a] += tIm;

                        var next = wRe * stepRe - wIm * stepIm;
                        wIm = wRe * stepIm + wIm * stepRe;
                        wRe = next;
                    }
                }
            }
        }
    }
}

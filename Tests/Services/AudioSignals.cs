using System;

namespace BH.SDK.Tests.Services
{
    /// <summary> Synthetic test signals for the audio analyzer: decaying sine bursts placed on a beat grid.
    /// A fixture helper, not a test. </summary>
    internal static class AudioSignals
    {
        /// <summary> Frequency of the click every beat carries (mid band). </summary>
        public const float ClickHz = 1500f;

        /// <summary> Frequency of the kick a downbeat adds (low band). </summary>
        public const float KickHz = 70f;

        /// <summary> Silence after the last beat, long enough for a kick to decay: an abrupt cut is an onset.
        /// </summary>
        public const double TailSeconds = 0.5;

        /// <summary> Adds a sine burst decaying exponentially from <paramref name="seconds"/>. </summary>
        public static void AddBurst(float[] signal, int sampleRate, double seconds, float hz, float amplitude,
            double decaySeconds)
        {
            var start = (int)Math.Ceiling(seconds * sampleRate);
            var length = (int)(decaySeconds * 8 * sampleRate);
            for (var i = Math.Max(0, start); i < Math.Min(signal.Length, start + length); i++)
            {
                var t = i / (double)sampleRate - seconds;
                signal[i] += (float)(amplitude * Math.Sin(2 * Math.PI * hz * t) * Math.Exp(-t / decaySeconds));
            }
        }

        /// <summary> A click on every beat from <paramref name="firstBeat"/>; beats whose index plus
        /// <paramref name="pickup"/> is a multiple of <paramref name="beatsPerBar"/> are louder and add
        /// a kick. </summary>
        public static float[] ClickTrack(int sampleRate, double seconds, double bpm, double firstBeat,
            int beatsPerBar = 4, int pickup = 0)
        {
            var signal = new float[(int)(seconds * sampleRate)];
            var period = 60.0 / bpm;
            for (var k = 0; firstBeat + k * period < seconds - TailSeconds; k++)
            {
                var t = firstBeat + k * period;
                if ((k + pickup) % beatsPerBar == 0)
                {
                    AddBurst(signal, sampleRate, t, KickHz, 0.8f, 0.06);
                    AddBurst(signal, sampleRate, t, ClickHz, 0.9f, 0.01);
                }
                else
                {
                    AddBurst(signal, sampleRate, t, ClickHz, 0.5f, 0.01);
                }
            }

            return signal;
        }

        /// <summary> Strong hits (kick and click) on every other beat, a faint click on the beats between.
        /// </summary>
        public static float[] EveryOtherBeat(int sampleRate, double seconds, double bpm, double firstBeat)
        {
            var signal = new float[(int)(seconds * sampleRate)];
            var period = 60.0 / bpm;
            for (var k = 0; firstBeat + k * period < seconds - TailSeconds; k++)
            {
                var t = firstBeat + k * period;
                if (k % 2 == 0)
                {
                    AddBurst(signal, sampleRate, t, KickHz, 0.8f, 0.06);
                    AddBurst(signal, sampleRate, t, ClickHz, 0.9f, 0.01);
                }
                else
                {
                    AddBurst(signal, sampleRate, t, ClickHz, 0.25f, 0.01);
                }
            }

            return signal;
        }
    }
}
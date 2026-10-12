using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace BH.SDK.Services.AudioAnalysis
{
    // The algorithm, step by step, and why each step is shaped as it is: Docs/MUSIC.md.
    // No static mutable state: every buffer is local to one Analyze call, so calls run in parallel.

    /// <summary> Hears a mono signal: envelopes, per-band onsets, a fitted tempo, the downbeat, sections
    /// and notes, as an <see cref="AudioMap"/>. Pure and thread-safe. </summary>
    public static class AudioAnalyzer
    {
        private const int Rms = 0;
        private const int Low = 1;
        private const int Mid = 2;
        private const int High = 3;
        private const int BandCount = 4;

        /// <summary> Analyses <paramref name="mono"/> (samples in -1..1) at <paramref name="sampleRate"/>.
        /// </summary>
        /// <exception cref="ArgumentException"> A null signal, a bad sample rate, bad settings. </exception>
        /// <exception cref="OperationCanceledException"> The token was cancelled. </exception>
        public static AudioMap Analyze(float[] mono, int sampleRate, AudioAnalysisSettings settings,
            CancellationToken cancellationToken = default)
        {
            if (mono == null) throw new ArgumentNullException(nameof(mono));
            if (sampleRate <= 0) throw new ArgumentException($"Sample rate must be positive, got {sampleRate}.");
            settings ??= AudioAnalysisSettings.Default;
            settings.Validate();

            var duration = mono.Length / (double)sampleRate;
            var map = new AudioMap
            {
                SampleRate = sampleRate,
                DurationSeconds = duration,
                EnvelopeRate = AudioAnalysisRules.EnvelopeRate,
                BeatsPerBar = settings.BeatsPerBar,
            };

            var fine = MeasureFineEnergy(mono, sampleRate, cancellationToken);
            map.Envelopes = BuildEnvelopes(fine);

            var flux = MeasureFlux(mono, sampleRate, cancellationToken);
            map.Onsets = new AudioOnsets
            {
                Low = PickOnsets(flux[0], sampleRate, fine, Low),
                Mid = PickOnsets(flux[1], sampleRate, fine, Mid),
                High = PickOnsets(flux[2], sampleRate, fine, High),
            };
            cancellationToken.ThrowIfCancellationRequested();

            var events = Merge(map.Onsets);
            var grid = FitTempo(events, settings);
            map.Tempo = grid.Tempo;
            cancellationToken.ThrowIfCancellationRequested();

            if (grid.Found)
            {
                map.Tempo.DriftMs = MeasureDrift(map.Onsets.Low, events, grid);
                map.Downbeat = FindDownbeat(map.Onsets, grid, settings.BeatsPerBar, duration);
            }

            map.Sections = FindSections(map, grid);
            map.Notes = WriteNotes(map, events.Count, grid);
            return map;
        }

        // ---- Energy: four band signals, 1 ms blocks; the 100 Hz envelopes are sums of ten of them ----

        private sealed class FineEnergy
        {
            public int SampleRate;
            public int Count;
            public double[][] Sums;
            public int[] Samples;

            public long BlockStart(int block) => (long)block * SampleRate / AudioAnalysisRules.FineRate;

            public float MeanSquare(int band, int block) =>
                Samples[block] > 0 ? (float)(Sums[band][block] / Samples[block]) : 0f;
        }

        private static FineEnergy MeasureFineEnergy(float[] mono, int sampleRate, CancellationToken token)
        {
            var n = mono.Length;
            var count = (int)(((long)n * AudioAnalysisRules.FineRate + sampleRate - 1) / sampleRate);
            var fine = new FineEnergy
            {
                SampleRate = sampleRate,
                Count = count,
                Sums = new double[BandCount][],
                Samples = new int[count],
            };
            for (var b = 0; b < BandCount; b++) fine.Sums[b] = new double[count];

            var low = Biquad.LowPass(AudioAnalysisRules.LowBandEdge, sampleRate);
            var midHigh = Biquad.HighPass(AudioAnalysisRules.LowBandEdge, sampleRate);
            var midLow = Biquad.LowPass(AudioAnalysisRules.HighBandEdge, sampleRate);
            var high = Biquad.HighPass(AudioAnalysisRules.HighBandEdge, sampleRate);

            var block = 0;
            var blockEnd = count > 0 ? fine.BlockStart(1) : 0;
            double sRms = 0, sLow = 0, sMid = 0, sHigh = 0;
            var samples = 0;

            for (var i = 0; i < n; i++)
            {
                if (i >= blockEnd)
                {
                    Flush();
                    block++;
                    blockEnd = fine.BlockStart(block + 1);
                    if ((block & 4095) == 0) token.ThrowIfCancellationRequested();
                }

                double x = mono[i];
                var l = low.Process(x);
                var m = midLow.Process(midHigh.Process(x));
                var h = high.Process(x);

                sRms += x * x;
                sLow += l * l;
                sMid += m * m;
                sHigh += h * h;
                samples++;
            }

            if (n > 0) Flush();
            return fine;

            void Flush()
            {
                if (block >= count) return;
                fine.Sums[Rms][block] = sRms;
                fine.Sums[Low][block] = sLow;
                fine.Sums[Mid][block] = sMid;
                fine.Sums[High][block] = sHigh;
                fine.Samples[block] = samples;
                sRms = sLow = sMid = sHigh = 0;
                samples = 0;
            }
        }

        private static AudioEnvelopes BuildEnvelopes(FineEnergy fine)
        {
            const int perEnvelope = AudioAnalysisRules.FineRate / AudioAnalysisRules.EnvelopeRate;
            var count = (fine.Count + perEnvelope - 1) / perEnvelope;
            var bands = new byte[BandCount][];

            for (var b = 0; b < BandCount; b++)
            {
                var values = new float[count];
                for (var e = 0; e < count; e++)
                {
                    double sum = 0;
                    long samples = 0;
                    var last = Math.Min(fine.Count, (e + 1) * perEnvelope);
                    for (var j = e * perEnvelope; j < last; j++)
                    {
                        sum += fine.Sums[b][j];
                        samples += fine.Samples[j];
                    }

                    values[e] = samples > 0 ? (float)Math.Sqrt(sum / samples) : 0f;
                }

                bands[b] = ToBytes(values);
            }

            return new AudioEnvelopes { Rms = bands[Rms], Low = bands[Low], Mid = bands[Mid], High = bands[High] };
        }

        private static byte[] ToBytes(float[] values)
        {
            var bytes = new byte[values.Length];
            var scale = Percentile(values, AudioAnalysisRules.EnvelopePercentile);
            if (scale <= 0f) scale = Max(values);
            if (scale <= 0f) return bytes;

            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i] / scale;
                bytes[i] = (byte)(v >= 1f ? 255 : (int)(v * 255f + 0.5f));
            }

            return bytes;
        }

        // ---- Spectral flux: STFT frames centred on k * Hop, two real frames per complex FFT ----

        private static float[][] MeasureFlux(float[] mono, int sampleRate, CancellationToken token)
        {
            const int size = AudioAnalysisRules.FrameSize;
            const int hop = AudioAnalysisRules.Hop;
            const int bins = size / 2 + 1;

            var n = mono.Length;
            var frames = n == 0 ? 0 : (n - 1) / hop + 1;
            var flux = new[] { new float[frames], new float[frames], new float[frames] };

            var window = new float[size];
            for (var i = 0; i < size; i++) window[i] = (float)(0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / size));

            var bandOf = new int[bins];
            var binCount = new int[3];
            for (var k = 0; k < bins; k++)
            {
                var hz = k * (double)sampleRate / size;
                bandOf[k] = k == 0 ? -1
                    : hz < AudioAnalysisRules.LowBandEdge ? 0
                    : hz < AudioAnalysisRules.HighBandEdge ? 1
                    : 2;
                if (bandOf[k] >= 0) binCount[bandOf[k]]++;
            }

            var re = new float[size];
            var im = new float[size];
            var previous = new float[bins];
            var current = new float[bins];
            var second = new float[bins];

            for (var frame = 0; frame < frames; frame += 2)
            {
                if ((frame & 1023) == 0) token.ThrowIfCancellationRequested();

                var hasSecond = frame + 1 < frames;
                Fill(re, mono, window, frame * hop - size / 2);
                if (hasSecond) Fill(im, mono, window, (frame + 1) * hop - size / 2);
                else Array.Clear(im, 0, size);

                Fft.Forward(re, im);

                for (var k = 0; k < bins; k++)
                {
                    var j = (size - k) & (size - 1);
                    var aRe = (re[k] + re[j]) * 0.5f;
                    var aIm = (im[k] - im[j]) * 0.5f;
                    var bRe = (im[k] + im[j]) * 0.5f;
                    var bIm = (re[j] - re[k]) * 0.5f;
                    current[k] = MathF.Log(1f + MathF.Sqrt(aRe * aRe + aIm * aIm));
                    second[k] = MathF.Log(1f + MathF.Sqrt(bRe * bRe + bIm * bIm));
                }

                Accumulate(frame, current, previous);
                if (!hasSecond) break;
                Accumulate(frame + 1, second, current);
                Array.Copy(second, previous, bins);
            }

            for (var b = 0; b < 3; b++)
            {
                if (binCount[b] == 0) continue;
                var inverse = 1f / binCount[b];
                for (var f = 0; f < frames; f++) flux[b][f] *= inverse;
            }

            return flux;

            void Accumulate(int frame, float[] now, float[] before)
            {
                for (var k = 1; k < bins; k++)
                {
                    var rise = now[k] - before[k];
                    if (rise > 0f) flux[bandOf[k]][frame] += rise;
                }
            }
        }

        private static void Fill(float[] destination, float[] mono, float[] window, int start)
        {
            var size = destination.Length;
            for (var i = 0; i < size; i++)
            {
                var s = start + i;
                destination[i] = s >= 0 && s < mono.Length ? mono[s] * window[i] : 0f;
            }
        }

        // ---- Onsets: peaks of the flux above a moving median, timed against the band's fine energy ----

        private static List<AudioOnset> PickOnsets(float[] flux, int sampleRate, FineEnergy fine, int band)
        {
            var onsets = new List<AudioOnset>();
            var frames = flux.Length;
            var scale = Percentile(flux, AudioAnalysisRules.EnvelopePercentile);
            if (frames < 3 || scale < AudioAnalysisRules.MinFluxPerBin) return onsets;

            var half = Math.Max(1, (int)Math.Round(AudioAnalysisRules.OnsetMedianSeconds * 0.5 * sampleRate
                                                   / AudioAnalysisRules.Hop));
            var scratch = new float[2 * half + 1];
            var candidates = new List<(double seconds, float value)>();

            for (var k = 0; k < frames; k++)
            {
                var value = flux[k] / scale;
                var left = k > 0 ? flux[k - 1] / scale : 0f;
                var right = k + 1 < frames ? flux[k + 1] / scale : 0f;
                if (value < left || value <= right) continue;

                var from = Math.Max(0, k - half);
                var to = Math.Min(frames - 1, k + half);
                var length = to - from + 1;
                for (var i = 0; i < length; i++) scratch[i] = flux[from + i];
                Array.Sort(scratch, 0, length);
                var median = scratch[length / 2] / scale;
                if (value <= median + AudioAnalysisRules.OnsetDelta) continue;

                candidates.Add((RefineSeconds(k, sampleRate, fine, band), value));
            }

            // Strongest first, so the gap drops the weaker of two neighbours.
            candidates.Sort((a, b) => b.value.CompareTo(a.value));
            var kept = new List<(double seconds, float value)>();
            foreach (var candidate in candidates)
            {
                var clear = true;
                foreach (var other in kept)
                {
                    if (Math.Abs(other.seconds - candidate.seconds) >= AudioAnalysisRules.OnsetMinGapSeconds) continue;
                    clear = false;
                    break;
                }

                if (clear) kept.Add(candidate);
            }

            if (kept.Count == 0) return onsets;

            var values = new float[kept.Count];
            for (var i = 0; i < kept.Count; i++) values[i] = kept[i].value;
            var strong = Percentile(values, AudioAnalysisRules.StrongPercentile);

            kept.Sort((a, b) => a.seconds.CompareTo(b.seconds));
            foreach (var (seconds, value) in kept)
            {
                var strength = Math.Min(1f, value / strong);
                if (strength >= AudioAnalysisRules.MinOnsetStrength) onsets.Add(new AudioOnset(seconds, strength));
            }

            return onsets;
        }

        // The flux peaks between half a hop early and half a hop late of the attack (impulse vs step);
        // the steepest rise of the band's 1 ms energy in that reach is the attack itself.
        private static double RefineSeconds(int frame, int sampleRate, FineEnergy fine, int band)
        {
            var centre = (long)frame * AudioAnalysisRules.Hop;
            var reach = AudioAnalysisRules.Hop * 3 / 2;
            var first = (int)Math.Max(0, (centre - reach) * AudioAnalysisRules.FineRate / sampleRate);
            var last = (int)Math.Min(fine.Count - 1, (centre + reach) * AudioAnalysisRules.FineRate / sampleRate + 1);

            var best = -1;
            var bestRise = 0f;
            for (var j = first; j <= last; j++)
            {
                var rise = fine.MeanSquare(band, j) - (j > 0 ? fine.MeanSquare(band, j - 1) : 0f);
                if (rise <= bestRise) continue;
                bestRise = rise;
                best = j;
            }

            var sample = best >= 0 ? fine.BlockStart(best) : centre;
            return sample / (double)sampleRate;
        }

        private static List<AudioOnset> Merge(AudioOnsets onsets)
        {
            var all = new List<AudioOnset>(onsets.Low.Count + onsets.Mid.Count + onsets.High.Count);
            all.AddRange(onsets.Low);
            all.AddRange(onsets.Mid);
            all.AddRange(onsets.High);
            all.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));

            var events = new List<AudioOnset>();
            var i = 0;
            while (i < all.Count)
            {
                // The strongest member speaks for the event: a sum would let an accent (kick on 1) outvote
                // the beat and push half tempo past the ambiguity note.
                var start = all[i].Seconds;
                var strongest = all[i];
                var j = i;
                for (; j < all.Count && all[j].Seconds - start <= AudioAnalysisRules.MergeSeconds; j++)
                    if (all[j].Strength > strongest.Strength)
                        strongest = all[j];

                events.Add(strongest);
                i = j;
            }

            return events;
        }

        // ---- Tempo: brute force over BPM on a phase histogram, then a weighted least-squares refine ----

        private readonly struct Grid
        {
            public readonly AudioTempo Tempo;
            public readonly double Period;
            public readonly double Phase;

            public Grid(AudioTempo tempo, double period, double phase)
            {
                Tempo = tempo;
                Period = period;
                Phase = phase;
            }

            public bool Found => Period > 0;
        }

        private sealed class TempoScorer
        {
            private readonly List<AudioOnset> _events;
            private readonly double _total;
            private readonly double _span;
            private double[] _weights = Array.Empty<double>();
            private double[] _counts = Array.Empty<double>();

            public TempoScorer(List<AudioOnset> events)
            {
                _events = events;
                foreach (var e in events) _total += e.Strength;
                _span = events.Count > 1 ? events[events.Count - 1].Seconds - events[0].Seconds : 0;
            }

            // sqrt(share of onset strength on the grid * share of grid beats with an onset): the first
            // half alone prefers double tempo, the second alone half tempo.
            public double Score(double bpm, out double phase)
            {
                phase = 0;
                if (bpm <= 0 || _total <= 0) return 0;

                var period = 60.0 / bpm;
                var bins = Math.Max(1, (int)Math.Round(period / AudioAnalysisRules.PhaseStepSeconds));
                var width = period / bins;
                if (_weights.Length < bins)
                {
                    _weights = new double[bins];
                    _counts = new double[bins];
                }
                else
                {
                    Array.Clear(_weights, 0, bins);
                    Array.Clear(_counts, 0, bins);
                }

                foreach (var e in _events)
                {
                    var residue = e.Seconds % period;
                    if (residue < 0) residue += period;
                    var bin = (int)(residue / width);
                    if (bin >= bins) bin = bins - 1;
                    _weights[bin] += e.Strength;
                    _counts[bin] += 1;
                }

                var sigma = AudioAnalysisRules.SigmaSeconds;
                var reach = Math.Min((int)Math.Ceiling(3 * sigma / width), bins / 2);
                var beats = Math.Floor(_span / period) + 1;

                var best = -1.0;
                for (var p = 0; p < bins; p++)
                {
                    double weight = 0, count = 0;
                    for (var m = -reach; m <= reach; m++)
                    {
                        var d = m * width;
                        var g = Math.Exp(-d * d / (2 * sigma * sigma));
                        var index = ((p + m) % bins + bins) % bins;
                        weight += _weights[index] * g;
                        count += _counts[index] * g;
                    }

                    var score = Math.Sqrt(weight / _total * Math.Min(1.0, count / beats));
                    if (score <= best) continue;
                    best = score;
                    phase = (p + 0.5) * width;
                }

                return Math.Max(0, best);
            }
        }

        private static Grid FitTempo(List<AudioOnset> events, AudioAnalysisSettings settings)
        {
            var tempo = new AudioTempo();
            if (events.Count < 2) return new Grid(tempo, 0, 0);

            var scorer = new TempoScorer(events);
            var scores = new List<double>();
            var bestBpm = 0.0;
            var bestScore = 0.0;
            var steps = (int)Math.Floor((settings.MaxBpm - settings.MinBpm) / AudioAnalysisRules.BpmStep + 1e-6);

            for (var s = 0; s <= steps; s++)
            {
                var bpm = settings.MinBpm + s * (double)AudioAnalysisRules.BpmStep;
                var score = scorer.Score(bpm, out _);
                scores.Add(score);
                if (score <= bestScore) continue;
                bestScore = score;
                bestBpm = bpm;
            }

            if (bestScore <= 0) return new Grid(tempo, 0, 0);

            var coarse = bestBpm;
            var fineSteps = (int)Math.Round(AudioAnalysisRules.BpmStep / AudioAnalysisRules.BpmFineStep);
            var phase = 0.0;
            var fineScore = -1.0;
            for (var s = -fineSteps; s <= fineSteps; s++)
            {
                var bpm = coarse + s * (double)AudioAnalysisRules.BpmFineStep;
                if (s != 0 && (bpm < settings.MinBpm || bpm > settings.MaxBpm)) continue;
                var score = scorer.Score(bpm, out var p);
                if (score <= fineScore) continue;
                fineScore = score;
                bestBpm = bpm;
                phase = p;
            }

            bestScore = Math.Max(bestScore, fineScore);
            var period = 60.0 / bestBpm;
            Refine(events, ref period, ref phase);
            if (60.0 / period < settings.MinBpm * 0.98 || 60.0 / period > settings.MaxBpm * 1.02)
            {
                period = 60.0 / bestBpm;
                scorer.Score(bestBpm, out phase);
            }

            phase %= period;
            if (phase < 0) phase += period;

            var finalBpm = 60.0 / period;
            var finalScore = scorer.Score(finalBpm, out _);
            var reference = Math.Max(finalScore, bestScore);
            var ambiguity = Math.Max(scorer.Score(finalBpm / 2, out _), scorer.Score(finalBpm * 2, out _));

            scores.Sort();
            var median = scores[scores.Count / 2];

            tempo.Bpm = finalBpm;
            tempo.PhaseSeconds = phase;
            tempo.Confidence = Clamp01(1 - median / reference);
            tempo.Ambiguity = Clamp01(ambiguity / reference);
            return new Grid(tempo, period, phase);
        }

        private static void Refine(List<AudioOnset> events, ref double period, ref double phase)
        {
            for (var iteration = 0; iteration < 3; iteration++)
            {
                var reach = Math.Min(3 * AudioAnalysisRules.SigmaSeconds, period / 4);
                double sw = 0, sk = 0, st = 0, skk = 0, skt = 0;
                var used = 0;

                foreach (var e in events)
                {
                    var k = Math.Round((e.Seconds - phase) / period);
                    var d = e.Seconds - phase - k * period;
                    if (Math.Abs(d) > reach) continue;

                    double w = e.Strength;
                    sw += w;
                    sk += w * k;
                    st += w * e.Seconds;
                    skk += w * k * k;
                    skt += w * k * e.Seconds;
                    used++;
                }

                var denominator = sw * skk - sk * sk;
                if (used < 3 || denominator <= 1e-9) return;

                var slope = (sw * skt - sk * st) / denominator;
                if (slope <= 0 || Math.Abs(slope - period) > period * 0.05) return;

                phase = (st - slope * sk) / sw;
                period = slope;
            }
        }

        private static double MeasureDrift(List<AudioOnset> low, List<AudioOnset> events, Grid grid)
        {
            var from = events[0].Seconds;
            var to = events[events.Count - 1].Seconds;
            var third = (to - from) / 3;
            if (third <= 0) return 0;

            if (TryDrift(low, grid, from + third, to - third, out var drift)) return drift;
            return TryDrift(events, grid, from + third, to - third, out drift) ? drift : 0;
        }

        private static bool TryDrift(List<AudioOnset> onsets, Grid grid, double firstEnd, double lastStart,
            out double driftMs)
        {
            var first = new List<double>();
            var last = new List<double>();
            foreach (var o in onsets)
            {
                var k = Math.Round((o.Seconds - grid.Phase) / grid.Period);
                var d = o.Seconds - grid.Phase - k * grid.Period;
                if (Math.Abs(d) > grid.Period / 4) continue;
                if (o.Seconds < firstEnd) first.Add(d);
                else if (o.Seconds >= lastStart) last.Add(d);
            }

            driftMs = 0;
            if (first.Count < AudioAnalysisRules.MinDriftOnsets || last.Count < AudioAnalysisRules.MinDriftOnsets)
                return false;

            driftMs = (Median(last) - Median(first)) * 1000;
            return true;
        }

        // ---- Downbeat: low band on beat 1, mid band on the even beats (2 and 4 in 4/4) ----

        private static AudioDownbeat FindDownbeat(AudioOnsets onsets, Grid grid, int beatsPerBar, double duration)
        {
            var downbeat = new AudioDownbeat { Seconds = grid.Phase };
            if (beatsPerBar <= 1)
            {
                downbeat.Confidence = 1;
                return downbeat;
            }

            var beats = (int)Math.Floor((duration - grid.Phase) / grid.Period) + 2;
            if (beats < 1) return downbeat;

            var low = Support(onsets.Low, grid, beats);
            var mid = Support(onsets.Mid, grid, beats);
            var scores = new double[beatsPerBar];
            for (var c = 0; c < beatsPerBar; c++)
            for (var k = 0; k < beats; k++)
            {
                var role = ((k - c) % beatsPerBar + beatsPerBar) % beatsPerBar;
                if (role == 0) scores[c] += low[k];
                else if (role % 2 == 1) scores[c] += mid[k];
            }

            if (Max(scores) <= 0)
            {
                var high = Support(onsets.High, grid, beats);
                for (var c = 0; c < beatsPerBar; c++)
                for (var k = c; k < beats; k += beatsPerBar)
                    scores[c] += low[k] + mid[k] + high[k];
            }

            var best = 0;
            for (var c = 1; c < beatsPerBar; c++)
                if (scores[c] > scores[best])
                    best = c;

            var second = 0.0;
            for (var c = 0; c < beatsPerBar; c++)
                if (c != best && scores[c] > second)
                    second = scores[c];

            downbeat.Phase = best;
            downbeat.Seconds = grid.Phase + best * grid.Period;
            downbeat.Confidence = scores[best] > 0 ? Clamp01((scores[best] - second) / scores[best]) : 0;
            return downbeat;
        }

        private static double[] Support(List<AudioOnset> onsets, Grid grid, int beats)
        {
            var support = new double[beats];
            var sigma = AudioAnalysisRules.SigmaSeconds;
            foreach (var o in onsets)
            {
                var k = (int)Math.Round((o.Seconds - grid.Phase) / grid.Period);
                if (k < 0 || k >= beats) continue;
                var d = o.Seconds - grid.Phase - k * grid.Period;
                if (Math.Abs(d) > 3 * sigma) continue;
                support[k] += o.Strength * Math.Exp(-d * d / (2 * sigma * sigma));
            }

            return support;
        }

        // ---- Sections: standardised distance between consecutive windows of SectionBars bars ----

        private const int FeatureCount = 7;

        private static List<AudioSection> FindSections(AudioMap map, Grid grid)
        {
            var duration = map.DurationSeconds;
            var sections = new List<AudioSection>();
            if (duration <= 0) return sections;

            var bounds = WindowBounds(map, grid);
            var windows = bounds.Count - 1;
            var features = new double[windows][];
            for (var w = 0; w < windows; w++) features[w] = Features(map, bounds[w], bounds[w + 1]);
            NormaliseRates(features);
            Standardise(features);

            var novelty = new double[windows];
            for (var w = 1; w < windows; w++) novelty[w] = Distance(features[w - 1], features[w]);

            var starts = new List<int> { 0 };
            for (var w = 1; w < windows; w++)
            {
                if (novelty[w] <= AudioAnalysisRules.SectionNovelty) continue;
                if (novelty[w] < novelty[w - 1]) continue;
                if (w + 1 < windows && novelty[w] < novelty[w + 1]) continue;
                starts.Add(w);
            }

            starts.Add(windows);
            var means = new List<double[]>();
            for (var s = 0; s + 1 < starts.Count; s++)
            {
                means.Add(MeanFeatures(features, bounds, starts[s], starts[s + 1]));
                sections.Add(new AudioSection { StartSeconds = bounds[starts[s]], EndSeconds = bounds[starts[s + 1]] });
            }

            Letter(sections, means);

            // Neighbours that came out with one letter are one section: the boundary was noise.
            for (var s = sections.Count - 1; s > 0; s--)
            {
                if (sections[s].Letter != sections[s - 1].Letter) continue;
                sections[s - 1].EndSeconds = sections[s].EndSeconds;
                sections.RemoveAt(s);
            }

            foreach (var section in sections)
            {
                section.Low = MeanByte(map.Envelopes.Low, section.StartSeconds, section.EndSeconds);
                section.Mid = MeanByte(map.Envelopes.Mid, section.StartSeconds, section.EndSeconds);
                section.High = MeanByte(map.Envelopes.High, section.StartSeconds, section.EndSeconds);
            }

            ClassifyEnergy(map, sections);
            return sections;
        }

        private static double[] MeanFeatures(double[][] features, List<double> bounds, int first, int end)
        {
            var mean = new double[FeatureCount];
            var length = bounds[end] - bounds[first];
            for (var w = first; w < end; w++)
            {
                var share = (bounds[w + 1] - bounds[w]) / length;
                for (var f = 0; f < FeatureCount; f++) mean[f] += features[w][f] * share;
            }

            return mean;
        }

        private static List<double> WindowBounds(AudioMap map, Grid grid)
        {
            var duration = map.DurationSeconds;
            double first, length;
            if (grid.Found)
            {
                length = AudioAnalysisRules.SectionBars * map.BeatsPerBar * grid.Period;
                first = map.Downbeat.Seconds + length;
            }
            else
            {
                length = AudioAnalysisRules.SectionFallbackSeconds;
                first = length;
            }

            var bounds = new List<double> { 0 };
            for (var t = first; t < duration; t += length) bounds.Add(t);

            // A tail shorter than one bar is folded into the window before it.
            var bar = grid.Found ? map.BeatsPerBar * grid.Period : length / AudioAnalysisRules.SectionBars;
            if (bounds.Count > 1 && duration - bounds[bounds.Count - 1] < bar) bounds.RemoveAt(bounds.Count - 1);
            bounds.Add(duration);
            return bounds;
        }

        private static double[] Features(AudioMap map, double from, double to)
        {
            var seconds = Math.Max(to - from, 1e-9);
            return new[]
            {
                MeanByte(map.Envelopes.Rms, from, to) / 255.0,
                MeanByte(map.Envelopes.Low, from, to) / 255.0,
                MeanByte(map.Envelopes.Mid, from, to) / 255.0,
                MeanByte(map.Envelopes.High, from, to) / 255.0,
                CountIn(map.Onsets.Low, from, to) / seconds,
                CountIn(map.Onsets.Mid, from, to) / seconds,
                CountIn(map.Onsets.High, from, to) / seconds,
            };
        }

        private static void NormaliseRates(double[][] features)
        {
            for (var f = 4; f < FeatureCount; f++)
            {
                var max = 0.0;
                foreach (var w in features) max = Math.Max(max, w[f]);
                if (max <= 0) continue;
                foreach (var w in features) w[f] /= max;
            }
        }

        // Each feature as standard deviations from its mean over the track: all features are positive and
        // alike, so unscaled windows look the same to any distance or angle.
        private static void Standardise(double[][] features)
        {
            if (features.Length == 0) return;

            for (var f = 0; f < FeatureCount; f++)
            {
                var mean = 0.0;
                foreach (var w in features) mean += w[f];
                mean /= features.Length;

                var variance = 0.0;
                foreach (var w in features) variance += (w[f] - mean) * (w[f] - mean);
                var spread = Math.Max(Math.Sqrt(variance / features.Length), AudioAnalysisRules.SectionSpreadFloor);

                foreach (var w in features) w[f] = (w[f] - mean) / spread;
            }
        }

        private static void ClassifyEnergy(AudioMap map, List<AudioSection> sections)
        {
            var energies = new double[sections.Count];
            var loudest = 0.0;
            for (var s = 0; s < sections.Count; s++)
            {
                energies[s] = MeanByte(map.Envelopes.Rms, sections[s].StartSeconds, sections[s].EndSeconds);
                loudest = Math.Max(loudest, energies[s]);
            }

            for (var s = 0; s < sections.Count; s++)
            {
                var share = loudest > 0 ? energies[s] / loudest : 0;
                sections[s].Energy = share < AudioAnalysisRules.LowEnergyShare ? AudioEnergy.Low
                    : share < AudioAnalysisRules.MidEnergyShare ? AudioEnergy.Mid
                    : AudioEnergy.High;
            }
        }

        private static void Letter(List<AudioSection> sections, List<double[]> means)
        {
            var letters = new List<(string letter, double[] feature)>();
            for (var s = 0; s < sections.Count; s++)
            {
                var best = -1;
                var nearest = double.MaxValue;
                for (var l = 0; l < letters.Count; l++)
                {
                    var d = Distance(letters[l].feature, means[s]);
                    if (d >= nearest) continue;
                    nearest = d;
                    best = l;
                }

                if (best >= 0 && nearest <= AudioAnalysisRules.SameSection)
                    sections[s].Letter = letters[best].letter;
                else if (best >= 0 && nearest <= AudioAnalysisRules.SimilarSection)
                    sections[s].Letter = letters[best].letter + "'";
                else
                {
                    var letter = letters.Count < 26
                        ? ((char)('A' + letters.Count)).ToString()
                        : "Z" + (letters.Count - 25).ToString(CultureInfo.InvariantCulture);
                    letters.Add((letter, means[s]));
                    sections[s].Letter = letter;
                }
            }
        }

        // ---- Notes ----

        private static List<string> WriteNotes(AudioMap map, int events, Grid grid)
        {
            var notes = new List<string>();
            var tempo = map.Tempo;
            var bpm = tempo.Bpm.ToString("0.##", CultureInfo.InvariantCulture);

            if (!grid.Found)
                notes.Add(Invariant($"Tempo confidence is 0: no beat was found ({events} onsets in the whole track)."));
            else if (tempo.Confidence < AudioAnalysisRules.LowConfidence)
                notes.Add(Invariant($"Tempo confidence is low ({tempo.Confidence:0.00}): {bpm} BPM is a guess;")
                          + " check it by ear or tap it.");

            if (grid.Found && tempo.Ambiguity > AudioAnalysisRules.AmbiguityNote)
                notes.Add(Invariant($"Half or double tempo scores almost as well (ambiguity {tempo.Ambiguity:0.00}):")
                          + Invariant(
                              $" the beat may be {tempo.Bpm / 2:0.##} or {tempo.Bpm * 2:0.##} BPM, not {bpm}."));

            if (grid.Found && Math.Abs(tempo.DriftMs) > AudioAnalysisRules.DriftNoteMs)
                notes.Add(Invariant($"The beat drifts {tempo.DriftMs:0} ms between the first and last third:")
                          + " the tempo is not constant, so one beat segment will not fit the whole track.");

            if (events == 0) return notes;
            foreach (var section in map.Sections)
            {
                if (CountIn(map.Onsets.Low, section.StartSeconds, section.EndSeconds) > 0) continue;
                notes.Add(Invariant($"Section {section.Letter} ({section.StartSeconds:0.0}-{section.EndSeconds:0.0} s)")
                          + " has no low-band onsets: no kick to lock the beat to there.");
            }

            return notes;
        }

        // ---- Small helpers ----

        private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

        private static int MeanByte(byte[] envelope, double from, double to)
        {
            var first = Math.Max(0, (int)Math.Floor(from * AudioAnalysisRules.EnvelopeRate));
            var last = Math.Min(envelope.Length, (int)Math.Ceiling(to * AudioAnalysisRules.EnvelopeRate));
            if (last <= first) return 0;

            long sum = 0;
            for (var i = first; i < last; i++) sum += envelope[i];
            return (int)Math.Round(sum / (double)(last - first));
        }

        private static int CountIn(List<AudioOnset> onsets, double from, double to)
        {
            var count = 0;
            foreach (var o in onsets)
                if (o.Seconds >= from && o.Seconds < to)
                    count++;
            return count;
        }

        // Root mean square of the per-feature differences, in standard deviations once standardised.
        private static double Distance(double[] a, double[] b)
        {
            double sum = 0;
            for (var i = 0; i < a.Length; i++) sum += (a[i] - b[i]) * (a[i] - b[i]);
            return Math.Sqrt(sum / a.Length);
        }

        private static float Percentile(float[] values, float percentile)
        {
            if (values.Length == 0) return 0f;
            var sorted = (float[])values.Clone();
            Array.Sort(sorted);
            var index = (int)Math.Min(sorted.Length - 1, Math.Floor(percentile * (sorted.Length - 1) + 0.5));
            return sorted[index];
        }

        private static double Median(List<double> values)
        {
            values.Sort();
            var middle = values.Count / 2;
            return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) * 0.5;
        }

        private static float Max(float[] values)
        {
            var max = 0f;
            foreach (var v in values) max = Math.Max(max, v);
            return max;
        }

        private static double Max(double[] values)
        {
            var max = 0.0;
            foreach (var v in values) max = Math.Max(max, v);
            return max;
        }

        private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

        // RBJ cookbook second-order section, Q = 1/sqrt(2). An edge at or past Nyquist passes everything
        // (low-pass) or nothing (high-pass).
        private struct Biquad
        {
            private double _b0, _b1, _b2, _a1, _a2;
            private double _x1, _x2, _y1, _y2;

            public static Biquad LowPass(double hz, int sampleRate) => Make(hz, sampleRate, false);

            public static Biquad HighPass(double hz, int sampleRate) => Make(hz, sampleRate, true);

            private static Biquad Make(double hz, int sampleRate, bool highPass)
            {
                var filter = new Biquad();
                if (hz >= sampleRate * 0.45)
                {
                    filter._b0 = highPass ? 0 : 1;
                    return filter;
                }

                var w0 = 2 * Math.PI * hz / sampleRate;
                var cos = Math.Cos(w0);
                var alpha = Math.Sin(w0) / (2 * Math.Sqrt(0.5));
                var a0 = 1 + alpha;
                var edge = highPass ? (1 + cos) / 2 : (1 - cos) / 2;

                filter._b0 = edge / a0;
                filter._b1 = (highPass ? -2 * edge : 2 * edge) / a0;
                filter._b2 = edge / a0;
                filter._a1 = -2 * cos / a0;
                filter._a2 = (1 - alpha) / a0;
                return filter;
            }

            public double Process(double x)
            {
                var y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1;
                _x1 = x;
                _y2 = _y1;
                _y1 = y;
                return y;
            }
        }
    }
}
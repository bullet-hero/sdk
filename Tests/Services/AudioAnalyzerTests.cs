using System;
using System.Linq;
using System.Threading;
using BH.SDK.Services.AudioAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    /// <summary> <see cref="AudioAnalyzer"/> on synthetic tracks generated here: click tracks whose tempo,
    /// phase and downbeat are known, silence, and a track that is honestly ambiguous. </summary>
    [TestFixture]
    public class AudioAnalyzerTests
    {
        private const int SampleRate = 22050;
        private const double Seconds = 24;

        [TestCase(120.0, 0.31, 1, 3)]
        [TestCase(97.3, 0.237, 2, 2)]
        [TestCase(97.3, 0.0, 0, 0)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ClickTrack_FitsTempoAndPhase_AndFindsTheDownbeat(double bpm, double firstBeat, int pickup,
            int downbeat)
        {
            var signal = AudioSignals.ClickTrack(SampleRate, Seconds, bpm, firstBeat, 4, pickup);

            var map = AudioAnalyzer.Analyze(signal, SampleRate, AudioAnalysisSettings.Default);

            Assert.AreEqual(bpm, map.Tempo.Bpm, 0.05, "bpm");
            Assert.AreEqual(0, PhaseError(map.Tempo.PhaseSeconds, firstBeat, 60 / bpm), 0.005, "phase");
            Assert.AreEqual(downbeat, map.Downbeat.Phase, "downbeat");
            Assert.AreEqual(firstBeat + downbeat * 60 / bpm, map.Downbeat.Seconds, 0.005, "downbeat seconds");
            Assert.GreaterOrEqual(map.Tempo.Confidence, AudioAnalysisRules.LowConfidence);
            Assert.Less(Math.Abs(map.Tempo.DriftMs), AudioAnalysisRules.DriftNoteMs);
            Assert.IsEmpty(map.Notes, string.Join(" | ", map.Notes));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ClickTrack_OnsetsLandOnTheClicks()
        {
            const double bpm = 120;
            var signal = AudioSignals.ClickTrack(SampleRate, Seconds, bpm, 0.31, 4, 1);

            var map = AudioAnalyzer.Analyze(signal, SampleRate, AudioAnalysisSettings.Default);

            int beats = 0, kicks = 0;
            for (; 0.31 + beats * 60 / bpm < Seconds - AudioSignals.TailSeconds; beats++)
                if ((beats + 1) % 4 == 0)
                    kicks++;

            Assert.AreEqual(beats, map.Onsets.Mid.Count, "a mid onset per click");
            Assert.AreEqual(kicks, map.Onsets.Low.Count, "a low onset per kick");
            foreach (var onset in map.Onsets.Mid)
                Assert.AreEqual(0, PhaseError(onset.Seconds, 0.31, 60 / bpm), 0.005, $"onset {onset.Seconds}");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Silence_HasNoOnsets_AndSaysTheConfidenceIsZero()
        {
            var map = AudioAnalyzer.Analyze(new float[SampleRate * 10], SampleRate, AudioAnalysisSettings.Default);

            Assert.IsEmpty(map.Onsets.Low);
            Assert.IsEmpty(map.Onsets.Mid);
            Assert.IsEmpty(map.Onsets.High);
            Assert.AreEqual(0, map.Tempo.Bpm);
            Assert.AreEqual(0, map.Tempo.Confidence);
            Assert.True(map.Notes.Any(n => n.Contains("confidence")), string.Join(" | ", map.Notes));
            Assert.True(map.Envelopes.Rms.All(b => b == 0));
            Assert.AreEqual(1, map.Sections.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryOtherBeatAccented_ReportsTheAmbiguity()
        {
            var signal = AudioSignals.EveryOtherBeat(SampleRate, Seconds, 120, 0.1);

            var map = AudioAnalyzer.Analyze(signal, SampleRate, AudioAnalysisSettings.Default);

            Assert.Greater(map.Tempo.Ambiguity, AudioAnalysisRules.AmbiguityNote);
            Assert.True(map.Notes.Any(n => n.Contains("Half or double")), string.Join(" | ", map.Notes));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Sections_SplitWhereHatsComeAndGo_AndLetterTheReturnAlike()
        {
            const double length = 64;
            var signal = AudioSignals.ClickTrack(SampleRate, length, 120, 0);
            for (var t = 0.0; t < length; t += 0.25)
                if (t >= 16 && t < 32 || t >= 48)
                    AudioSignals.AddBurst(signal, SampleRate, t, 7000f, 0.4f, 0.005);

            var map = AudioAnalyzer.Analyze(signal, SampleRate, AudioAnalysisSettings.Default);

            var starts = map.Sections.Select(s => s.StartSeconds).ToArray();
            Assert.AreEqual(new[] { 0.0, 16, 32, 48 }, starts.Select(s => Math.Round(s)).ToArray());
            Assert.AreEqual(map.Sections[0].Letter, map.Sections[2].Letter);
            Assert.AreEqual(map.Sections[1].Letter, map.Sections[3].Letter);
            Assert.AreNotEqual(map.Sections[0].Letter, map.Sections[1].Letter);
            Assert.Greater(map.Sections[1].High, map.Sections[0].High);
            for (var s = 1; s < map.Sections.Count; s++)
                Assert.AreEqual(map.Sections[s - 1].EndSeconds, map.Sections[s].StartSeconds);
            Assert.AreEqual(length, map.Sections[map.Sections.Count - 1].EndSeconds, 1e-9);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Map_RoundTripsThroughJson_UnderCamelCaseNames()
        {
            var signal = AudioSignals.ClickTrack(SampleRate, 8, 120, 0);
            var map = AudioAnalyzer.Analyze(signal, SampleRate, AudioAnalysisSettings.Default);

            var json = JsonConvert.SerializeObject(map);
            var tree = JObject.Parse(json);
            var back = JsonConvert.DeserializeObject<AudioMap>(json);

            Assert.AreEqual(AudioAnalysisRules.MapVersion, (int)tree["version"]);
            Assert.NotNull(tree["tempo"]["phaseSeconds"]);
            Assert.NotNull(tree["downbeat"]["phase"]);
            Assert.AreEqual(JTokenType.String, tree["envelopes"]["rms"].Type, "bytes travel as base64");
            Assert.AreEqual(2, ((JArray)tree["onsets"]["mid"][0]).Count, "an onset is [seconds, strength]");
            Assert.AreEqual(map.Envelopes.Low, back.Envelopes.Low);
            Assert.AreEqual(map.Onsets.Mid.Count, back.Onsets.Mid.Count);
            Assert.AreEqual(map.Onsets.Mid[3].Seconds, back.Onsets.Mid[3].Seconds, 1e-4);
            Assert.AreEqual(map.Tempo.Bpm, back.Tempo.Bpm);
            Assert.AreEqual(map.Sections.Count, back.Sections.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Envelopes_RunAtTheEnvelopeRate_AndPeakAt255()
        {
            var signal = AudioSignals.ClickTrack(SampleRate, 8, 120, 0);

            var map = AudioAnalyzer.Analyze(signal, SampleRate, AudioAnalysisSettings.Default);

            var expected = (int)Math.Ceiling(8.0 * AudioAnalysisRules.EnvelopeRate);
            foreach (var envelope in new[] { map.Envelopes.Rms, map.Envelopes.Low, map.Envelopes.Mid })
            {
                Assert.AreEqual(expected, envelope.Length);
                Assert.AreEqual(255, envelope.Max());
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Analyze_HonoursCancellation_AndRefusesBadInput()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            var signal = new float[SampleRate];

            Assert.Throws<OperationCanceledException>(() =>
                AudioAnalyzer.Analyze(signal, SampleRate, AudioAnalysisSettings.Default, source.Token));
            Assert.Throws<ArgumentNullException>(() => AudioAnalyzer.Analyze(null, SampleRate, null));
            Assert.Throws<ArgumentException>(() => AudioAnalyzer.Analyze(signal, 0, null));
            Assert.Throws<ArgumentException>(() =>
                AudioAnalyzer.Analyze(signal, SampleRate, new AudioAnalysisSettings { MinBpm = 200, MaxBpm = 100 }));
        }

        private static double PhaseError(double actual, double expected, double period)
        {
            var d = (actual - expected) % period;
            if (d > period / 2) d -= period;
            if (d < -period / 2) d += period;
            return d;
        }
    }
}

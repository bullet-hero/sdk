using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace BH.SDK.Services.AudioAnalysis
{
    // A cache format, not a level model: no [GenerateModel], no generation. A change of shape or of the
    // algorithm moves AudioAnalysisRules.MapVersion instead.

    /// <summary> What <see cref="AudioAnalyzer"/> heard in one track: envelopes, onsets, the tempo, the
    /// downbeat, sections and plain-language notes. </summary>
    public sealed class AudioMap
    {
        /// <summary> <see cref="AudioAnalysisRules.MapVersion"/> of the analyzer that made it. </summary>
        [JsonProperty("version")]
        public int Version { get; set; } = AudioAnalysisRules.MapVersion;

        /// <summary> Sample rate of the analysed signal, Hz. </summary>
        [JsonProperty("sampleRate")]
        public int SampleRate { get; set; }

        /// <summary> Length of the analysed signal, seconds. </summary>
        [JsonProperty("durationSeconds")]
        public double DurationSeconds { get; set; }

        /// <summary> Envelope samples per second (<see cref="AudioAnalysisRules.EnvelopeRate"/>). </summary>
        [JsonProperty("envelopeRate")]
        public int EnvelopeRate { get; set; }

        /// <summary> Loudness over time, overall and per band. </summary>
        [JsonProperty("envelopes")]
        public AudioEnvelopes Envelopes { get; set; } = new();

        /// <summary> Attacks per band. </summary>
        [JsonProperty("onsets")]
        public AudioOnsets Onsets { get; set; } = new();

        /// <summary> The fitted beat. </summary>
        [JsonProperty("tempo")]
        public AudioTempo Tempo { get; set; } = new();

        /// <summary> Which beat starts a bar. </summary>
        [JsonProperty("downbeat")]
        public AudioDownbeat Downbeat { get; set; } = new();

        /// <summary> Beats per bar the downbeat and sections were found with. </summary>
        [JsonProperty("beatsPerBar")]
        public int BeatsPerBar { get; set; }

        /// <summary> Consecutive stretches of similar sound, in time order, covering the track. </summary>
        [JsonProperty("sections")]
        public List<AudioSection> Sections { get; set; } = new();

        /// <summary> Sentences for the reader: what to doubt and why. </summary>
        [JsonProperty("notes")]
        public List<string> Notes { get; set; } = new();
    }

    /// <summary> Four envelopes at <see cref="AudioMap.EnvelopeRate"/>, each 0..255 against its own 99th
    /// percentile. JSON carries each as base64. </summary>
    public sealed class AudioEnvelopes
    {
        /// <summary> RMS of the whole signal. </summary>
        [JsonProperty("rms")]
        public byte[] Rms { get; set; } = Array.Empty<byte>();

        /// <summary> RMS below <see cref="AudioAnalysisRules.LowBandEdge"/>. </summary>
        [JsonProperty("low")]
        public byte[] Low { get; set; } = Array.Empty<byte>();

        /// <summary> RMS between the two band edges. </summary>
        [JsonProperty("mid")]
        public byte[] Mid { get; set; } = Array.Empty<byte>();

        /// <summary> RMS above <see cref="AudioAnalysisRules.HighBandEdge"/>. </summary>
        [JsonProperty("high")]
        public byte[] High { get; set; } = Array.Empty<byte>();
    }

    /// <summary> Onsets of the three bands, each in time order. </summary>
    public sealed class AudioOnsets
    {
        /// <summary> Low band (kick, bass). </summary>
        [JsonProperty("low")]
        public List<AudioOnset> Low { get; set; } = new();

        /// <summary> Mid band (snare, voice, most instruments). </summary>
        [JsonProperty("mid")]
        public List<AudioOnset> Mid { get; set; } = new();

        /// <summary> High band (hats, cymbals). </summary>
        [JsonProperty("high")]
        public List<AudioOnset> High { get; set; } = new();
    }

    /// <summary> One attack: when, and how strong against the band's own strong attacks (0..1). JSON
    /// writes it as <c>[seconds, strength]</c>. </summary>
    [JsonConverter(typeof(AudioOnsetConverter))]
    public readonly struct AudioOnset
    {
        /// <summary> Time of the attack, seconds from the start of the signal. </summary>
        public readonly double Seconds;

        /// <summary> 0..1, 1 being the band's strong attacks. </summary>
        public readonly float Strength;

        /// <summary> Built from both values. </summary>
        public AudioOnset(double seconds, float strength)
        {
            Seconds = seconds;
            Strength = strength;
        }
    }

    /// <summary> The fitted beat grid: <c>phaseSeconds + k * 60 / bpm</c>. </summary>
    public sealed class AudioTempo
    {
        /// <summary> Beats per minute; 0 when nothing was found. </summary>
        [JsonProperty("bpm")]
        public double Bpm { get; set; }

        /// <summary> Time of the first beat, in [0, one beat). </summary>
        [JsonProperty("phaseSeconds")]
        public double PhaseSeconds { get; set; }

        /// <summary> 0..1: how far the best tempo stands above the median one. </summary>
        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        /// <summary> 0..1: the better of half and double tempo, against the best. </summary>
        [JsonProperty("ambiguity")]
        public double Ambiguity { get; set; }

        /// <summary> How far the beat slides between the first and last third, milliseconds. </summary>
        [JsonProperty("driftMs")]
        public double DriftMs { get; set; }
    }

    /// <summary> Which beat of the grid is the first of a bar. </summary>
    public sealed class AudioDownbeat
    {
        /// <summary> Index of the first downbeat among the grid's beats, 0..beatsPerBar-1. </summary>
        [JsonProperty("phase")]
        public int Phase { get; set; }

        /// <summary> Time of the first downbeat, seconds. </summary>
        [JsonProperty("seconds")]
        public double Seconds { get; set; }

        /// <summary> 0..1: how far the best candidate stands above the second. </summary>
        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }

    /// <summary> Loudness class of a section against the loudest one. </summary>
    public static class AudioEnergy
    {
        /// <summary> Under <see cref="AudioAnalysisRules.LowEnergyShare"/> of the loudest section. </summary>
        public const string Low = "low";

        /// <summary> Under <see cref="AudioAnalysisRules.MidEnergyShare"/> of the loudest section. </summary>
        public const string Mid = "mid";

        /// <summary> The rest. </summary>
        public const string High = "high";
    }

    /// <summary> A stretch of similar sound between two downbeats. </summary>
    public sealed class AudioSection
    {
        /// <summary> Start, seconds. </summary>
        [JsonProperty("startSeconds")]
        public double StartSeconds { get; set; }

        /// <summary> End, seconds (exclusive). </summary>
        [JsonProperty("endSeconds")]
        public double EndSeconds { get; set; }

        /// <summary> <see cref="AudioEnergy"/>: <c>low</c>, <c>mid</c> or <c>high</c>. </summary>
        [JsonProperty("energy")]
        public string Energy { get; set; } = AudioEnergy.Low;

        /// <summary> Mean low-band envelope, 0..255. </summary>
        [JsonProperty("low")]
        public int Low { get; set; }

        /// <summary> Mean mid-band envelope, 0..255. </summary>
        [JsonProperty("mid")]
        public int Mid { get; set; }

        /// <summary> Mean high-band envelope, 0..255. </summary>
        [JsonProperty("high")]
        public int High { get; set; }

        /// <summary> Similarity letter: equal letters sound alike, a prime (<c>A'</c>) is a variation.
        /// </summary>
        [JsonProperty("letter")]
        public string Letter { get; set; } = string.Empty;
    }

    /// <summary> <see cref="AudioOnset"/> as a two-number array. </summary>
    public sealed class AudioOnsetConverter : JsonConverter<AudioOnset>
    {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, AudioOnset value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteRawValue(Math.Round(value.Seconds, 4).ToString("R", CultureInfo.InvariantCulture));
            writer.WriteRawValue(Math.Round(value.Strength, 3).ToString("R", CultureInfo.InvariantCulture));
            writer.WriteEndArray();
        }

        /// <inheritdoc />
        public override AudioOnset ReadJson(JsonReader reader, Type objectType, AudioOnset existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException($"An onset is [seconds, strength], got {reader.TokenType}.");

            var seconds = ReadNumber(reader);
            var strength = ReadNumber(reader);
            if (!reader.Read() || reader.TokenType != JsonToken.EndArray)
                throw new JsonSerializationException("An onset is exactly two numbers.");

            return new AudioOnset(seconds, (float)strength);
        }

        private static double ReadNumber(JsonReader reader)
        {
            if (!reader.Read() || (reader.TokenType != JsonToken.Float && reader.TokenType != JsonToken.Integer))
                throw new JsonSerializationException("An onset is [seconds, strength], two numbers.");
            return Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture);
        }
    }
}

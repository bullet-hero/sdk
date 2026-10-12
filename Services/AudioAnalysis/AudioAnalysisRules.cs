namespace BH.SDK.Services.AudioAnalysis
{
    // Why each number is what it is: Docs/MUSIC.md. Changing one changes every cached map,
    // so it moves MapVersion with it.

    /// <summary> The fixed numbers of <see cref="AudioAnalyzer"/>. </summary>
    public static class AudioAnalysisRules
    {
        /// <summary> Version of the algorithm a map was made by; a cache keyed on content compares it.
        /// </summary>
        public const int MapVersion = 2;

        /// <summary> STFT window, in samples; a power of two. </summary>
        public const int FrameSize = 2048;

        /// <summary> STFT step, in samples. </summary>
        public const int Hop = 512;

        /// <summary> Upper edge of the low band, Hz. </summary>
        public const float LowBandEdge = 150f;

        /// <summary> Upper edge of the mid band, Hz; the high band is everything above. </summary>
        public const float HighBandEdge = 4000f;

        /// <summary> Envelope samples per second. </summary>
        public const int EnvelopeRate = 100;

        /// <summary> Samples per second of the energy an onset time is refined against. </summary>
        public const int FineRate = 1000;

        /// <summary> Envelopes are divided by this percentile of themselves before the 0..255 scale.
        /// </summary>
        public const float EnvelopePercentile = 0.99f;

        /// <summary> Width of the moving median under the onset threshold, seconds. </summary>
        public const float OnsetMedianSeconds = 0.5f;

        /// <summary> Added to the moving median, in units of the band's flux percentile. </summary>
        public const float OnsetDelta = 0.1f;

        /// <summary> Two onsets of one band are at least this far apart, seconds. </summary>
        public const float OnsetMinGapSeconds = 0.05f;

        /// <summary> Onset strength is against this percentile of the band's picked peaks. </summary>
        public const float StrongPercentile = 0.95f;

        /// <summary> A weaker onset is dropped: it is spill from a loud attack in another band. </summary>
        public const float MinOnsetStrength = 0.25f;

        /// <summary> A band whose flux percentile per bin is below this has no onsets at all (silence).
        /// </summary>
        public const float MinFluxPerBin = 1e-4f;

        /// <summary> Onsets of different bands this close are one event to the tempo fit, seconds. </summary>
        public const float MergeSeconds = 0.025f;

        /// <summary> Slowest tempo tried. </summary>
        public const float MinBpm = 60f;

        /// <summary> Fastest tempo tried. </summary>
        public const float MaxBpm = 200f;

        /// <summary> Coarse tempo step, BPM. </summary>
        public const float BpmStep = 0.5f;

        /// <summary> Fine tempo step around the coarse best, BPM. </summary>
        public const float BpmFineStep = 0.02f;

        /// <summary> Phase step of the tempo search, seconds. </summary>
        public const float PhaseStepSeconds = 0.004f;

        /// <summary> Gaussian width an onset is scored against a predicted beat with, seconds. </summary>
        public const float SigmaSeconds = 0.02f;

        /// <summary> Default beats in a bar. </summary>
        public const int DefaultBeatsPerBar = 4;

        /// <summary> Bars in one window of the section search. </summary>
        public const int SectionBars = 4;

        /// <summary> Section window length when no tempo was found, seconds. </summary>
        public const float SectionFallbackSeconds = 8f;

        /// <summary> The least spread a section feature is standardised by, so a steady track's noise is not
        /// blown up into boundaries. In the feature's own units (0..1). </summary>
        public const float SectionSpreadFloor = 0.1f;

        /// <summary> Distance between two windows above which a boundary is a candidate. </summary>
        public const float SectionNovelty = 0.6f;

        /// <summary> Distance at or below which two sections share a letter. </summary>
        public const float SameSection = 0.4f;

        /// <summary> Distance at or below which a section is a variation (<c>A'</c>). </summary>
        public const float SimilarSection = 0.7f;

        /// <summary> A section quieter than this share of the loudest is <c>low</c> energy. </summary>
        public const float LowEnergyShare = 0.5f;

        /// <summary> A section quieter than this share of the loudest is <c>mid</c> energy. </summary>
        public const float MidEnergyShare = 0.8f;

        /// <summary> Tempo confidence below this gets a note. </summary>
        public const float LowConfidence = 0.5f;

        /// <summary> Ambiguity above this gets a note. </summary>
        public const float AmbiguityNote = 0.8f;

        /// <summary> Drift above this, milliseconds, gets a note. </summary>
        public const float DriftNoteMs = 15f;

        /// <summary> Onsets a third of the track needs before its drift residual counts. </summary>
        public const int MinDriftOnsets = 4;

        /// <summary> Merged onsets a window needs before its own tempo is fitted (<c>FitWindow</c>). </summary>
        public const int MinWindowEvents = 8;
    }
}
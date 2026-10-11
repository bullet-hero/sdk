# Music: `AudioAnalyzer` and `MusicGrid`

Two pure halves of hearing a level's track and speaking about it in bars. `Services/AudioAnalysis/`
turns a mono signal into an `AudioMap`; `Agent/MusicGrid` numbers the bars of the level's own
`BeatSegment`s, and `TimeAddress` reads the musical time forms through it. Both are BCL + Newtonsoft,
hold no static mutable state and run as well in the game as in a server. The game repo's
`Docs/Plans/LEVEL_MCP_PLAN.md` (Phase 4) is what consumes them.

## `AudioAnalyzer.Analyze(mono, sampleRate, settings, token)`

Every number below is a constant of `AudioAnalysisRules`; a change to any of them, or to the steps,
moves `AudioAnalysisRules.MapVersion`, which a content-hash cache compares.

1. **Band signals and fine energy.** Three RBJ biquads (Q = 1/sqrt 2) split the signal at
   `LowBandEdge` and `HighBandEdge` (low = low-pass, mid = high-pass then low-pass, high = high-pass);
   an edge at or past 0.45 of the sample rate passes everything or nothing. The mean square of the
   signal and of each band is kept per 1 ms block (`FineRate`). Block `j` covers samples
   `[floor(j * sr / 1000), floor((j + 1) * sr / 1000))`, so ten blocks are exactly one envelope sample.
2. **Envelopes.** RMS per `1 / EnvelopeRate` s, overall and per band, divided by the envelope's own
   `EnvelopePercentile` (its max when that is zero) and stored as 0..255. JSON writes each `byte[]`
   as base64.
3. **Spectral flux.** STFT frames of `FrameSize` samples, periodic Hann, centred on `k * Hop` (zero
   outside the signal), two real frames packed into one complex FFT. Magnitudes are compressed as
   `log(1 + |X|)`; the flux of a band is the half-wave rectified rise over the previous frame, summed
   over the band's bins (DC excluded) and divided by their count.
4. **Onsets per band.** Flux divided by its `EnvelopePercentile` (a band below `MinFluxPerBin` has no
   onsets: silence). A frame is a candidate when it is a local maximum above the moving median of
   `OnsetMedianSeconds` plus `OnsetDelta`. Candidates closer than `OnsetMinGapSeconds` keep the
   stronger. Strength is the peak against the band's `StrongPercentile` of picked peaks, capped at 1;
   below `MinOnsetStrength` it is dropped - a loud click spills a sixth of a kick's flux into the low
   band through its own decay, and that spill is not a kick.
5. **Onset time.** The flux of an impulse peaks half a hop BEFORE the attack (the derivative of the
   window), the flux of a sustained tone half a hop AFTER it, so a frame time alone is off by up to
   a hop either way (23 ms at 22.05 kHz). The time is instead the start of the 1 ms block with the
   steepest rise of the band's fine energy within 1.5 hops of the frame centre. Measured on the
   synthetic clicks of `AudioAnalyzerTests`: mid-band onsets within 0.6 ms of the click, low-band
   (the 70 Hz kick, behind a 150 Hz low-pass) 1-4 ms late.
6. **Tempo.** Onsets of all bands closer than `MergeSeconds` are one event, as strong as its
   strongest member (a sum lets an accented beat 1 outvote the beat and pushes half tempo over the
   ambiguity note). For each BPM from `MinBpm` to `MaxBpm` in `BpmStep`, then `BpmFineStep` around
   the best, every event's phase residue is binned in `PhaseStepSeconds` bins and the histogram is
   convolved with a Gaussian of `SigmaSeconds`. The score at a phase is
   `sqrt(onsetShare * min(1, beatPresence))`: the strength share of events near a beat, times the
   share of grid beats with an event near them. Either factor alone is biased - the first always
   prefers double tempo, the second half tempo. A weighted least-squares fit of `t = phase + k * T`
   over the events within `min(3 sigma, T/4)` of a beat then refines period and phase (three
   rounds; a step moving the period over 5% is refused). Measured: 120 and 97.3 BPM click tracks
   at 22.05 and 44.1 kHz fit within 0.002 BPM and 0.5 ms.
7. **Reported.** `confidence = 1 - median(coarse scores) / best`; `ambiguity = max(score(bpm / 2),
   score(bpm * 2)) / best` (the half and double tempo are scored even outside the BPM range);
   `driftMs` = median residual of low-band onsets within a quarter beat, last third of the onset span
   minus first third (all events when a third has fewer than `MinDriftOnsets`). A plain 4/4 click
   track with an accented beat 1 measures ambiguity 0.73-0.75; strong odd beats with soft even ones
   0.84.
8. **Downbeat.** For each of `BeatsPerBar` candidates: low-band support (strength times the Gaussian)
   on beat 1 plus mid-band support on the even beats (2 and 4 in 4/4). With no low or mid support at
   all, the whole onset strength on beat 1 decides. `confidence = (best - second) / best`; a
   candidate shift by two beats in 4/4 differs only by the low band, so 0.2-0.35 is the normal range
   for a correct answer on a click track.
9. **Sections.** Windows of `SectionBars` bars from the first downbeat (`SectionFallbackSeconds`
   with no tempo); the stretch before the first downbeat joins the first window, a tail under a bar
   the last. Features per window: mean rms/low/mid/high envelope and the onset rate per band
   (each rate divided by its maximum over windows). A boundary is a novelty peak
   (`1 - cosine` with the previous window) above `SectionNovelty`. Sections are lettered against the
   earlier letters' mean features: `SameSection` or more shares the letter, `SimilarSection` or
   more takes it with a prime (`A'`), else the next letter. Neighbours with one letter merge. Energy
   is the section's mean rms against the loudest section (`LowEnergyShare`, `MidEnergyShare`).
10. **Notes.** Low or zero confidence (`LowConfidence`), ambiguity over `AmbiguityNote`, drift over
    `DriftNoteMs`, and every section with no low-band onset (when the track has onsets at all).

Cost: a 270 s, 44.1 kHz track (11.9 M samples) analyses in about 0.6 s on one core, the STFT and
the biquads being most of it; the map's JSON is about 160 KB, three quarters of it envelopes.

## `MusicGrid` - one bar count across segments

- Segments are taken in start order; null and unusable ones (`BeatMath.IsUsable`) are skipped, and so
  is a segment with no beat inside its span. The grid copies what it needs, so it is immutable.
- **Beats are `BeatMath`'s**: beat `i` of a segment is `start + round(offset + i * framesPerBeat)`,
  computed in float, rounded half away from zero, `i >= 0`, kept while inside the span; index
  `i % BeatsPerBar == 0` is a downbeat (`BeatMath.IsDownbeat`). An address therefore lands on the line
  the editor's grid draws.
- **Bars continue across segments with no gap**, even when the segments have gaps between them. A
  segment contributes `floor(last / perBar) - floor(first / perBar) + 1` bars over its beat indices,
  which is `ceil(beats / perBar)` whenever the first beat is a downbeat; a negative offset that hides
  index 0 makes the first bar a pickup (partial, counted as one). `bar:N` is the first beat of bar N
  that exists; `bar:N.B` is beat B (1-based) and fails when that beat lies outside the segment.
- **`beat:X`** counts every segment's beats in order from 1; a fraction is a fraction of that beat's
  segment tempo, and landing past the segment's end is an error, never the next segment.
- **Frame to position**: the segment containing the frame, the last beat at or before it (rounded
  beat frames count as exact), and the fraction of a beat since. Before a segment's first beat the
  fraction is negative. A frame in no segment has no position.
- **Every failure names the nearest segment** (`MusicGrid.Describe`: name, span, bars, beats).
- **Shifts**: `TryShiftBars` keeps beat and fraction and changes the bar (a beat the target bar does
  not have is an error); `TryShiftBeats` moves the global beat number, so it crosses gaps.

## Musical time in `TimeAddress`

The overloads that take a `TimeAddressContext` (a `MusicGrid` plus a marker-name lookup the caller
builds from its own level) add `bar:17`, `bar:17.3`, `beat:64.5`, `marker:drop`,
`marker:drop+2bars`, `marker:drop-1beat` as times and `2bars`, `3beats` as durations. A marker name is
tried whole first, so a marker called `pre-drop` is not read as `pre` minus something. Bars are whole
numbers; beats may be fractional. A musical duration is measured from a start frame (bars keep the
beat, so `1bar` from beat 4 into a 3/4 segment fails); without one, only a grid with a single tempo
and bar length can answer. The context-free overloads behave as before, except that a musical form
now fails saying the level has no beat grid instead of asking for a frame.

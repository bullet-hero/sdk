# CLAUDE.md - `Assets/Plugins/BH.SDK/Services/AudioAnalysis`

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

`AudioAnalyzer.Analyze` hears a mono signal (envelopes, per-band onsets, tempo, downbeat, sections,
notes) into an `AudioMap`; `Fft` is its radix-2 transform. Every step and every number:
`Docs/MUSIC.md`.

## Invariants

- **Pure and thread-safe**: BCL + Newtonsoft, no `UnityEngine`, no static mutable state - every buffer
  belongs to one `Analyze` call, which a host runs on the thread pool and a server runs as is.
- **`AudioMap` is a cache format, not a level model**: explicit camelCase `[JsonProperty]`, no
  `[GenerateModel]`, no generation, no bump. A change to its shape, a step or an `AudioAnalysisRules`
  number moves `AudioAnalysisRules.MapVersion` instead, so a cached map is recomputed.
- **Tests synthesise their signals** (`Tests/Services/AudioSignals`); no audio file is checked in.

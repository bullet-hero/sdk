# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/Game

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## `Models/Game/`, `Models/Events/`: level-global vs. per-object

`GameLevel` (`Level.Game`) = `Objects` (the `IObjectScope`) + four `[ModelGeneration]`-tagged event
aggregates: `GameEvents` (Markers — editor-only annotation, no gameplay effect — **Beats**, Checkpoints,
ScreenLimits, Backgrounds [`Color3Key`, themeable], Themes [`ThemeKeyframe`]), `CameraEvents`
(Positions/Rotations/Pivots like a `RectObject`, but `Zooms` instead of 2-axis `Sizes` and an added
`Shakes` track — no Layer/Anchors/Sizes, the camera has no parent/isn't rendered), `PostProcessingEvents`
(above), `PlayerEvents` (`Visibles`/`Controls`/`Collisions`, each `List<BoolKey>`). `Checkpoint`/
`Marker` (`Models/Events/`) are flat one-shot lists, not animated keyframe tracks, despite living
alongside `ThemeKeyframe` (which *is* a real track) in the same `GameEvents` class.

**`GameEvents.Beats`** (`List<BeatSegment>`, `Models/Events/BeatSegment.cs`) is a third shape again —
neither a track nor a list of points, but a list of **non-overlapping `FrameSpan` segments**, one per
stretch of constant tempo (`Bpm` + `Offset`, the phase, in **fractional** frames + `BeatsPerBar`,
plus `Marker`-style `Name`/`Color4`). Spans rather than tempo points because a point track cannot
express a HOLE: an intro with no percussion, a break, the tail after the song ends. Editor-only in
exactly the sense `Markers` is — saved, read back, consumed by generators and by the editor's own
snapping, never by playback. `Span`'s setter strips `FrameAnchor`s: anchors mean "follow the parent's
edge" and a segment has no parent. Adding it needed no migration — the domain stays at generation 1 and
an older file deserializes to an empty list.

**Where the beats fall is computed, never stored**: `Utils/BeatMath` resolves
`Start + round(Offset + i * framesPerBeat)` — rounded from the segment's own start rather than
accumulated, so the error stays at half a frame instead of growing with every beat — and every
collection is bounded by `LevelRules.MaxBeatGridPoints`, since a fast tempo over a long span is
millions of lines. It lives here rather than in the consumer because both halves need the SAME grid:
a generator resolves beats with no Unity around it, the editor draws and snaps with the result.
Non-overlap is `LevelGraphAnalyzer`'s (`GraphRule.BeatSegmentsOverlap`), not a `[RuleXxx]`'s — an
attribute sees one property at a time — and like every graph finding it carries no repair.

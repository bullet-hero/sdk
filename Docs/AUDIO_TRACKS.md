# Audio tracks: `AudioLevel`, `LevelTrack`, `LevelTrackEffects`

Split out of `Models/CLAUDE.md`, which keeps the summary and links here. Design facts of
`Models/Audio/` and `Models/AudioEffects/` that the code cannot say by itself; the consumer side is
the Unity project's `BuildAudioJob`.

## The track dictionary

`AudioLevel.Tracks : Dictionary<AudioId, LevelTrack>` - one flat dict per level. `AudioId` bans
negative values: there is no game/user-defined split, unlike the other ids (`IDENTIFIERS.md`).

## The effect chain (`LevelTrackEffects`)

- **A flat class with one slot per DSP effect** (Lowpass/Highpass/Echo/Reverb/Chorus/PitchShifter/
  Distortion/Flange/Compressor/Normalize/ParamEQ), not a dictionary/flags/list.
- **A slot is NULL until an author touches it**, and null means "this effect is not in the chain".
  The eleven were always-present objects sitting at the disabled floor, which wrote 1.1 KB of pure
  defaults into every track of every level. They are constructed null because a member that may be
  null has to be (see `Docs/NAMING.md`), which is what lets the writer skip it: an untouched track's
  whole chain is `"eff":{"vlm":[],"stpan":[],"a":false}`, and dialling ONE effect in writes that one.
- **`MixLevel` did NOT change meaning with that**: it is a real wet level in dB whose floor is
  silence, so `AudioRules.IsActiveMixLevel` still answers "is this audible". Null answers "is it here
  at all", and the two are different questions - an effect an author dialled in and then switched off
  keeps its object and its settings. Every consumer treats a null slot as the default instance, which
  IS the off state field for field (`MixLevel_Default` is `MixLevel_Disabled`).
- **`LevelTrackEffects.Active`** (track-level, default `false`) is the master switch for the whole
  chain - a third thing again, don't confuse it with either.

## Two things called volume

**`LevelTrack.Volume` (`[0, 1]`, default `1`) is the track's own fader and the SECOND thing on it
called volume** - the first being `Effects.Volumes`, the keyframed curve. They MULTIPLY at playback
rather than compete (the consumer does it in `BuildAudioJob`): the fader is what the whole track sits
behind, the curve is what fades it in and out inside that, and an author wanting "this track,
quieter" should not have to rewrite every key on it. Additive with a constructor default, so it
needed no migration before 1.0.0 (added 2026-08-19) and `AudioLevel` stays at generation 1 - a pre-fader file reads back at full
volume, which is exactly how it sounded before. Note this is the opposite call from `Speed` below,
whose pre-Speed default deserialized to a silent `0`.

## Speed

`LevelTrack.Speed` (`[-2, 2]`, default `1`) is the track's own resample rate - faster is also
higher-pitched, negative **reverses** the track (it starts at the clip's END and plays back to its
start, with `OffsetTime` skipping the tail rather than the head), `0` freezes it silent - and it is
deliberately **not** keyframed: an animated rate would make the clip position the integral of that
curve, which no consumer can evaluate from one frame's data.

It shipped **without a migration** on purpose, before 1.0.0 (added 2026-08-10; `AudioLevel` stays at generation 1): a pre-Speed file
deserializes to `0f`, i.e. silent tracks, and the levels that existed at the time were the author's
own to re-save. Don't add a `NullSpeed`-style sentinel after the fact - `0` is a legal authored value
here.
